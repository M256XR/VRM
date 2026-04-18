# Build And Ops

## Project context

- Unity: `2022.3.22f1`
- Android live wallpaper project
- project root:
  - `D:/Projects/VRM`

## Important paths

- APK output:
  - `D:/Projects/VRM/Exportapk/wallpaper.apk`
- build/export logs:
  - `D:/Projects/VRM/BuildSmoke/`
- editor perf logs:
  - `D:/Projects/VRM/Logs/EditorPerf/`
- build script:
  - `D:/Projects/VRM/Scripts/build_android.ps1`

## Recommended build flow

基本はビルドスクリプトを使う。

```powershell
powershell -ExecutionPolicy Bypass -File D:\Projects\VRM\Scripts\build_android.ps1
```

主な処理:

- Unity batch export を `BuildSmokeTest.ExportAndroidProject` で実行
- export 先フォルダを毎回分けて `local.properties` ロックを避ける
- Gradle `assembleDebug` を Unity 同梱 JDK で実行
- 生成 APK を `Exportapk/wallpaper.apk` にコピー
- Unity / Gradle ログを `BuildSmoke/` に保存

export フォルダ名を固定したい時:

```powershell
powershell -ExecutionPolicy Bypass -File D:\Projects\VRM\Scripts\build_android.ps1 -ExportFolderName AndroidExportManual
```

## VRC SDK integration

今は DLL 持ち込みではなく、`Packages/com.vrchat.base` と `Packages/com.vrchat.avatars` を embedded package として入れている。

関連ファイル:

- `Packages/vpm-manifest.json`
- `Packages/packages-lock.json`

## Android side notes

- `MainActivity` を launcher にしている
- portrait 固定
- Unity preview を見ながらネイティブ設定を触れる構成

関連:

- [AndroidManifest.xml](D:/Projects/VRM/Assets/Plugins/Android/AndroidManifest.xml)
- [MainActivity.java](D:/Projects/VRM/Assets/Plugins/Android/VRMWallpaperNative.androidlib/src/main/java/com/oreoreooooooo/VRM/MainActivity.java)

## Current baseline summary

- PhysBone runtime 演算は復旧済み
- AssetBundle 表示は改善済み
- パフォーマンスは端末依存があるので、設定で調整できる前提で運用
