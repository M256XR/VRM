# Rendering And Performance

## Summary

描画崩れの主因は「lilToon が完全に死んでいる」ケースと、「ちゃんと描けるようになった結果として重い」ケースが分かれていた。

後半の問題は shader 読み込み不全より、Android ホーム UI とライブ壁紙 3D の合成時の GPU 余裕不足の寄与が大きい。

## AssetBundle side findings

- lilToon が壊れている時はピンクや fallback になりやすい
- 直った後は fur / outline / gem / transparent が正しく出る代わりに重くなる
- polygon 数だけでは体感の重さを説明しきれない
- renderer 数、material 数、pass 数、半透明系が効いている可能性が高い

## Practical observations

- VRM より AssetBundle の方が重く感じやすい
- 特にホーム復帰、通知バー、アプリ一覧など Android のシステム UI 合成時に差が出やすい
- 起動直後はそこまで悪くなくても、アプリを開いてホームに戻るとカクつきやすい
- ただし再現は完全固定ではなく、端末状態やバックグラウンド状況でも揺れる

## lilToon side

### Kept expressions

残したい表現として確認していたもの:

- outline
- gem
- fur
- transparent
- emission
- lilToon material color change

### Effective optimization direction

VRC 側で bundle 専用に軽くするのがいちばん現実的だった。

実際に効いた変更例:

- 体の大半の material を `lilToonLite` 化
- fur layer を 4 -> 1
- gem の一部裏面描画を抑制
- 不要オブジェクト削除

この方向は「見た目を大きく崩さずに軽くする」バランスが比較的よかった。

## What did not look like the main culprit

- fur だけが主犯、とは言い切れなかった
- polygon 数だけでもなさそうだった
- targetFrameRate の明示だけでは決定打になりにくかった

## Current interpretation

今の重さは、

- モデル描画
- 半透明
- gem
- outline
- material/pass
- Android UI 合成

の合算で出ていると見るのが自然。

## Editor profiling

Editor 上の 10 秒前後ロギングで VRM / bundle を比較した。

ログ出力先の例:

- `Logs/EditorPerf/`

ただし Editor の数字はそのままスマホ実機と一致しない。  
傾向比較用として使うのがよい。

## Current status

- 現状は「だいぶまし」「許容範囲寄り」
- 無理にさらに触って崩すより、今の状態を baseline として固定する方がよい

