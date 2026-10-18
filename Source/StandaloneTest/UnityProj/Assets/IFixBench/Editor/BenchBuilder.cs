using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Player;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

//Batchmode entry points (called by run.sh)
//Build: create a scene with only BenchRunner and build a macOS IL2CPP Standalone player (IFix's PostProcessScene injects Assembly-CSharp)
//GenWorkflowPatch: with IFIX_PATCHED defined, compile the player scripts and generate the Assembly-CSharp patch
public static class BenchBuilder
{
    const string ScenePath = "Assets/IFixBench/Bench.unity";

    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("BenchRunner").AddComponent<BenchRunner>();
        EditorSceneManager.SaveScene(scene, ScenePath);

        var target = NamedBuildTarget.Standalone;
        PlayerSettings.productName = "IFixBench";
        PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetIl2CppCompilerConfiguration(target, Il2CppCompilerConfiguration.Release);
        PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.Minimal);
        //Unity's default .NET Standard 2.1: Assembly-CSharp references netstandard; verify the patch's type names load under IL2CPP
        PlayerSettings.SetApiCompatibilityLevel(target, ApiCompatibilityLevel.NET_Standard);
        UnityEditor.OSXStandalone.UserBuildSettings.architecture = OSArchitecture.ARM64;

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = getArg("-benchBuildPath") ?? "Build/IFixBench.app",
            target = BuildTarget.StandaloneOSX,
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
        PlayerBuildInterface.CompilePlayerScripts(new ScriptCompilationSettings
        {
            group = BuildTargetGroup.Standalone,
            target = BuildTarget.StandaloneOSX,
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
