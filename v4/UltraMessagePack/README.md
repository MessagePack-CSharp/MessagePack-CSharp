# UltraMessagePack

DisassembleBenchmark の最適化ループ(JIT 逆アセンブル解析+分岐予測カウンタ計測)で収束させたプリミティブを核に組み上げた、実験的な最速志向 MessagePack シリアライザコア。

## 設計原理

ベンチマークループで実証した3つの原理をレイヤー全体に適用している:

1. **整数エンコードは「fixint 1比較の先行 + 分岐レス本体」**(Hybrid)。分岐ミス1回 ≈ 13〜18 サイクルに対し、lzcnt+表引きの分岐レス変換はどんな値分布でもミス0。fixint 先行は典型データで 4 サイクル/値まで下がり、かつ本体から len==1 を構造的に排除するのでシフト量のラップ(C# の `<<` は下位ビットマスク)も回避できる
2. **Writer は容量を先に保証し、プリミティブは無条件ワイド書き込み**。`Reserve(worst-case)` 後は境界チェックゼロ・分岐ゼロの `movbe` 1発で書く(bytesWritten を超えるスクラッチ書き込みは Reserve 窓内で許容)
3. **要素単位のデコードはループ跨ぎ依存チェーン(code→len→次アドレス ≈ 15サイクル)が床**であり、これを破るには API 形状を変えるしかない → `int[]` 専用フォーマッターは fixint 連続チャンクを SIMD で一括処理する(下記)

## Int32ArrayFormatter の SIMD バッチ

- **Serialize**: 16要素を一括判定(全部 fixint 範囲か)→ 真なら int32×16 → byte×16 に narrow して 16 バイト一括書き込み。チャンクが混在ならその16要素だけスカラー処理して次のチャンクで再試行
- **Deserialize**: コードバイト16個を `(b+0x20) <= 0x9f` のベクトル比較で一括分類 → 全部 fixint なら byte×16 → int32×16 に符号拡張して一括ストア
- **3階層ディスパッチ**(`IsSupported` は JIT 定数なので実行時コストなし):
  1. AVX-512: `vpcmpud`+`vpmovdb` / `vpcmpleub`+`vpmovsxbd`(各1命令)
  2. ポータブル `Vector128`: ARM NEON / SSE2 / AVX2-only x86 で動作。判定は Max 畳み込み+`LessThanOrEqualAll`、変換は `Narrow`×3 / `Widen`×3(NEON では `xtn`/`sshll` 系)。範囲判定通過後の値は [-32,127] なので飽和/切詰めナローのプラットフォーム差が結果に影響しない、というのが安全性の根拠
  3. スカラー(それでも分岐レス)
- 3階層すべて同一テストスイートで検証(`DOTNET_EnableAVX512F=0` / `DOTNET_EnableHWIntrinsic=0` で下位階層を強制)。なおこのマシン(Strix Point、512bit がダブルポンプ実行の Zen 5 モバイル)では AVX-512 階層とポータブル階層の実測差はほぼゼロ — ネイティブ 512bit データパスのデスクトップ Zen 5 / Intel P-core では AVX-512 階層が優位になる見込み

## Write プリミティブ層(完全実装)

全プリミティブが `ref byte` + 書き込み長返却の形で、必要バイト数の事前確保(`Writer.Reserve`)と引き換えに境界チェック・条件ストアゼロ:

- 整数(int8〜64 / uint8〜64): fixint 1比較先行 + lzcnt→表→`movbe` の分岐レス本体
- nil / bool(`0xc2 | boolバイト` で分岐レス)/ float / double
- array / map / str / bin ヘッダ: **整数書き込みと同型**(fix形式=値埋め込み+2/3/5バイト幅)なので、`{len, header, fixマスク}` の33エントリ表を差し替えるだけの共有分岐レスコア `WriteHeaderCore` に統合
- ヘッダには fix 形式の1比較先行を追加。**呼び出しサイトで定数の小カウント**(POCO フォーマッターの固定フィールド数など)では完全予測される分岐が表引きより速い、という計測結果に基づく(整数と同じ Hybrid 原則)

検証は MessagePack-CSharp の `MessagePackWriter` を直接オラクルにしたバイト一致(境界値×全プリミティブ)。なお本家 `WriteStringHeader(int.MaxValue)` はペイロード分の事前確保で OverflowException になるため、巨大値のみ仕様バイト列と直接比較している。

## 文字列書き込み(クラス安定性戦略)

MessagePack-CSharp の「文字数からヘッダクラスを推定して投機エンコード、外れたら memmove」を土台に、**クラス安定性の保証**を加えた形。byteCount は必ず [L, 3L] に入るので、class(L) == class(3L) となる長さ(L ∈ [0..10], [32..85], [256..21845])では推定が**構造的に必中**になり、事後の移動判定ごと不要。跨ぎ域(L ∈ [11..31], [86..255], [21846..65535])のみ投機+memmove(ASCII は byteCount == L なので必中、非ASCIIだけが移動を踏む)。GetByteCount の2パスは ≥64K 文字(3倍確保が MB 級に効く領域)だけ。

実測(vs MessagePack-CSharp、文字列単体 Serialize、6長×ASCII/日本語の12ケース): 全ケースで同等〜27%高速(比 0.73〜1.00)。この戦略は `MessagePackPrimitives.UnsafeWriteString`(契約: 宛先 3L+5 バイト)として実装され、Writer は「Reserve+委譲」の薄いラッパー。GetByteCount による正確確保(≥64K文字)だけが Writer 側のメモリポリシーとして残る。

## 対応範囲(実験的スコープ)

- プリミティブ: int8/16/32/64, uint8/16/32/64, bool, float, double, string(UTF-8), byte[](bin)
- コレクション: `T[]`, `List<T>`, `Dictionary<K,V>`, `Nullable<T>`(リフレクションで自動解決)
- POCO: `IMessagePackFormatter<T>` の手書き実装を `Register`(コード生成は未実装)
- 読み取りは非最小エンコード(int64/uint64 に小さい値)も受理。uint64 の符号エイリアス(`FF×8` → long 再解釈 -1)は符号検査で防御
- 未対応: ext/timestamp、LZ4、`ReadOnlySequence` 入力、Typeless

## 正しさ

`UltraMessagePack.Tests` で MessagePack-CSharp をオラクルとして検証:

- int32(境界値近傍+ランダム10万値)・int64・uint64・bool・double・string(絵文字/サロゲート/サイズ境界)の**出力バイト完全一致**
- `int[]` 14サイズ×3分布のバイト一致+相互デシリアライズ(こちら→オラクル、オラクル→こちら)
- SIMD チャンク境界: 48要素中の任意位置で fixint 連続が破れるケースを全オフセット試験
- 非最小エンコード受理と uint64 符号罠の拒否

```
dotnet test UltraMessagePack.Tests -c Release
```
