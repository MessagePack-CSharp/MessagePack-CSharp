# MessagePack.Tests.V3Compat

MessagePack-CSharp v3.1.8 の `tests/MessagePack.Tests`(+ `sandbox/SharedData`)をそのまま持ち込み、v4 に対して走らせる互換スイート。
upstream との diff 同期を最優先とし、テストファイル本体への編集は最小限に留める。

## 編集ポリシー

- upstream ファイルへの手編集はすべて `V3COMPAT-EDIT` コメント付き。無効化は `#if V3COMPAT_EXCLUDED`(未定義シンボル)で括る。
- ファイル単位で成立しないものは csproj の `<Compile Remove>` で隔離(理由コメント付き)。削除はしない。
- v3 だけにある API は `V3CompatShims/` で v4 に写像する。テスト側を v4 風に書き換えるより、シムを足す。
  - `V3Shims.cs`: v3 リゾルバ名 → v4 factory チェーン、`WithCompression`/`WithSecurity` などの options 拡張、`UnionAttribute` → `UnionTagAttribute`、xunit v2 の `SkippableFact` 等。
  - `V3OracleAliases.cs`: `MessagePackWriter`/`MessagePackReader`/`ExtensionHeader` などは本物の v3 ランタイム(`tools/oracle/MessagePackV3.dll`、extern alias V3)へ global using alias。手書きペイロードは「本物の v3 が書いたバイト列を v4 が読む」形になる。
- 機械置換(全域 sed): `MessagePackSerializer.Typeless.*` → `TypelessCompat.*`、`MessagePackSerializer.SerializeToJson` → `V3JsonCompat.SerializeToJson`、`MessagePackSerializerOptions.Standard` → `.Default`、`MessagePackSerializer.DefaultOptions` → `MessagePackSerializerOptions.Default`。
- `DYNAMIC_GENERATION` を定義してビルドする(upstream が動的リゾルバ系テストをこのシンボルで括っている。v4 では reflection tier が対応物)。

## 実行

```
dotnet run --project tests/MessagePack.Tests.V3Compat -c Release
```

## 失敗 = 発見

このスイートの赤は「v4 のバグ」か「意図的な非互換」のどちらか。2026-08-29 の調査で全失敗を分類済み。

**意図的な非互換は `[Fact(Skip = "v4 intentional: ...")]` で明示**(理由は各 Skip 文字列に記載)。主なもの:

- ExpandoObject / System.Type: 既定チェーンから外れた opt-in factory。
- boxed primitive は forced-width 書き込みで .NET 型を保存(v3 の compact 書き込みは読み戻しで幅が縮む)。
- ConvertToJson は diagnostic view(ext の `{"$extension"}` 形式、DateTime の ISO-8601 トリム)。
- v3 の可変 options オブジェクトモデル(LoadType / ThrowIfDeserializingTypeIsDisallowed / WithPool)は sealed record の v4 に対応物がない。
- 例外の InnerException は作らない。v3 の inner EndOfStreamException は「リーダが投げた実例外をシリアライザが包んだ」構造の副産物であって設計された契約ではなく、それを模して情報のない `new EndOfStreamException()` を詰めるのは無意味(2026-08-29 に一度実装して撤回)。

**2026-08-29 時点で失敗 0**: 全 579 テストが「緑」または「理由付き Skip(意図的非互換)」に分類済み。追補された v3 互換挙動:

- `[SerializationConstructor]` は非公開 ctor でも allowPrivate 無関係に尊重(v3 は常に NonPublic 込みで探索)。
- 文字列キーモードのメンバー順は `[DataMember(Order)]` で安定ソート(欠落は int.MaxValue、Order 未指定の属性は -1)。
- contractless / map モードの包含規則: 「書き込み不能(公開 setter なし・initonly)かつ ctor に消費されず、明示契約でもない」メンバーは**直列化自体されない**(計算プロパティ `Foo => throw` は触られない。private-setter も ctor 不一致なら落ちる — oracle 実証 `{}`)。
- `[MessagePackObject]` は Inherited=true(v3 と同宣言)なので、無注釈の派生型は base の契約で直列化(factory の claim も base チェーンを見る)。
- readonly(initonly)フィールドへの書き戻しは可能(v3 は IL で書いていた。Emit 側は skipVisibility stfld、ns2.0 側は FieldInfo.SetValue)。

(解消済み 2026-08-29:
- [MessagePackFormatter] の v3 互換 → メンバーレベルは名前照合+v3 FormatterType/v4 FactoryType の両プロパティ読みで reflection スロットが解釈。型レベルのランタイム tier(AttributeFormatterFactory)は 2026-08-30 に追加したが 2026-09-03 に再削除: この属性が指すのは実行コードで、prebuilt v3 DLL が名指しできるのは v3 コンパイル済みフォーマッタ(v3 の Writer/Reader に ABI 依存、v4 では実行不可)だけなので、救える DLL が存在しない。SG は core パッケージ同梱なので v4 参照アセンブリは全て SG 経由。残したのは診断のみ: 型レベル注釈を持つ型が reflection tier まで落ちてきたら「v3 フォーマッタは v4 で実行不可、移植を」(v3 IMessagePackFormatter<T> 実装時)または「SG が走っていない」を専用メッセージで throw(無言の reflection map は出さない)。
- ランタイム Union → ReflectionUnionFormatter を reflection tier(非AOT)に追加。v4 の UnionTagAttribute と v3 の UnionAttribute の両方をフルネーム+ダックリーディングで認識するので、v3 でビルド済みの annotation-only DLL がそのまま動く(v4 は UnionAttribute という型を出荷しないため C# 15 の union と名前衝突しない)。ワイヤは fixarray(2) [tag, payload]。unknown tag は skip+null、untagged 実行時型は v3 同様の無言 nil(SG 生成側は throw のまま = 宣言バグ検出)。新規コードは SG を使う前提の互換層。
- 機械的修正7件: `new` シャドーイングは v3 規則を完全移植(base優先の安定ソート、衝突2つ目以降は `DeclaringType.FullName.Name` 修飾キーで両方直列化)。map モード + 迷い込み `[Key(int)]` は無視(map が勝つ)。base virtual の契約属性は手動ウォークで継承(`[DataMember]` は Inherited=false なので inherit:true では届かない)。`[NonSerialized]` は全モードで除外。ctor 引数は代入可能性で一致。`byte[]` は fixarray ワイヤも受理(空は Array.Empty シングルトン)。ConvertToJson の float32 は float の最短表現。
- 注意: PrivateReadonlyFieldSetInConstructor の赤は v4 のバグではなく、LangVersion preview の C# 14 `field` キーワードが upstream テストの `Field => field` を合成バッキングフィールドに束縛していたのが原因。V3Compat は LangVersion 13 に固定した。
- DeserializeAsync(Stream) の先読み破棄 → seekable ストリームでは同期版と同じく余剰分を Seek で返すよう修正。
- 縮まないペイロードの LZ4 raw フォールバック → v3 互換に倒して撤廃。閾値以上なら常に封筒(縮まないデータでも +0.4〜0.8% 程度)。封筒の有無がデータ内容に依存しない。
- LZ4 復元爆弾 → 二段ガードを実装。宣言長 > 圧縮長×255 は嘘として即拒否(常時)、加えて宣言長合計をプロセッサの MaxDecompressedSize(既定 64MB、v3 UntrustedData と同値)で上限。WithLz4Block(long) / new Lz4BlockProcessor(long) で変更可。
- カスタムコレクション → v3 DynamicGenericResolver の4規則を Generic tier に移植: IDictionary+new → IReadOnlyDictionary+ctor → ICollection+new → 非generic view+new → IEnumerable<T>+ctor(構築は v3 同様 Activator.CreateInstance、DefaultBinder のオーバーロード選択込み)。SG ハーベストと MsgPack101 アナライザの3ミラーも同規則に拡張(AOT でも生成登録で動く)。contractless が `{}` に潰していた形状は Generic tier が先に claim。ctor 無しの IEnumerable 実装はv3同様 contractless の object 扱いのまま。
- Typeless の interface/abstract 静的型 → ForceTypelessFormatter(v3 同名クラスの移植)を WithTypeless の末尾側 factory として追加。object 担当は先頭・interface/abstract 担当は末尾の2枚差し(v3 の TypelessObjectResolver 末尾配置の写像)で、IList<int> 等のコレクション interface は BuiltIn の担当のまま。
- object 型スロットの実行時型ディスパッチ → ObjectFallbackFormatter(v3 DynamicObjectTypeFallbackFormatter 移植)を非AOTチェーン(Default / DotNetOptimized)の BuiltIn 手前に配置。POCO は実行時型のフォーマッタで直列化、プリミティブは mini-protocol 維持(forced-width も維持)、素の object は空マップ、読みは PrimitiveObjectFormatter 転送(v3 の非対称そのまま)。AOT チェーンは closed table のまま(v3 の AvoidDynamicCode 分岐と同じ線引き)。)
