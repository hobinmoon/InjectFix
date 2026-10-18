using UnityEngine;

//UnityEngine API: struct math (Vector3/Quaternion/Matrix4x4), Mathf, GameObject/Transform/components
#if IFIX_INTERP
namespace IFixBench.Interp
#else
namespace IFixBench.Native
#endif
{
    public static class UnityCases
    {
        public static int CheckCount()
        {
            return 10;
        }

        public static string Check(int id)
        {
            var a = new Vector3(1, 2, 3);
            var b = new Vector3(4, 5, 6);
            switch (id)
            {
                case 0:
                    return "Vector3 ops: " + (a + b).ToString("F2") + " " + Vector3.Dot(a, b) + " "
                        + Vector3.Cross(a, b).ToString("F2") + " " + (a * 2f - b / 2f).ToString("F2");
                case 1:
                    return "Vector3 props: " + a.magnitude.ToString("F4") + " " + a.normalized.ToString("F4") + " "
                        + Vector3.Distance(a, b).ToString("F4");
                case 2:
                    return "Quaternion: " + (Quaternion.Euler(0, 90, 0) * Vector3.forward).ToString("F3") + " "
                        + Quaternion.Angle(Quaternion.identity, Quaternion.Euler(30, 0, 0)).ToString("F2");
                case 3:
                    return "Mathf: " + Mathf.Lerp(0, 10, 0.25f) + " " + Mathf.Clamp(15, 0, 10) + " "
                        + Mathf.Approximately(0.1f + 0.2f, 0.3f) + " " + Mathf.Sqrt(2f).ToString("F5") + " "
                        + Mathf.RoundToInt(2.5f) + " " + Mathf.PingPong(7f, 3f);
                case 4:
                    {
                        var go = new GameObject("vm_go");
                        go.transform.position = a;
                        go.transform.Translate(1, 0, 0);
                        go.transform.Rotate(0, 90, 0);
                        string s = "GameObject/Transform: " + go.name + " " + go.transform.position.ToString("F1")
                            + " " + go.transform.forward.ToString("F1") + " " + go.activeSelf;
                        Object.DestroyImmediate(go);
                        return s;
                    }
                case 5:
                    {
                        var parent = new GameObject("parent");
                        var child = new GameObject("child");
                        child.transform.SetParent(parent.transform, false);
                        parent.transform.position = new Vector3(10, 0, 0);
                        child.transform.localPosition = new Vector3(0, 1, 0);
                        string s = "hierarchy: " + parent.transform.childCount + " "
                            + child.transform.position.ToString("F1") + " " + (child.transform.parent == parent.transform)
                            + " " + parent.transform.Find("child").name;
                        Object.DestroyImmediate(parent);
                        return s;
                    }
                case 6:
                    {
                        var go = new GameObject("comp");
                        var mf = go.AddComponent<MeshFilter>();
                        MeshRenderer mr;
                        bool hasRenderer = go.TryGetComponent(out mr);
                        string s = "components: " + (go.GetComponent<MeshFilter>() == mf) + " " + hasRenderer + " "
                            + go.GetComponents<Component>().Length + " " + (mf.gameObject == go);
                        Object.DestroyImmediate(go);
                        return s;
                    }
                case 7:
                    return "Color: " + Color.Lerp(Color.red, Color.blue, 0.5f) + " " + (Color32)Color.green;
                case 8:
                    Random.InitState(42);
                    return "Random: " + Random.Range(0, 100) + " " + Random.Range(0, 100) + " "
                        + Random.value.ToString("F4");
                case 9:
                    {
                        var m = Matrix4x4.TRS(new Vector3(1, 0, 0), Quaternion.Euler(0, 0, 90), Vector3.one * 2);
                        return "Matrix4x4: " + m.MultiplyPoint3x4(Vector3.right).ToString("F2") + " "
                            + m.determinant.ToString("F2");
                    }
                default:
                    return "unknown";
            }
        }

        //Vector3 static operators: struct arguments and return values
        public static long PerfVectorMath(int n)
        {
            var acc = Vector3.zero;
            var step = new Vector3(0.001f, 0.002f, 0.003f);
            for (int i = 0; i < n; i++)
            {
                acc = acc + step * 0.5f;
            }
            return (long)(acc.x * 1000);
        }

        //Mathf: float/int static methods
        public static long PerfMathf(int n)
        {
            float r = 0;
            for (int i = 0; i < n; i++)
            {
                r += Mathf.Clamp(i & 127, 10, 100) + Mathf.Abs(-1.5f);
            }
            return (long)r;
        }

        //Transform: reference type instance methods + struct arguments/return values + engine native calls
        public static long PerfTransform(Transform t, int n)
        {
            float r = 0;
            for (int i = 0; i < n; i++)
            {
                t.position = new Vector3(i & 15, 0, 0);
                r += t.position.x;
            }
            return (long)r;
        }
    }
}
