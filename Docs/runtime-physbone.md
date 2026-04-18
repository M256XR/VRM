# Runtime PhysBone

## Summary

最終的な原因は、`VRCPhysBone` 自体の登録失敗や grab 失敗ではなく、player runtime 側で PhysBone の演算フレームが進んでいなかったこと。

`PhysBoneManager` に chain が登録され、grab も開始できる状態でも、`VRCDynamicsScheduler.UpdateConstraints(true)` が回っていないと、見た目は「かちかち」のままになる。

## Symptoms

- PhysBone が入っているオブジェクトが固い
- grab 風の入力は入るが、つながった chain 全体が自然に動かない
- 「触れるけど戻らない」「掴んでいる一点だけ引っ張っている」ように見える

## What was confirmed

- `PhysBoneManager` の chain 自体は作成できていた
- `GetOrCreateRoot(...)` を使って current SDK の root 型に合わせる必要があった
- current SDK では `VRCPhysBoneBase.root` は `PhysBoneRootDefinition`
- `PhysBoneManager.ReleaseGrab(ChainId)` オーバーロードがある
- `VRCDynamicsScheduler.Initialize()` は `Camera.onPreCull` に hook するだけ
- `OnCameraPreCull` 自体は `FinalizeJob()` しかしていない
- 実際に演算を進めるには `VRCDynamicsScheduler.UpdateConstraints(true)` が必要

## Current implementation

### Main files

- [VrcSdkRuntimeDynamicsBootstrap.cs](D:/Projects/VRM/Assets/script/VrcSdkRuntimeDynamicsBootstrap.cs)
- [PhysBoneTouchHandler.cs](D:/Projects/VRM/Assets/script/PhysBoneTouchHandler.cs)

### Bootstrap

`VrcSdkRuntimeDynamicsBootstrap` で以下を行う。

- `DynamicsComponent.DefaultUsage = Avatar`
- `ContactBase.OnInitialize`
- `VRCPhysBoneBase.OnInitialize`
- `VRCPhysBoneColliderBase.OnPreShapeInitialize`
- `ContactManager` / `PhysBoneManager` の生成
- `PhysBoneManager.Inst.IsSDK = true`
- `PhysBoneManager.Inst.Init()`
- runtime driver の生成

### Driver

`VrcSdkRuntimeDynamicsDriver` を常駐させ、`LateUpdate` で以下を呼ぶ。

- `VRCDynamicsScheduler.UpdateConstraints(true)`

これで player runtime 側でも PhysBone の演算が進む。

## Why previous attempts were not enough

- manager を作るだけでは足りない
- chain を追加するだけでも足りない
- root を正しく与えるだけでも足りない
- grab の経路だけ作っても、scheduler が回らなければ揺れない

## Logging

通常時は verbose ログを抑えている。

`PhysBoneTouchHandler.enableVerboseLogging` を有効にすると、以下の詳細が見られる。

- AddPhysBone 前後
- AddChains 前後
- chain 数
- direct grab の候補探索

通常運用ではオフ推奨。

## Known cautions

- VRC SDK のバージョン差分で private/internal API は変わる可能性がある
- `root` の型や `ReleaseGrab` のシグネチャは SDK 更新時に再確認が必要
- runtime touch 周りは実験コードを含む

