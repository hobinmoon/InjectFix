using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using IFix.Core;
using IFixBench.Target;
using IFixBench.Workflow;
using UnityEngine;
using Nat = IFixBench.Native;
using Vm = IFixBench.Interp;

//1. VM bench: compare results and timings of IFixBench.Interp (no_cfg injected, fully run by the VM) and IFixBench.Native
//2. workflow: Assembly-CSharp is injected per the [IFix] config; compare behavior before/after loading the -wfPatch patch
//Writes the results to the -benchOut file and quits
public class BenchRunner : MonoBehaviour
{
    const int N = 200000;

    const int REPEAT = 5;

    StringBuilder report = new StringBuilder();

    int failures = 0;

    IEnumerator Start()
    {
        yield return null;
        try
        {
            header();
            vmBench();
            workflow();
        }
        catch (Exception e)
        {
            failures++;
            report.AppendLine("FATAL " + e);
        }
        report.AppendLine();
        report.AppendLine(failures == 0 ? "RESULT OK" : "RESULT FAIL " + failures);

        var outPath = getArg("-benchOut") ?? Path.Combine(Application.persistentDataPath, "ifix_bench.txt");
        File.WriteAllText(outPath, report.ToString());
        UnityEngine.Debug.Log(report.ToString());
        Application.Quit(failures == 0 ? 0 : 1);
    }

    void header()
    {
        bool generic = typeof(VirtualMachine).Assembly.GetType("IFix.Core.GenericMethodInvoker") != null;
        report.AppendLine("core: " + (generic ? "generic invoker" : "reflection only"));
#if ENABLE_IL2CPP
        report.AppendLine("backend: IL2CPP");
#else
        report.AppendLine("backend: Mono");
#endif
        report.AppendLine("unity: " + Application.unityVersion + ", platform: " + Application.platform
            + ", N=" + N);
    }

    void vmBench()
    {
        var patch = Resources.Load<TextAsset>("IFixBench.Interp.patch");
        var sw = Stopwatch.StartNew();
        PatchManager.Load(new MemoryStream(patch.bytes));
        report.AppendLine("vm bench patch load: " + sw.ElapsedMilliseconds + " ms");

        checkSuite("basic", Nat.Cases.CheckCount(), Nat.Cases.Check, Vm.Cases.Check);
        checkSuite("unity api", Nat.UnityCases.CheckCount(), Nat.UnityCases.Check, Vm.UnityCases.Check);
        checkSuite("game logic", Nat.GameCases.CheckCount(), Nat.GameCases.Check, Vm.GameCases.Check);
        checkSuite("c# language", Nat.LangCases.CheckCount(), Nat.LangCases.Check, Vm.LangCases.Check);
        checkSuite("regression", Nat.ConstCases.CheckCount(), Nat.ConstCases.Check, Vm.ConstCases.Check);

        var square = new Square(3);
        var list = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 };
        var counter = new Counter();
        var box = new Box<int>(3);
        var go = new GameObject("perf");
        var t = go.transform;

        perfHeader("extern call shapes");
        perf("no extern", Nat.Cases.PerfNoExtern, Vm.Cases.PerfNoExtern);
        perf("static Add", Nat.Cases.PerfStaticAdd, Vm.Cases.PerfStaticAdd);
        perf("virtual", n => Nat.Cases.PerfVirtual(square, n), n => Vm.Cases.PerfVirtual(square, n));
        perf("interface", n => Nat.Cases.PerfInterface(square, n), n => Vm.Cases.PerfInterface(square, n));
        perf("struct x2", Nat.Cases.PerfStruct, Vm.Cases.PerfStruct);
        perf("string.Length", n => Nat.Cases.PerfStringLength("hello", n),
            n => Vm.Cases.PerfStringLength("hello", n));
        perf("List<int>[i]", n => Nat.Cases.PerfListIndexer(list, n), n => Vm.Cases.PerfListIndexer(list, n));
        perf("Box<int>.Get", n => Nat.Cases.PerfGenericBox(box, n), n => Vm.Cases.PerfGenericBox(box, n));

        perfHeader("still reflection");
        perf("newobj class", Nat.Cases.PerfNewobj, Vm.Cases.PerfNewobj);
        perf("newobj struct", Nat.Cases.PerfNewStruct, Vm.Cases.PerfNewStruct);
        perf("struct instance", Nat.Cases.PerfStructInstance, Vm.Cases.PerfStructInstance);
        perf("out param", Nat.Cases.PerfOutParam, Vm.Cases.PerfOutParam);
        perf("ref param", Nat.Cases.PerfRefParam, Vm.Cases.PerfRefParam);
        perf("field read", n => Nat.Cases.PerfFieldRead(counter, n), n => Vm.Cases.PerfFieldRead(counter, n));
        perf("field write", n => Nat.Cases.PerfFieldWrite(counter, n), n => Vm.Cases.PerfFieldWrite(counter, n));
        perf("static field +=", Nat.Cases.PerfStaticField, Vm.Cases.PerfStaticField);

        perfHeader("unity api");
        perf("Vector3 a+b*f", Nat.UnityCases.PerfVectorMath, Vm.UnityCases.PerfVectorMath);
        perf("Mathf", Nat.UnityCases.PerfMathf, Vm.UnityCases.PerfMathf);
        perf("transform.pos", n => Nat.UnityCases.PerfTransform(t, n), n => Vm.UnityCases.PerfTransform(t, n));

        perfHeader("scenarios (per unit)");
        perf("game tick/entity", Nat.GameCases.PerfGameTick, Vm.GameCases.PerfGameTick);
        perf("List.Sort cb/elem", Nat.LangCases.PerfNativeCallback, Vm.LangCases.PerfNativeCallback);
        perf("LINQ/elem", Nat.LangCases.PerfLinq, Vm.LangCases.PerfLinq);
        perf("foreach List", Nat.LangCases.PerfForeachList, Vm.LangCases.PerfForeachList);
        perf("event callback", Nat.LangCases.PerfEventCallback, Vm.LangCases.PerfEventCallback);
        perf("vm virtual", Nat.LangCases.PerfInternalVirtual, Vm.LangCases.PerfInternalVirtual);

        Destroy(go);
    }

    void workflow()
    {
        report.AppendLine();
        report.AppendLine("== workflow (Assembly-CSharp inject + [Patch])");
        var pts = new Vector3[100];
        for (int i = 0; i < pts.Length; i++)
        {
            pts[i] = new Vector3(i - 50, i % 7, 0);
        }

        //Before the patch: original (buggy) behavior
        var before = workflowResults(pts);
        double nativeAcc = measure(n => { float r = 0; for (int i = 0; i < n / 100; i++) r += WfHot.Accumulate(pts); return (long)r; });
        double nativeMix = measure(n => { long r = 0; for (int i = 0; i < n; i++) r += WfHot.Mix(i, 3); return r; });

        var patchPath = getArg("-wfPatch");
        if (patchPath == null || !File.Exists(patchPath))
        {
            failures++;
            report.AppendLine("FAIL patch not found: " + patchPath);
            return;
        }
        var sw = Stopwatch.StartNew();
        PatchManager.Load(new MemoryStream(File.ReadAllBytes(patchPath)));
        report.AppendLine("patch load: " + sw.ElapsedMilliseconds + " ms");

        var after = workflowResults(pts);
        double vmAcc = measure(n => { float r = 0; for (int i = 0; i < n / 100; i++) r += WfHot.Accumulate(pts); return (long)r; });
        double vmMix = measure(n => { long r = 0; for (int i = 0; i < n; i++) r += WfHot.Mix(i, 3); return r; });

        string[,] expected =
        {
            { "Add(3,4)", "12", "7" },
            { "Sub(10,4)", "6", "6" },
            { "Damage(10,20)", "-10", "1" },
            { "Mover.Step(0.5)", "(1.00, 0.00, 0.00) 0", "(0.50, 0.00, 0.00) 90" },
            { "Cheapest(2)", "30,10", "10,20" },
            { "Twice()", "60", "60" },
            { "MaxHp", "106", "160" },
            { "Gold(3)", "30", "39" },
            { "Orc.Describe", "orc", "orc<enemy>" },
            { "Accumulate", before[9], before[9] },
            { "Mix(9,3)", "11", "11" },
        };
        for (int i = 0; i < expected.GetLength(0); i++)
        {
            bool ok = before[i] == expected[i, 1] && after[i] == expected[i, 2];
            if (!ok)
            {
                failures++;
            }
            report.AppendLine(string.Format("{0} {1,-16} before={2} after={3}{4}", ok ? "OK  " : "FAIL",
                expected[i, 0], before[i], after[i],
                ok ? "" : "  (expected " + expected[i, 1] + " / " + expected[i, 2] + ")"));
        }

        report.AppendLine(string.Format("{0,-18} {1,10} {2,10} {3,8}", "patched method", "native", "vm",
            "vm/nat"));
        report.AppendLine(string.Format("{0,-18} {1,10:F1} {2,10:F1} {3,7:F0}x", "Accumulate/elem", nativeAcc,
            vmAcc, vmAcc / Math.Max(nativeAcc, 0.01)));
        report.AppendLine(string.Format("{0,-18} {1,10:F1} {2,10:F1} {3,7:F0}x", "Mix call (entry)", nativeMix,
            vmMix, vmMix / Math.Max(nativeMix, 0.01)));
    }

    static string[] workflowResults(Vector3[] pts)
    {
        var results = new List<string>();
        var calc = new WfCalculator();
        results.Add(call(() => calc.Add(3, 4).ToString()));
        results.Add(call(() => calc.Sub(10, 4).ToString()));
        results.Add(call(() => WfMath.Damage(10, 20).ToString()));
        results.Add(call(() =>
        {
            var go = new GameObject("mover");
            var mover = go.AddComponent<WfMover>();
            mover.Step(0.5f);
            var s = go.transform.position.ToString("F2") + " " + Mathf.RoundToInt(go.transform.eulerAngles.y);
            DestroyImmediate(go);
            return s;
        }));
        var inv = new WfInventory();
        results.Add(call(() => inv.Cheapest(2)));
        results.Add(call(() => inv.Twice().ToString()));
        results.Add(call(() => new WfPlayer().MaxHp.ToString()));
        results.Add(call(() => new WfReward().Gold(3).ToString()));
        results.Add(call(() => new WfOrc().Describe()));
        results.Add(call(() => WfHot.Accumulate(pts).ToString("F2")));
        results.Add(call(() => WfHot.Mix(9, 3).ToString()));
        return results.ToArray();
    }

    void checkSuite(string name, int count, Func<int, string> native, Func<int, string> vm)
    {
        report.AppendLine();
        report.AppendLine("== correctness: " + name + " (native vs vm)");
        for (int i = 0; i < count; i++)
        {
            int id = i;
            string expected = call(() => native(id));
            string actual = call(() => vm(id));
            bool ok = expected == actual;
            if (!ok)
            {
                failures++;
            }
            report.AppendLine((ok ? "OK   " : "FAIL ") + expected + (ok ? "" : "  <vm> " + actual));
        }
    }

    static string call(Func<string> f)
    {
        try
        {
            return f();
        }
        catch (Exception e)
        {
            return "EX " + e.GetType().Name + ": " + e.Message;
        }
    }

    void perfHeader(string title)
    {
        report.AppendLine();
        report.AppendLine("== performance: " + title + " (ns/iter, best of " + REPEAT + ")");
        report.AppendLine(string.Format("{0,-18} {1,10} {2,10} {3,8}", "case", "native", "vm", "vm/nat"));
    }

    void perf(string name, Func<int, long> native, Func<int, long> vm)
    {
        long nativeSum, vmSum;
        double nativeNs = measure(native, out nativeSum);
        double vmNs;
        try
        {
            vmNs = measure(vm, out vmSum);
        }
        catch (Exception e)
        {
            failures++;
            report.AppendLine(string.Format("{0,-18} FAIL {1}: {2}", name, e.GetType().Name, e.Message));
            return;
        }
        bool ok = nativeSum == vmSum;
        if (!ok)
        {
            failures++;
        }
        report.AppendLine(string.Format("{0,-18} {1,10:F1} {2,10:F1} {3,7:F0}x{4}", name, nativeNs, vmNs,
            vmNs / Math.Max(nativeNs, 0.01), ok ? "" : "  FAIL checksum " + nativeSum + " != " + vmSum));
    }

    static double measure(Func<int, long> f)
    {
        long checksum;
        return measure(f, out checksum);
    }

    static double measure(Func<int, long> f, out long checksum)
    {
        checksum = f(1000);
        double best = double.MaxValue;
        for (int i = 0; i < REPEAT; i++)
        {
            var sw = Stopwatch.StartNew();
            checksum = f(N);
            sw.Stop();
            best = Math.Min(best, sw.Elapsed.TotalMilliseconds * 1e6 / N);
        }
        return best;
    }

    static string getArg(string name)
    {
        var args = Environment.GetCommandLineArgs();
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
