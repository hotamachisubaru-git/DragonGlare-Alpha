# Unity 用ゲームコア

`DragonGlare.Core.csproj` は元の `Domain/`、`Data/`、ゲームサービス、保存 DTO を明示的に参照し、Unity に読み込める `netstandard2.1` DLL を生成します。MonoGame、NAudio、GDI 描画、Windows の起動 UI は参照しません。元のゲームソースは変更しません。

## ビルドと検証

リポジトリルートで実行します。

```powershell
.\UnityPort\Build-Core.ps1
dotnet test UnityPort\Core\Tests\DragonGlare.Core.Tests.csproj --configuration Release
```

DLL と必要な JSON 依存 DLL は `Assets/DragonGlareUnity/Plugins/` に出力されます。Unity でソースの最新構文をコンパイルする必要はありません。ビルドスクリプトは .NET SDK と NuGet 復元を使用します。

元の 49 個のコアファイルのうち、44 個は直接リンクします。4 個は `obj/` に互換版を生成し、`AntiCheatService` は Unity 用の API 実装を使用します。生成時に適応対象の原文が一致しなければビルドを停止します。

## 互換性の適応

- `ProtectedInt` の rotate 演算を標準整数演算に置き換えます。改ざん検出・再鍵化は元の実装を保持します。
- 保存データの SHA256/HMAC、確定 JSON の順序、エスケープ、バージョン 10 の署名構造を保持します。JSON には `System.Text.Json` 8.0.6 を使用します。
- Windows CurrentUser DPAPI をネイティブ API で呼び出します。元と同じスロット別 entropy を使うので、同じ Windows ユーザーで元ゲームの保存データを読み取れます。
- 一時ファイルの確定は新規なら `File.Move`、既存なら `File.Replace` を使います。
- コア単独使用時の保存・設定フォルダーは `DragonGlareUnity` です。元の `DragonGlareAlpha` フォルダーへ自動保存しません。
- Unity 側は `new SaveService(Path.Combine(Application.persistentDataPath, "saves"))` と `new LaunchSettingsService(設定ファイルの絶対パス)` を使用します。元セーブの移行は別途明示的に行います。
- `AntiCheatService.TryDetectStartupViolation(out string)` と `TryDetectViolation(out string)` は Unity 用では違反を返しません。Unity Editor のデバッガーや外部プロセスを誤検出する全体スキャンを行わず、ゲームデータの `ProtectedInt` と保存の DPAPI・署名検証を保持します。

保存の暗号化は Windows 専用です。他 OS 向けに無暗号の保存へ切り替えることはありません。

テストプロジェクトは既存 xUnit テストをこのコアに対して実行し、元 MonoGame アセンブリとの双方向保存互換、署名改ざん拒否、既存保存の置き換え、設定ファイルの保存先を追加検証します。.NET 10 上のテスト成功と Unity Editor 上の読み込み・プレイ検証は別の結果です。
