# Game assets

既存のMonoGame版で使うアセットです。Unity版の起動シーンと生成アセットは `DragonGlareUnity/` にあります。

- Sprites: characters, NPCs
- Tiles: map chipsets
- UI: windows, cursors
- Portraits: dialogue illustrations and reference art

Unity版は元のUI画像、フォント、会話立ち絵、音楽を `DragonGlareUnity/Resources/` にコピーして利用します。元ファイルは変更しません。3D地形・キャラクター・演出は `UnityPort/Runtime/DragonGlareWorld.cs` で生成します。

移植版の起動・再ビルド・セーブ取り込みは [UnityPort/README.md](../UnityPort/README.md) を参照してください。
