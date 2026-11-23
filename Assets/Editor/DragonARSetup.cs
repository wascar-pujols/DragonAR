using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Configura el proyecto automáticamente la primera vez que se abre:
/// escena, ajustes de Android (IL2CPP, ARM64, API 26, OpenGLES3, APK) e intenta activar ARCore.
/// Menú: "Dragon AR" para reconfigurar o compilar el APK.
/// </summary>
[InitializeOnLoad]
public static class DragonARSetup
{
    const string ScenePath = "Assets/Scenes/Main.unity";
    const string DoneFile = "ProjectSettings/DragonARSetup.done";

    static DragonARSetup()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(DoneFile)) Configure(true);
        };
    }

    [MenuItem("Dragon AR/1) Configurar proyecto")]
    public static void ConfigureMenu() { Configure(true); }

    public static void Configure(bool showDialog)
    {
        string xrMsg = null;
        try
        {
            EnsureScene();
            SetAndroidSettings();
            xrMsg = SetupARCore();
            bool needRestart = EnableInputSystem();
            File.WriteAllText(DoneFile, DateTime.Now.ToString());
            AssetDatabase.SaveAssets();

            if (showDialog)
            {
                string msg = "Proyecto configurado para Android AR.\n\n";
                msg += xrMsg == null
                    ? "ARCore activado.\n"
                    : "AVISO: no pude activar ARCore automáticamente (" + xrMsg + ").\n" +
                      "Hazlo a mano: Edit > Project Settings > XR Plug-in Management > pestaña Android > marcar ARCore.\n";
                if (needRestart) msg += "\nUnity se reiniciará una vez para activar el Input System.";
                msg += "\nSiguiente paso: menú Dragon AR > 2) Compilar APK.";
                EditorUtility.DisplayDialog("Dragon AR", msg, "OK");
            }
            if (needRestart) EditorApplication.OpenProject(Directory.GetCurrentDirectory());
        }
        catch (Exception e)
        {
            Debug.LogError("[DragonAR] Error al configurar: " + e);
        }
    }

    [MenuItem("Dragon AR/2) Compilar APK")]
    public static void BuildApk()
    {
        Configure(false);
        Directory.CreateDirectory("Builds");
        EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.Android, BuildTarget.Android);

        var opts = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Builds/DragonAR.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        };
        BuildReport report = BuildPipeline.BuildPlayer(opts);
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log("[DragonAR] APK generado: " + report.summary.outputPath);
            EditorUtility.RevealInFinder(report.summary.outputPath);
        }
        else
        {
            EditorUtility.DisplayDialog("Dragon AR", "La compilación falló. Revisa la consola de Unity.", "OK");
        }
    }

    // ---------------- Pasos ----------------
    static void EnsureScene()
    {
        if (!File.Exists(ScenePath))
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        if (EditorSceneManager.GetActiveScene().path != ScenePath)
            EditorSceneManager.OpenScene(ScenePath);
    }

    static void SetAndroidSettings()
    {
        PlayerSettings.companyName = "Wascar";
        PlayerSettings.productName = "Dragon AR";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.wascar.dragonar");
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

        // APK (no AAB). Por reflexión para no depender de la versión exacta de la API.
        try
        {
            var p = typeof(EditorUserBuildSettings).GetProperty("buildAppBundle", BindingFlags.Public | BindingFlags.Static);
            if (p != null) p.SetValue(null, false);
        }
        catch (Exception e) { Debug.LogWarning("[DragonAR] No pude fijar buildAppBundle: " + e.Message); }
    }

    static bool EnableInputSystem()
    {
        var all = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
        if (all == null || all.Length == 0) return false;
        var so = new SerializedObject(all[0]);
        var prop = so.FindProperty("activeInputHandler");
        if (prop == null || prop.intValue != 0) return false; // ya es 1 (nuevo) o 2 (ambos)
        prop.intValue = 1;
        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        return true;
    }

    // ---- ARCore via XR Plug-in Management (reflexión: no rompe la compilación si cambia la API) ----
    static Type FindType(string name)
    {
        foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = a.GetType(name, false);
            if (t != null) return t;
        }
        return null;
    }

    static object Call(object target, Type type, string method, params object[] args)
    {
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        var m = type.GetMethods(flags).FirstOrDefault(x => x.Name == method && x.GetParameters().Length == args.Length);
        if (m == null) throw new MissingMethodException(type.Name + "." + method);
        return m.Invoke(m.IsStatic ? null : target, args);
    }

    /// <returns>null si todo salió bien; mensaje de error si no.</returns>
    static string SetupARCore()
    {
        try
        {
            Type perTargetT = FindType("UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget");
            Type generalT = FindType("UnityEngine.XR.Management.XRGeneralSettings");
            Type managerT = FindType("UnityEngine.XR.Management.XRManagerSettings");
            Type storeT = FindType("UnityEditor.XR.Management.Metadata.XRPackageMetadataStore");
            if (perTargetT == null || generalT == null || managerT == null || storeT == null)
                return "el paquete XR Management aún no está cargado";

            string key = "com.unity.xr.management.loader_settings";
            var keyField = generalT.GetField("k_SettingsKey", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (keyField != null) key = (string)keyField.GetValue(null);

            // Obtener o crear el asset de ajustes por plataforma
            var tryGet = typeof(EditorBuildSettings).GetMethod("TryGetConfigObject").MakeGenericMethod(perTargetT);
            object[] args = { key, null };
            bool found = (bool)tryGet.Invoke(null, args);
            UnityEngine.Object perTarget = args[1] as UnityEngine.Object;
            if (!found || perTarget == null)
            {
                Directory.CreateDirectory("Assets/XR");
                perTarget = ScriptableObject.CreateInstance(perTargetT);
                AssetDatabase.CreateAsset(perTarget, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
                EditorBuildSettings.AddConfigObject(key, perTarget, true);
            }

            var group = BuildTargetGroup.Android;
            object general = Call(perTarget, perTargetT, "SettingsForBuildTarget", group);
            if (general == null)
            {
                var gs = ScriptableObject.CreateInstance(generalT);
                gs.name = "Android Settings";
                AssetDatabase.AddObjectToAsset(gs, perTarget);
                Call(perTarget, perTargetT, "SetSettingsForBuildTarget", group, gs);
                general = gs;
            }

            object manager = Call(perTarget, perTargetT, "ManagerSettingsForBuildTarget", group);
            if (manager == null)
            {
                var ms = ScriptableObject.CreateInstance(managerT);
                ms.name = "Android Providers";
                AssetDatabase.AddObjectToAsset(ms, perTarget);
                Call(perTarget, perTargetT, "SetManagerSettingsForBuildTarget", group, ms);
                manager = ms;
            }

            generalT.GetProperty("Manager").SetValue(general, manager);
            generalT.GetProperty("InitManagerOnStart").SetValue(general, true);

            bool ok = (bool)Call(null, storeT, "AssignLoader", manager, "UnityEngine.XR.ARCore.ARCoreLoader", group);
            EditorUtility.SetDirty(perTarget);
            AssetDatabase.SaveAssets();
            return ok ? null : "AssignLoader devolvió false";
        }
        catch (Exception e)
        {
            Debug.LogWarning("[DragonAR] ARCore: " + e);
            return e.GetBaseException().Message;
        }
    }
}
