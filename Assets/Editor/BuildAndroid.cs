using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Script de compilación para Android. Genera Builds/SurfTsunami2D.apk.
/// Menú: "Build > Android APK"            (IL2CPP, el modo normal)
///       "Build > Android APK (Mono)"     (Mono, más ligero/rápido)
/// Batch: Unity.exe -batchmode -projectPath &lt;proyecto&gt; -executeMethod BuildAndroid.BuildAPK -logFile build.log
/// </summary>
public static class BuildAndroid
{
    private const string OutputDir = "Builds";
    private const string ApkName = "SurfTsunami2D.apk";
    private const string AndroidAppId = "com.jonathanmf12.surftsunami2d";

    [MenuItem("Build/Android APK")]
    public static void BuildAPK()
    {
        Compile(ScriptingImplementation.IL2CPP, AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64, ApkName);
    }

    [MenuItem("Build/Android APK (Mono)")]
    public static void BuildAPKMono()
    {
        Compile(ScriptingImplementation.Mono2x, AndroidArchitecture.ARMv7, "SurfTsunami2D-mono.apk");
    }

    private static void Compile(ScriptingImplementation backend, AndroidArchitecture archs, string apkName)
    {
        int exitCode = 1;
        try
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError("[BuildAndroid] El módulo 'Android Build Support' no está instalado en este Editor de Unity.");
                EditorApplication.Exit(2);
                return;
            }

            ConfigurarJdk();

            ConfigureAndroidPlayer(backend, archs);

            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Debug.LogError("[BuildAndroid] No hay escenas habilitadas en Build Settings.");
                EditorApplication.Exit(1);
                return;
            }

            Directory.CreateDirectory(OutputDir);
            string apkPath = Path.Combine(OutputDir, apkName);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None,
            };

            Debug.Log("[BuildAndroid] Compilando (" + backend + ") " + apkPath + " ...");
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                ulong sizeMb = summary.totalSize / (1024UL * 1024UL);
                Debug.Log("[BuildAndroid] APK generado correctamente: " + Path.GetFullPath(apkPath) + " (" + sizeMb + " MB)");
                exitCode = 0;
            }
            else
            {
                Debug.LogError("[BuildAndroid] Falló la compilación: " + summary.result +
                               " (" + summary.totalErrors + " errores)");
                exitCode = 1;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[BuildAndroid] Excepción durante la compilación:\n" + e);
            exitCode = 1;
        }

        EditorApplication.Exit(exitCode);
    }

    /// <summary>
    /// Se asegura de que Gradle tenga un JDK 17. Si Unity no trae el suyo instalado
    /// (PlaybackEngines/AndroidPlayer/OpenJDK), usa el de la variable de entorno
    /// SURF_JDK_PATH o el que hayamos dejado en %LOCALAPPDATA%\Unity\OpenJDK17.
    /// </summary>
    private static void ConfigurarJdk()
    {
        string jdkUnity = Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines/AndroidPlayer/OpenJDK");
        if (Directory.Exists(Path.Combine(jdkUnity, "bin")))
            return; // Unity ya tiene su propio JDK: no tocamos nada.

        string jdk = Environment.GetEnvironmentVariable("SURF_JDK_PATH");
        if (string.IsNullOrEmpty(jdk) || !Directory.Exists(Path.Combine(jdk, "bin")))
        {
            jdk = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Unity", "OpenJDK17");
        }

        if (!Directory.Exists(Path.Combine(jdk, "bin")))
        {
            Debug.LogWarning("[BuildAndroid] No se ha encontrado ningún JDK 17. " +
                             "Configúralo en Edit > Preferences > External Tools, o pon SURF_JDK_PATH.");
            return;
        }

        Debug.Log("[BuildAndroid] Usando el JDK: " + jdk);
        EditorPrefs.SetString("JdkPath", jdk);
        EditorPrefs.SetBool("JdkUseEmbedded", false);

        Type ajustes = Type.GetType("UnityEditor.Android.AndroidExternalToolsSettings, UnityEditor.Android.Extensions");
        if (ajustes == null)
            return;

        System.Reflection.PropertyInfo propiedad = ajustes.GetProperty(
            "jdkRootPath",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (propiedad != null && propiedad.CanWrite)
            propiedad.SetValue(null, jdk);

        // Desmarcar "JDK Installed with Unity" en el objeto serializado.
        object instancia = ajustes.GetProperty(
            "instance",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?.GetValue(null);

        if (instancia != null)
        {
            System.Reflection.FieldInfo campo = ajustes.GetField(
                "m_JdkUseEmbedded",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (campo != null)
                campo.SetValue(instancia, false);
        }
    }

    private static void ConfigureAndroidPlayer(ScriptingImplementation backend, AndroidArchitecture archs)
    {
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AndroidAppId);
        PlayerSettings.Android.bundleVersionCode = Math.Max(1, PlayerSettings.Android.bundleVersionCode);
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.Android.targetArchitectures = archs;

        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, backend);
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Low);
        if (backend == ScriptingImplementation.IL2CPP)
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Android, Il2CppCompilerConfiguration.Release);

        // El juego está pensado en horizontal.
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;

        // APK (no AAB) para poder instalarlo a mano en el teléfono.
        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.development = false;

        AssetDatabase.SaveAssets();
    }
}
