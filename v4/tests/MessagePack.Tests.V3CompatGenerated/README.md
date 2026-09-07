# MessagePack.Tests.V3CompatGenerated

MessagePack-CSharp v3.1.8 の `tests/MessagePack.SourceGenerator.ExecutionTests`(+ `MapModeExecutionTests`、`MapMode/` サブフォルダ)を持ち込み、**v4 のソースジェネレータを有効にして**走らせる互換スイート。
生成される**テキスト**は v3 と設計的に異なるが、ラウンドトリップの**セマンティクス**は一致しなければならない、というのが検証対象。MessagePack.Tests.V3Compat(ランタイム tier)と対になる。

## 編集ポリシー

MessagePack.Tests.V3Compat と同一: 手編集は `V3COMPAT-EDIT` コメント付き、ファイル単位の隔離は csproj の `<Compile Remove>`(理由コメント付き)、削除はしない。

主な書き換え:

- v3 形の手書きフォーマッタ(`IMessagePackFormatter<T>` + MessagePackWriter/Reader)は v4 の buffer-generic 形(`IMessagePackFormatter<TWriteBuffer, TReadBuffer, T>`)へ書き換え。属性引数は unbound generic 形(`typeof(XxxFormatter<,>)`)。
- `[Union(0, typeof(...))]` → `[MessagePackObject]` + `[UnionTag(typeof(...), 0)]`(属性リネームが定める機械的移行。v3 綴りの認識はランタイム tier 専用で、SG が主役の本プロジェクトではネイティブ綴りが忠実な移植)。
- `AllowPrivate = true` の型は partial 化(v4 はフォーマッタを nested class として生成する — MsgPack010)。
- `ConvertToJson` は options を取らない(resolver 非依存の diagnostic view)。
- v3 がワイヤを `MessagePackReader` で歩いた検証は、v4 に単体リーダ構造体がないため同形状の map deserialize で置換(StringKeyOverrideTests)。
- LangVersion 13 固定(C# 14 の `field` キーワードが upstream のメンバーアクセスを再束縛するため。V3Compat と同じ理由)。

## 隔離(v4 に対応物がない v3 SG 機構)

- `GeneratedMessagePackResolver.cs`(×2): v4 は module initializer 登録で、リゾルバ宣言属性がない。
- `GeneratedCompositeResolverTests.cs`: v3 の `[CompositeResolver]` SG 合成。v4 は factory チェーンをランタイム合成。
- `ExcludedCustomFormatter.cs`: `ExcludeFormatterFromSourceGeneratedResolver` は批准済み削除(MessagePackAttributes.cs deviation #2)。v4 の SG は手書きフォーマッタを自動収集しないので、除外属性も除外対象の収集も存在しない。同じ批准の裏面として `GeneratedResolverPicksUpCustomResolversAutomatically` は Skip。

v3 の MapModeExecutionTests プロジェクトが持っていた「ExecutionTests 全体を FORCE_MAP_MODE で再コンパイル」するグローバル強制 map モードスイッチも v4 に対応物がなく、map 固有型(`[MessagePackObject(true)]` 系)のテストのみの縮小移植。

## 実行

```
dotnet run --project tests/MessagePack.Tests.V3CompatGenerated -c Release
```

## この移植が掘り当てたもの(2026-08-30)

- **`new` シャドウイングを SG が most-derived しか直列化しなかった**(base 側の明示 `[Key]` が黙って落ち、ワイヤ上のデータ損失)。v3 の SG は全宣言を各自のキーで直列化し、v4 のランタイム tier も(V3Compat の作業で)そう直したため、同じ型が tier によって異なるワイヤを吐く不整合でもあった。ObjectParser のメンバー発見を「名前 dedup」から「override チェーン dedup」に変更し(override は同一ストレージ、シャドウは別ストレージ)、emitter は隠された base 宣言に declaring type へのキャスト経由でアクセス、識別子は Sanitize 接尾辞で一意化。init-only / required なシャドウ base メンバーは object initializer で到達不能なので MsgPack005 で reflection tier へ。
- **ReflectionUnionFormatter が移行エイリアス属性で throw**: 「`MessagePack.UnionAttribute` という v3 名だが `UnionTagAttribute` 派生(CS0104 回避パターン)」の属性が名前優先の duck read で v3 側に誤誘導され、Key プロパティがなく失敗。名前一致でも Key が読めなければ base チェーン歩行を継続して v4 読みに落ちるよう修正(ReflectionUnionTests.MigrationAliasAttribute_ReadsAsV4ThroughTheBaseChain)。
