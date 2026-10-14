using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DragonGlareAlpha.Persistence;
using DragonGlareUnity;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class DragonGlareEditor
{
    public const string ScenePath = "Assets/DragonGlareUnity/Scenes/DragonGlare3D.unity";
    private const string ResourceRoot = "Assets/DragonGlareUnity/Resources";

    static DragonGlareEditor()
    {
        // Updating only changed properties avoids dirtying InputManager on
        // every domain reload and preserves all unrelated project axes.
        EditorApplication.delayCall += EnsureConfiguration;
    }

    [MenuItem("Tools/DragonGlare/Open 3D RPG")]
    public static void Open3DRpg()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EnsureScene();
        EnsureConfiguration();
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    [MenuItem("Tools/DragonGlare/Build Windows")]
    public static void BuildWindows()
    {
        try
        {
            ValidateAndBuild();
            EditorUtility.DisplayDialog("DragonGlare", "Windows build completed.\n" + DefaultBuildPath, "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("DragonGlare build failed", exception.Message, "OK");
        }
    }

    [MenuItem("Tools/DragonGlare/Configure Pixel UI Textures")]
    public static void ConfigureTextureImports()
    {
        if (!AssetDatabase.IsValidFolder(ResourceRoot)) return;
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ResourceRoot }))
        {
            var importer = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as TextureImporter;
            if (importer == null) continue;
            var changed = importer.filterMode != FilterMode.Point || importer.mipmapEnabled ||
                importer.textureCompression != TextureImporterCompression.Uncompressed ||
                importer.npotScale != TextureImporterNPOTScale.None || importer.wrapMode != TextureWrapMode.Clamp;
            if (!changed) continue;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }
    }

    public static void EnsureInputAxes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var manager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset").FirstOrDefault();
        if (manager == null) return;
        var serialized = new SerializedObject(manager);
        var axes = serialized.FindProperty("m_Axes");
        if (axes == null || !axes.isArray) return;
        ConfigureAxis(axes, "DragonGlarePadX", 0, false);
        ConfigureAxis(axes, "DragonGlarePadY", 1, true);
        ConfigureAxis(axes, "DragonGlareDpadX", 5, false);
        ConfigureAxis(axes, "DragonGlareDpadY", 6, false);
        if (serialized.hasModifiedProperties) serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void EnsureConfiguration()
    {
        EnsureInputAxes();
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (PlayerSettings.SplashScreen.showUnityLogo) PlayerSettings.SplashScreen.showUnityLogo = false;
        if (PlayerSettings.SplashScreen.show) PlayerSettings.SplashScreen.show = false;
        var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset").FirstOrDefault();
        if (settings == null) return;
        var serialized = new SerializedObject(settings);
        var included = serialized.FindProperty("m_AlwaysIncludedShaders");
        if (included == null || !included.isArray) return;
        foreach (var name in new[] { "Standard", "Sprites/Default" })
        {
            var shader = Shader.Find(name);
            if (shader == null) throw new InvalidOperationException("Required DragonGlare shader was not found: " + name);
            var exists = false;
            for (var index = 0; index < included.arraySize; index++)
            {
                if (included.GetArrayElementAtIndex(index).objectReferenceValue == shader)
                {
                    exists = true;
                    break;
                }
            }
            if (exists) continue;
            included.InsertArrayElementAtIndex(included.arraySize);
            included.GetArrayElementAtIndex(included.arraySize - 1).objectReferenceValue = shader;
        }
        if (serialized.hasModifiedProperties) serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureAxis(SerializedProperty axes, string name, int axis, bool invert)
    {
        SerializedProperty item = null;
        for (var index = 0; index < axes.arraySize; index++)
        {
            var candidate = axes.GetArrayElementAtIndex(index);
            if (candidate.FindPropertyRelative("m_Name").stringValue == name)
            {
                item = candidate;
                break;
            }
        }
        if (item == null)
        {
            axes.InsertArrayElementAtIndex(axes.arraySize);
            item = axes.GetArrayElementAtIndex(axes.arraySize - 1);
        }
        SetString(item, "m_Name", name);
        foreach (var field in new[] { "descriptiveName", "descriptiveNegativeName", "negativeButton", "positiveButton", "altNegativeButton", "altPositiveButton" })
            SetString(item, field, string.Empty);
        SetFloat(item, "gravity", 0f);
        SetFloat(item, "dead", 0.25f);
        SetFloat(item, "sensitivity", 1f);
        SetBool(item, "snap", false);
        SetBool(item, "invert", invert);
        SetInt(item, "type", 2);
        SetInt(item, "axis", axis);
        SetInt(item, "joyNum", 0);
    }

    private static void SetString(SerializedProperty item, string field, string value)
    {
        var property = item.FindPropertyRelative(field);
        if (property.stringValue != value) property.stringValue = value;
    }
    private static void SetFloat(SerializedProperty item, string field, float value)
    {
        var property = item.FindPropertyRelative(field);
        if (!Mathf.Approximately(property.floatValue, value)) property.floatValue = value;
    }
    private static void SetBool(SerializedProperty item, string field, bool value)
    {
        var property = item.FindPropertyRelative(field);
        if (property.boolValue != value) property.boolValue = value;
    }
    private static void SetInt(SerializedProperty item, string field, int value)
    {
        var property = item.FindPropertyRelative(field);
        if (property.intValue != value) property.intValue = value;
    }

    private static void EnsureScene()
    {
        if (File.Exists(ScenePath)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        AssetDatabase.Refresh();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            var root = new GameObject("DragonGlare 3D RPG");
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<DragonGlareBootstrap>();
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("Could not save the DragonGlare 3D scene.");
        }
        finally { EditorSceneManager.CloseScene(scene, true); }
    }

    [MenuItem("Tools/DragonGlare/Import MonoGame Save Copies")]
    public static void ImportMonoGameSaveCopies()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("DragonGlare", "Stop Play mode before importing saves.", "OK");
            return;
        }
        var sourceRoot = EditorUtility.OpenFolderPanel("Select MonoGame save folder",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DragonGlareAlpha"), string.Empty);
        if (string.IsNullOrEmpty(sourceRoot)) return;
        var destinationRoot = Path.Combine(Application.persistentDataPath, "saves");
        if (string.Equals(Path.GetFullPath(sourceRoot), Path.GetFullPath(destinationRoot), StringComparison.OrdinalIgnoreCase))
        {
            EditorUtility.DisplayDialog("DragonGlare", "Select the original MonoGame save folder.\nThe Unity save folder was selected.", "OK");
            return;
        }
        var source = new SaveService(sourceRoot);
        var destination = new SaveService(destinationRoot);
        var stagingRoot = Path.Combine(destinationRoot, ".import-" + Guid.NewGuid().ToString("N"));
        var messages = new List<string>();
        try
        {
            var staged = new SaveService(stagingRoot);
            for (var slot = 1; slot <= SaveService.SlotCount; slot++)
            {
                var destinationPath = destination.GetSlotPath(slot);
                if (File.Exists(destinationPath) || File.Exists(destinationPath + ".tmp"))
                {
                    messages.Add("Slot " + slot + ": existing Unity save preserved.");
                    continue;
                }
                if (!source.TryLoadSlot(slot, out var data))
                {
                    var legacyPath = Path.Combine(sourceRoot, "save1.sav");
                    if (slot != 1 || File.Exists(source.GetSlotPath(slot)) || !File.Exists(legacyPath))
                    {
                        messages.Add("Slot " + slot + ": " + source.LastFailureReason + ".");
                        continue;
                    }
                    // Legacy migration removes its input. Migrate a temporary
                    // copy so the original MonoGame save always survives.
                    Directory.CreateDirectory(stagingRoot);
                    var legacyCopy = Path.Combine(stagingRoot, "legacy-source.sav");
                    File.Copy(legacyPath, legacyCopy);
                    if (!staged.TryMigrateLegacySave(legacyCopy, slot) || !staged.TryLoadSlot(slot, out data))
                    {
                        messages.Add("Slot 1: legacy save could not be validated.");
                        continue;
                    }
                }
                staged.SaveSlot(slot, data);
                if (!staged.TryLoadSlot(slot, out _))
                    throw new IOException("Imported slot validation failed: " + slot);
                Directory.CreateDirectory(destinationRoot);
                // The two-argument Move rejects an existing destination even
                // if it appeared after the earlier existence check.
                File.Move(staged.GetSlotPath(slot), destinationPath);
                messages.Add("Slot " + slot + ": imported a validated copy.");
            }
        }
        catch (Exception exception)
        {
            messages.Add("Import stopped: " + exception.Message);
            Debug.LogException(exception);
        }
        finally
        {
            var resolvedStaging = Path.GetFullPath(stagingRoot);
            var expectedParent = Path.GetFullPath(destinationRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (resolvedStaging.StartsWith(expectedParent, StringComparison.OrdinalIgnoreCase) && Directory.Exists(resolvedStaging))
                Directory.Delete(resolvedStaging, true);
        }
        EditorUtility.DisplayDialog("DragonGlare save import", string.Join("\n", messages) + "\n\nUnity saves: " + destinationRoot, "OK");
    }

    private static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    private static string DefaultBuildPath => Path.Combine(ProjectRoot, ".codex-build", "unity-validation", "DragonGlare3D.exe");

    // Invoke with -executeMethod DragonGlareEditor.ValidateAndBuild. This
    // verifies import/scene/build; player smoke and screenshots are separate.
    public static void ValidateAndBuild()
    {
        var validation = new ValidationReport
        {
            unityVersion = Application.unityVersion,
            scenePath = ScenePath,
            outputPath = GetArgument("-dragonGlareBuildPath") ?? DefaultBuildPath,
            result = "Failed",
            messages = Array.Empty<string>()
        };
        try
        {
            EnsureScene();
            EnsureConfiguration();
            ConfigureTextureImports();
            validation.splashScreenEnabled = PlayerSettings.SplashScreen.show;
            validation.unityLogoEnabled = PlayerSettings.SplashScreen.showUnityLogo;
            if (validation.splashScreenEnabled || validation.unityLogoEnabled)
                throw new InvalidOperationException("The Unity startup splash must be disabled in this build.");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                throw new InvalidOperationException("Windows build support is not installed in this Unity editor.");
            if (Resources.Load<UnityEngine.Font>("DragonGlare/Fonts/JF-Dot-ShinonomeMin14") == null)
                throw new InvalidOperationException("The original Japanese UI font is missing. Run UnityPort/Build-Unity.ps1 first.");
            ValidateScene();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(validation.outputPath)));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = validation.outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            validation.result = report.summary.result.ToString();
            validation.passed = report.summary.result == BuildResult.Succeeded;
            validation.errorCount = report.summary.totalErrors;
            validation.warningCount = report.summary.totalWarnings;
            validation.totalSizeBytes = (long)report.summary.totalSize;
            validation.durationSeconds = report.summary.totalTime.TotalSeconds;
            validation.messages = report.steps.SelectMany(step => step.messages)
                .Where(message => message.type == LogType.Error || message.type == LogType.Exception || message.type == LogType.Warning)
                .Select(message => message.content).ToArray();
        }
        catch (Exception exception)
        {
            validation.errorCount++;
            validation.messages = new[] { exception.ToString() };
            Debug.LogException(exception);
        }
        finally
        {
            var reportPath = Path.Combine(ProjectRoot, ".codex-build", "unity-validation", "report.json");
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, JsonUtility.ToJson(validation, true));
            Debug.Log("DragonGlare validation report: " + reportPath);
        }
        if (Application.isBatchMode) EditorApplication.Exit(validation.passed ? 0 : 1);
        else if (!validation.passed) throw new InvalidOperationException("DragonGlare Windows validation/build failed. See .codex-build/unity-validation/report.json.");
    }

    private static void ValidateScene()
    {
        var scene = SceneManager.GetSceneByPath(ScenePath);
        var opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            var roots = scene.GetRootGameObjects();
            if (roots.Length != 1 || roots[0].GetComponent<DragonGlareBootstrap>() == null)
                throw new InvalidOperationException("DragonGlare3D must contain one root with the DragonGlareBootstrap component.");
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(roots[0]) != 0)
                throw new InvalidOperationException("The DragonGlare scene contains a missing script.");
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }

    private static string GetArgument(string name)
    {
        var arguments = Environment.GetCommandLineArgs();
        for (var index = 0; index + 1 < arguments.Length; index++)
            if (string.Equals(arguments[index], name, StringComparison.Ordinal)) return arguments[index + 1];
        return null;
    }

    [Serializable]
    private sealed class ValidationReport
    {
        public bool passed;
        public bool splashScreenEnabled;
        public bool unityLogoEnabled;
        public string unityVersion;
        public string scenePath;
        public string outputPath;
        public string result;
        public int errorCount;
        public int warningCount;
        public long totalSizeBytes;
        public double durationSeconds;
        public string[] messages;
    }
}
