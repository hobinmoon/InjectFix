using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Player;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

//Batchmode entry points (called by run.sh)
//Build/BuildIOS: create a scene with only BenchRunner and build a macOS IL2CPP Standalone player / iOS Xcode project
//(IFix's PostProcessScene injects Assembly-CSharp)
//GenWorkflowPatch: with IFIX_PATCHED defined, compile the player scripts (-benchTarget ios for iOS) and generate the Assembly-CSharp patch
public static class BenchBuilder
{
    const string ScenePath = "Assets/IFixBench/Bench.unity";

    public static void Build()
    {
        setup(NamedBuildTarget.Standalone);
        UnityEditor.OSXStandalone.UserBuildSettings.architecture = OSArchitecture.ARM64;
        build(BuildTarget.StandaloneOSX, getArg("-benchBuildPath") ?? "Build/IFixBench.app");
    }

    //Exports an Xcode project for the iOS Simulator (arm64); run.sh builds it with xcodebuild
    public static void BuildIOS()
    {
        setup(NamedBuildTarget.iOS);
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.ifix.bench");
        PlayerSettings.iOS.sdkVersion = iOSSdkVersion.SimulatorSDK;
        PlayerSettings.iOS.simulatorSdkArchitecture = AppleMobileArchitectureSimulator.ARM64;
        build(BuildTarget.iOS, getArg("-benchBuildPath") ?? "Build/IFixBench-Xcode");
    }

    static void setup(NamedBuildTarget target)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("BenchRunner").AddComponent<BenchRunner>();
        EditorSceneManager.SaveScene(scene, ScenePath);

        PlayerSettings.productName = "IFixBench";
        PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetIl2CppCompilerConfiguration(target, Il2CppCompilerConfiguration.Release);
        PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.Minimal);
        //Unity's default .NET Standard 2.1: Assembly-CSharp references netstandard; verify the patch's type names load under IL2CPP
        PlayerSettings.SetApiCompatibilityLevel(target, ApiCompatibilityLevel.NET_Standard);
    }

    static void build(BuildTarget target, string path)
    {
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = path,
            target = target,
            options = BuildOptions.None,
        });
        Debug.Log("IFixBench build result: " + report.summary.result);
        EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    public static void GenWorkflowPatch()
    {
#if IFIX_PATCHED
        var outDir = "Temp/ifixbench_patch";
        var patchPath = getArg("-wfPatchOut") ?? "Assembly-CSharp.patch.bytes";
        Directory.CreateDirectory(outDir);
        bool ios = getArg("-benchTarget") == "ios";
        PlayerBuildInterface.CompilePlayerScripts(new ScriptCompilationSettings
        {
            group = ios ? BuildTargetGroup.iOS : BuildTargetGroup.Standalone,
            target = ios ? BuildTarget.iOS : BuildTarget.StandaloneOSX,
        }, outDir);
        if (File.Exists(patchPath))
        {
            File.Delete(patchPath);
        }
        IFix.Editor.IFixEditor.GenPatch("Assembly-CSharp", outDir + "/Assembly-CSharp.dll",
            "./Assets/Plugins/IFix.Core.dll", patchPath);
        bool ok = File.Exists(patchPath);
        Debug.Log("IFixBench patch result: " + (ok ? "Succeeded " + patchPath : "Failed"));
        EditorApplication.Exit(ok ? 0 : 1);
#else
        Debug.LogError("IFixBench patch result: Failed, IFIX_PATCHED is not defined");
        EditorApplication.Exit(1);
#endif
    }

    static string getArg(string name)
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name)
            {
                return args[i + 1];
            }
        }
        return null;
    }
}
