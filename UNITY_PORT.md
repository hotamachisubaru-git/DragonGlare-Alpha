# DragonGlare Alpha Unity Port

このブランチではリポジトリ直下を Unity プロジェクトとして扱います。

- Unity 2022.3 系を想定しています。
- `Assets/Scripts` にゲーム本体を移植しました。
- 起動用の `MonoBehaviour` はコード側で自動生成されるため、空のシーンでも再生できます。
- セーブ先は `Application.persistentDataPath/DragonGlareAlpha` です。

## 現時点の移植範囲

- モード選択、言語選択、名前入力
- セーブ/ロード
- フィールド移動、会話、回復イベント、マップ遷移
- ランダムエンカウント、バトル、ショップ
- BGM/SE の基本切り替え

## 既知の差分

- WinForms 固有の描画や起動ダイアログは Unity 向けに再実装しています。
- Unity では DPAPI を使わず、署名付き JSON セーブに置き換えています。
