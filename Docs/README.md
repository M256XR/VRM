# VRM Wallpaper Notes

このフォルダは、このプロジェクトで実際に詰まった点と、その時点で有効だった実装・知見を残すためのメモ置き場です。

## Files

- `runtime-physbone.md`
  - VRC PhysBone を Android player runtime で動かすための知見
- `rendering-and-performance.md`
  - AssetBundle / lilToon / 描画負荷まわりの知見
- `build-and-ops.md`
  - ビルド、APK、bundle、運用時のメモ

## Current baseline

- PhysBone は runtime で動作する
- AssetBundle 側の見た目崩れは大きく改善済み
- heavy なモデルでは Android ホーム UI 合成時の負荷に注意

## Important commits

- `6727ebc3`
  - runtime PhysBone scheduling 修正

