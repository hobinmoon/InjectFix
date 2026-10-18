using System;
using System.Collections.Generic;
using System.Text;
using IFixBench.Target;

//The same code is compiled twice: Native runs as is, Interp (IFIX_INTERP defined) is injected with no_cfg and runs entirely in the VM.
//Comparing their results checks correctness; comparing timings shows the cost of VM extern calls.
#if IFIX_INTERP
namespace IFixBench.Interp
#else
namespace IFixBench.Native
#endif
{
    public static class Cases
    {
        public static int CheckCount()
        {
            return 22;
        }

        public static string Check(int id)
        {
            var sq = new Square(3);
            var v = new V3(1, 2, 3);
            switch (id)
            {
                case 0:
                    return "static int: " + Api.Add(2, 3);
                case 1:
                    return "mixed prims: " + Api.Mix(5L, 0.5, true, 7);
                case 2:
                    return "uint/short/char: " + Api.U(10u, -3, 'A');
                case 3:
                    return "struct arg/ret: " + Api.Scale(v, 2f);
                case 4:
                    return "struct -> float: " + Api.Sum(v);
                case 5:
                    return "enum int: " + Api.Next(Color.Red);
                case 6:
                    return "enum long: " + (long)Api.Twice(Big.A);
                case 7:
                    return "nullable: " + Api.Nul(21) + "/" + Api.Nul(null).HasValue;
                case 8:
                    return "objects: " + Api.Join(42, "x") + "/" + Api.Join(null, null);
                case 9:
                    return "virtual: " + ((ShapeBase)sq).Name();
                case 10:
                    return "abstract: " + ((ShapeBase)sq).Area(2);
                case 11:
                    return "interface: " + ((IShape)sq).Area(2);
                case 12:
                    return "struct instance (fallback): " + v.Length();
                case 13:
                    {
                        int x = 1;
                        Api.Inc(ref x);
                        return "ref param (fallback): " + x;
                    }
                case 14:
                    return "string api: " + "hello".Length + "/" + "a,b,c".Split(',').Length + "/"
                        + string.Concat("p", "q");
                case 15:
                    {
                        var list = new List<int>();
                        list.Add(5);
                        list.Add(6);
                        return "List<int>: " + list.Count + "/" + list[1] + "/" + list.Contains(5);
                    }
                case 16:
                    {
                        var dict = new Dictionary<string, int>();
                        dict["a"] = 1;
                        int val;
                        bool found = dict.TryGetValue("a", out val);
                        return "Dictionary: " + dict.ContainsKey("a") + "/" + found + "/" + val;
                    }
                case 17:
                    return "StringBuilder: " + new StringBuilder().Append("a").Append(1).Append(2.5f).ToString();
                case 18:
                    return "Math: " + Math.Max(1.5f, -2f) + "/" + Math.Abs(-7) + "/" + Math.Sqrt(16.0);
                case 19:
                    try
                    {
                        Api.Throw();
                        return "exception: none";
                    }
                    catch (InvalidOperationException e)
                    {
                        return "exception: " + e.Message;
                    }
                case 20:
                    return "delegate invoke: " + Api.MakeDoubler()(4);
                case 21:
                    Api.Counter = 5;
                    return "generic method / static prop: " + Api.Echo(5) + "/" + Api.Echo("s") + "/" + Api.Counter;
                default:
                    return "unknown";
            }
        }

        //The performance cases below return a checksum that must match between Native and Interp

        //No extern calls: baseline for the VM's own overhead
        public static long PerfNoExtern(int n)
        {
            long r = 0;
            for (int i = 0; i < n; i++)
            {
                r += i;
            }
            return r;
        }

        public static long PerfStaticAdd(int n)
        {
            int r = 0;
            for (int i = 0; i < n; i++)
            {
                r = Api.Add(r, i);
            }
            return r;
        }

        public static long PerfVirtual(ShapeBase s, int n)
        {
            long r = 0;
            for (int i = 0; i < n; i++)
            {
                r += s.Area(1);
            }
            return r;
        }

        public static long PerfInterface(IShape s, int n)
        {
            long r = 0;
            for (int i = 0; i < n; i++)
            {
                r += s.Area(1);
            }
            return r;
        }

        //Two extern calls per iteration; arguments and return values are structs (boxed)
        public static long PerfStruct(int n)
        {
            var v = new V3(1, 1, 1);
            float r = 0;
            for (int i = 0; i < n; i++)
            {
                r += Api.Sum(Api.Scale(v, 0.5f));
            }
            return (long)r;
        }

        public static long PerfStringLength(string s, int n)
        {
            long r = 0;
            for (int i = 0; i < n; i++)
            {
                r += s.Length;
            }
            return r;
        }

        public static long PerfListIndexer(List<int> list, int n)
        {
            long r = 0;
            for (int i = 0; i < n; i++)
            {
                r += list[i & 7];
            }
            return r;
        }

        //Constructors still go through reflection; control case
        public static long PerfNewobj(int n)
        {
            long r = 0;
            for (int i = 0; i < n; i++)
            {
                r += new Square(i & 7).Side;
            }
            return r;
        }

        //Paths below still go through reflection (fields, ref/out, struct instance methods, struct constructors)

        public static long PerfFieldRead(Counter c, int n)
        {
            long r = 0;
            for (int i = 0; i < n; i++)
            {
                r += c.Value;
            }
            return r;
        }

        public static long PerfFieldWrite(Counter c, int n)
        {
            for (int i = 0; i < n; i++)
            {
                c.Value = i;
            }
            return c.Value;
        }

        public static long PerfStaticField(int n)
        {
            Counter.StaticValue = 0;
            for (int i = 0; i < n; i++)
            {
                Counter.StaticValue += 1;
            }
            return Counter.StaticValue;
        }

        public static long PerfOutParam(int n)
        {
            long r = 0;
            for (int i = 0; i < n; i++)
            {
                int half;
                Api.TryHalf(i, out half);
                r += half;
            }
            return r;
        }

        public static long PerfRefParam(int n)
        {
            int a = 1, b = 2;
            for (int i = 0; i < n; i++)
            {
                Api.Swap(ref a, ref b);
            }
            return a * 10 + b;
        }

        public static long PerfStructInstance(int n)
        {
            var v = new V3(3, 4, 0);
            float r = 0;
            for (int i = 0; i < n; i++)
            {
                r += v.Length();
            }
            return (long)r;
        }

        public static long PerfNewStruct(int n)
        {
            float r = 0;
            for (int i = 0; i < n; i++)
            {
                r += new V3(i & 7, 0, 0).x;
            }
            return (long)r;
        }

        //Instance method of a generic class (generic invoker)
        public static long PerfGenericBox(Box<int> box, int n)
        {
            long r = 0;
            for (int i = 0; i < n; i++)
            {
                r += box.Get();
            }
            return r;
        }
    }
}
