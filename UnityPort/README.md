# DragonGlare Unity 3D 版

既存の640×480 UI、文言、メニュー構成を再利用し、フィールドと戦闘背景をUnityの3D描画へ移植しています。MonoGame版のソースと元アセットは維持しています。

## 起動

1. Unity 6000.7.0b2でこのフォルダを開きます。
2. `Assets/DragonGlareUnity/Scenes/DragonGlare3D.unity` を開き、Playを押します。
3. メニューの `Tools > DragonGlare > Open 3D RPG` からも開けます。

コードを変更したときは、リポジトリルートで以下を実行してDLLとコピーしたアセットを更新します。

```powershell
.\UnityPort\Build-Unity.ps1
```

エディターがDLLを再読み込みしてからPlayを実行してください。DLLの生成に.NET SDKが必要ですが、Unityプレイヤーに.NET 10やMonoGameは不要です。

Unity用のDLLとResourcesはリポジトリに含まれています。新しいcloneでも、上のコマンドでDLLを再生成できます。元のUI画像・音楽・効果音・フォントがローカルにある場合はResourcesを更新し、元ファイルがない場合は同名のResourcesをそのまま使用します。元ファイルと必要なResourcesの両方がない場合は、対象のパスを示してビルドを停止します。

検証済みWindowsプレイヤーは `.release/DragonGlareUnity-20261014-1052/DragonGlare3D.exe` です。同じフォルダのData・DLL類も必要です。

左右の移動と起動ロゴを修正した最新版は `.release/DragonGlareUnity-20261014-1100/DragonGlare3D.exe` です。以前のビルドは残しています。

Unityが `Untitled` を表示している場合は、`Tools > DragonGlare > Open 3D RPG` でゲーム用シーンを開いてください。停止中のHierarchyには起動用オブジェクトが1つあります。3D地形・キャラクター・カメラはPlay時に生成されます。

## 操作

| 操作 | キーボード | Xbox形式のパッド |
|---|---|---|
| 移動・選択 | 矢印 / WASD | 十字キー / 左スティック |
| 決定・会話 | Enter / Z | A / Y |
| 戻る | Esc | B |
| 状態表示・サブメニューから戻る | X | X |
| 名前の削除 | Backspace | X |
| 元版の戦闘ショートカット | B | LB |
| 元版のショップショートカット | V | RB |
| 全画面切替 | F11 | — |

名前選択表の「おわり」で確定し、セーブ枠を選んで探索を開始します。導入の語りはEnter/Zでスキップできます。パッドの軸・ボタン割り当てはXbox形式を基準にしています。

## 移植した内容

- 4マップの地形、主人公、NPC、宝箱、入口、回復泉を3D表示。
- 既存の12種類の敵に対応する立体モデルと戦闘カメラ。
- 攻撃、呪文、防御、道具、装備、逃走、状態異常、報酬、レベルアップ。
- ショップ売買、装備6部位、銀行、借入・利息。
- 名前入力、言語選択、3セーブ枠、コピー・削除、完了済みイベント。
- 日本語フォント、既存のUI画像、会話立ち絵、BGM、効果音。
- カメラ追従、影、魔法・回復・被弾・撃破の3D演出。

3Dモデルはコードで生成する試作用モデルです。マップの通行判定とイベント条件、戦闘計算は元コードで判定します。ダンジョンの地形は元版と同じ城マップを使い、3D側では照明と色を変えています。

## 保存と元版からの取り込み

Unity版は `Application.persistentDataPath/saves` に保存し、起動設定も同じUnity専用フォルダへ保存します。元版の `%APPDATA%/DragonGlareAlpha` は通常プレイで変更しません。

`Tools > DragonGlare > Import MonoGame Saves` で元版の保存フォルダを指定できます。署名を検証してから、Unity側の空いている同番号のスロットだけに取り込みます。元ファイルと既存のUnityセーブは上書きしません。

旧形式の `save1.sav` は、元のslot1が存在しない場合に一時コピーを移行し、Unityの空きslot1へ取り込めます。元の旧セーブも残します。

保存形式v10とWindowsユーザーのDPAPI保護を維持しているため、保存・取り込みの対象はWindowsです。保護値と署名検証を維持しています。外部ツール・デバッガーの検出はUnity Editorで誤検出するため移植版では無効にしています。

## 構成

- `Core/`: 元のDomain/Data/Services/Persistenceを参照し、互換性が必要なAPIだけ生成コードで置換する`netstandard2.1`プロジェクト。
- `Runtime/`: 元の状態更新と画面配置を再利用するセッション、Unity描画、3D地形・演出、入力・音声。
- `Prepare-Runtime.ps1`: 元画面コードからUnity用の描画接続だけ生成。変更前提が合わない場合はビルドを停止。
- `../Assets/DragonGlareUnity/`: 起動シーン、Unityスクリプト、生成DLL、コピーしたResources。

UnityがサポートするAPIに合わせ、最新C#構文を含む元コードを外部SDKでDLL化しています。[Unityの.NET互換性](https://docs.unity.com/ja-jp/engine/6000.0/manual/scripting/environment-and-tools/overview-of-dot-net-in-unity/dotnet-profile-support)、[管理プラグイン](https://docs.unity.com/en-us/engine/6000.0/manual/programming-environment/plug-ins)を参照。

## 検証

```powershell
dotnet test DragonGlareAlpha.Tests\DragonGlareAlpha.Tests.csproj
dotnet test UnityPort\Core\Tests\DragonGlare.Core.Tests.csproj
.\UnityPort\Validate-Unity.ps1
```

最後のコマンドは`.codex-build`内の新しい検証用Unityプロジェクトでプレイヤーをビルドします。開いている元プロジェクトや既存セーブを操作せず、移植コードのインポートとWindowsビルドを検証します。

2026-10-14の結果: 元版91/91、互換コア96/96、Unity Windowsビルド警告・エラー0。通常プレイヤーの86項目と14画像を検証し、実行時エラー0です。[Windowsビルド結果](Validation/windows-build-20261014.json)、[Unity実行結果](Validation/player-smoke-20261014.json)を参照してください。画像は `.codex-build/player-smoke-final-20261014-1052/` にあります。実ゲームパッド、音声の聴感、長時間プレイは未検証です。

左右修正版: 4マップ×矢印・WASDの32ケースで、実移動のタイル、画面上の進行方向、主人公の位置と向きを検証。222項目成功、16画像、実行時エラー0です。Windowsビルドは警告・エラー0で、`splashScreenEnabled` と `unityLogoEnabled` が両方falseです。[移動検証](Validation/player-directions-20261014.json)、[ロゴ非表示のビルド結果](Validation/windows-directions-splash-20261014.json)を参照してください。

生成したプレイヤーに `-dragonGlareSmoke -dragonGlareSmokeOutput <新しい検証フォルダ>` を渡すと、Unity上のキー操作・画面遷移・保存・ショップ・銀行・戦闘と3D画面を自動検証し、レポートと画像を出力します。自動検証では隔離した保存先を使用します。

```powershell
.\UnityPort\Run-PlayerSmoke.ps1 -PlayerPath '<検証ビルドのDragonGlare3D.exe>'
```

画面の検証は、バックグラウンド実行を有効にした通常のプレイヤーで行います。バッチプレイヤーではIMGUIが描画されず黒画像になるため、画像の画素・明暗差・色の変化も検証します。
