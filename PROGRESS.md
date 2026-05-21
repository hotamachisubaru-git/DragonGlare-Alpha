# DragonGlareAlpha 進捗メモ

更新日: 2026-05-22

## 現状サマリー

- `DragonGlareAlpha` は `DragonGlareAlpha.csproj` を基準に管理する MonoGame / DesktopGL 製 Windows 向け RPG 風プロトタイプです。
- リポジトリはアーカイブ済みで、現在はローカル保守用の記録として扱います。
- 現在のアプリ表示名は `DragonGlare Beta`、現在の `AppVersion` は `1.0.0` です。
- 画面遷移は「起動設定 → モード選択 → 言語選択 → 名前入力 → フィールド → バトル / ショップ / 銀行 / ダンジョン」まで実装済みです。
- 武器、むねあて、あたま、こて、レギンス、ブーツの装備スロットを保持し、戦闘計算とセーブデータに反映します。
- 起動設定は表示モード、起動時確認、アプリ音量を保存し、次回起動時に復元します。

## 直近の主な更新

### DragonGlare Beta 1.0.0

- アプリ表示名、製品名、アセンブリ名を `DragonGlare Beta` に変更。
- `AppVersion` を `1.0.0` に更新し、Beta 版として整理。
- 起動設定に音量保存を追加し、終了時に現在のアプリ音量を保存、起動時に復元するよう変更。
- 呪文、武器、防具、消耗品、敵データを `Data/GameContent.cs` から専用ファイルへ分割。
- 消耗品、呪文、装備、敵ステータス、報酬量を Beta 向けに再調整。
- `BattleService` をアクション解決、補助処理、状態異常処理の partial クラスへ分割。
- `ProgressionService` の経験値、レベルアップ、ドロップ、借入ペナルティ処理を報酬用 partial クラスへ分割。
- バトル、戦闘結果表示、装備、ショップ、銀行周辺の UI 状態ヘルパーを分離。
- フィールドイベント取得とバトルメッセージ表示を改善し、戦闘選択メッセージの生成処理を整理。
- 未使用コード調査用の `tools/FindUnused` と補助 PowerShell スクリプトを追加。

### Alpha 1.4 系

- フィールド遷移とイベント定義を `FieldContent` に分離。
- フィールドからダンジョンへ移動できる入口と、ダンジョンから戻る遷移を追加。
- フィールド上の看板と宝箱イベントを追加し、宝箱報酬と完了済みイベントの保存に対応。
- `SpriteFontPlus` 依存を外し、TTF フォントを直接描画する方式に変更。
- 戦闘結果メッセージの自動ステップ送りと、`BattleVisualCue` ごとの個別演出を追加。
- `AntiCheatService` の起動時検出を同期化し、実行中スキャンをスレッドセーフ化。
- フィールド移動のアニメーションを短縮し、通常の進捗保存を入力更新フレームから遅延実行するよう変更。
- 名前入力画面の日本語テーブルに小文字かなを追加し、行数増加に合わせて配置を調整。

## 実装済み機能

- フィールド探索、イベント会話、マップ遷移。
- ハブ、城、フィールド、ダンジョンの 4 マップ。
- ランダムエンカウントによるバトル。
- 攻撃、呪文、防御、どうぐ使用、装備変更、逃走。
- 毒、睡眠、状態異常回復。
- 勝利報酬、レベルアップ、敵ドロップ。
- ショップでの購入、売却、装備の自動更新。
- 銀行での預け入れ、引き出し、借入、利息、借入ペナルティ。
- セーブ / ロード、3 スロット管理、コピー、削除、旧セーブデータ移行。
- DPAPI、署名、完了済みフィールドイベントによる保存データ保護。
- キーボードとゲームパッド入力。
- デバッガ、不正ツール、保護値改ざん検出。

## Beta 構成メモ

- `Data/GameContent.cs` はゲームデータへの中央アクセスポイントです。
- 呪文は `Data/Spells.cs`、武器と防具は `Data/Equipment.cs`、消耗品は `Data/Consumables.cs`、敵は `Data/Enemies.cs` に分離されています。
- フィールド遷移とフィールドイベントは `Data/FieldContent.cs` に分離されています。
- バトル処理は `Services/BattleService*.cs` の partial クラス群に分割されています。
- 戦闘報酬処理は `Services/ProgressionService.Rewards.cs` に分離されています。
- 画面状態の補助処理は `UI/State/DragonGlare.*.cs` に分割されています。
- 起動設定は `%LOCALAPPDATA%\DragonGlareAlpha\launch_settings.json` に保存します。
- セーブデータは `%APPDATA%\DragonGlareAlpha\slot1.sav` から `slot3.sav` に保存します。

## 残タスク候補

- アーカイブ済みのため、現在の進行中タスクはありません。
- 今後作業する場合は、ローカル保守、記録更新、必要最小限の不具合修正に限定します。

## 検証状況

- 今回の `PROGRESS.md` 更新はドキュメントのみのため、ビルドとテストは未実行です。
- コード変更を行う場合は、通常どおり `dotnet test DragonGlareAlpha.Tests\DragonGlareAlpha.Tests.csproj` を実行します。

## 現在のフォルダ構成

```text
Assets/                実行時アセット
Content/               MonoGame Content 用素材
Data/                  ゲームデータ
Domain/                ドメインモデル
Persistence/           保存処理 / セーブデータ移行
Security/              チート対策
Services/              ロジックサービス
UI/                    レンダリング、入力、画面状態、音声、起動処理
DragonGlareAlpha.Tests/ xUnit による自動テスト
tools/                 調査用補助ツール
```
