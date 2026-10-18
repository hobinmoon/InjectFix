using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFixBench.Target;

//C# language features: closures, LINQ, events, generics, inheritance, exceptions, foreach/iterators, boxing, structs
#if IFIX_INTERP
namespace IFixBench.Interp
#else
namespace IFixBench.Native
#endif
{
    public abstract class Animal
    {
        public virtual string Speak()
        {
            return "...";
        }

        public abstract int Legs { get; }
    }

    public class Dog : Animal
    {
        public override string Speak()
        {
            return "woof(" + base.Speak() + ")";
        }

        public override int Legs
        {
            get { return 4; }
        }
    }

    public class Bird : Animal
    {
        public override int Legs
        {
            get { return 2; }
        }
    }

    public class Pool<T>
    {
        readonly Stack<T> items = new Stack<T>();
        readonly Func<T> factory;
        public int Created;

        public Pool(Func<T> factory)
        {
            this.factory = factory;
        }

        public T Get()
        {
            if (items.Count > 0)
            {
                return items.Pop();
            }
            Created++;
            return factory();
        }

        public void Release(T item)
        {
            items.Push(item);
        }
    }

    public struct Point
    {
        public int X, Y;

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int ManhattanTo(Point other)
        {
            return Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
        }

        public void Move(int dx, int dy)
        {
            X += dx;
            Y += dy;
        }
    }

    public class GameException : Exception
    {
        public int Code;

        public GameException(string msg, int code) : base(msg)
        {
            Code = code;
        }
    }

    public static class LangCases
    {
        public static int CheckCount()
        {
            return 15;
        }

        public static string Check(int id)
        {
            switch (id)
            {
                case 0:
                    {
                        int counter = 0;
                        Func<int, int> addAndCount = x => { counter++; return x + counter; };
                        int r = addAndCount(10) + addAndCount(10);
                        Action reset = () => counter = 100;
                        reset();
                        return "closure: " + r + " " + counter;
                    }
                case 1:
                    {
                        int threshold = 3;
                        var list = new List<int> { 5, 1, 4, 2, 3 };
                        list.Sort((x, y) => y.CompareTo(x));
                        var filtered = list.FindAll(x => x >= threshold);
                        var doubled = list.ConvertAll(x => x * 2);
                        return "delegate from native: " + string.Join(",", list.Select(x => x.ToString()).ToArray())
                            + " " + filtered.Count + " " + doubled[0];
                    }
                case 2:
                    {
                        var nums = Enumerable.Range(0, 20);
                        int sum = nums.Where(x => x % 3 == 0).Select(x => x * x).Sum();
                        var groups = nums.GroupBy(x => x % 4).OrderBy(g => g.Key)
                            .Select(g => g.Key + ":" + g.Count()).ToArray();
                        var dict = nums.Take(5).ToDictionary(x => "k" + x, x => x * 10);
                        return "LINQ: " + sum + " " + string.Join(" ", groups) + " " + dict["k3"] + " "
                            + nums.Any(x => x > 18) + " " + nums.First(x => x > 7) + " "
                            + nums.Skip(15).Max() + " " + new[] { "b", "c", "a" }.OrderBy(s => s).First();
                    }
                case 3:
                    {
                        var emitter = new Emitter();
                        int total = 0;
                        Action<int> handler = v => total += v;
                        emitter.OnValue += handler;
                        emitter.OnValue += v => total += v * 100;
                        emitter.Emit(1);
                        emitter.OnValue -= handler;
                        emitter.Emit(2);
                        return "events: " + total;
                    }
                case 4:
                    {
                        var pool = new Pool<StringBuilder>(() => new StringBuilder());
                        var a = pool.Get();
                        var b = pool.Get();
                        pool.Release(a);
                        var c = pool.Get();
                        var box = new Box<int>(5);
                        box.Set(box.Get() * 3);
                        var sbox = new Box<string>("x");
                        return "generics: " + pool.Created + " " + (c == a) + " " + (b != a) + " " + box.Get() + " "
                            + sbox.Get() + " " + Max(3, 9) + " " + Max("pear", "apple");
                    }
                case 5:
                    {
                        Animal[] zoo = { new Dog(), new Bird() };
                        var sb = new StringBuilder("inheritance:");
                        foreach (var an in zoo)
                        {
                            sb.Append(' ').Append(an.Speak()).Append('/').Append(an.Legs).Append('/')
                                .Append(an is Dog).Append('/').Append(an.GetType().Name);
                        }
                        return sb.ToString();
                    }
                case 6:
                    {
                        var log = new List<string>();
                        try
                        {
                            try
                            {
                                log.Add("try");
                                throw new GameException("bad", 7);
                            }
                            //IFix doesn't support exception filters (catch ... when, endfilter)
                            catch (GameException e)
                            {
                                log.Add("catch" + e.Code);
                                throw;
                            }
                            finally
                            {
                                log.Add("finally");
                            }
                        }
                        catch (Exception e)
                        {
                            log.Add("outer:" + e.Message + ":" + e.GetType().Name);
                        }
                        try
                        {
                            object o = null;
                            o.ToString();
                        }
                        catch (NullReferenceException)
                        {
                            log.Add("npe");
                        }
                        try
                        {
                            int[] arr = new int[2];
                            arr[3] = 1;
                        }
                        catch (IndexOutOfRangeException)
                        {
                            log.Add("ioor");
                        }
                        return "exceptions: " + string.Join(",", log.ToArray());
                    }
                case 7:
                    {
                        var list = new List<int> { 1, 2, 3 };
                        int s = 0;
                        foreach (var x in list)
                        {
                            s += x;
                        }
                        var dict = new Dictionary<int, string> { { 1, "a" }, { 2, "b" } };
                        foreach (var kv in dict)
                        {
                            s += kv.Key * kv.Value.Length;
                        }
                        foreach (var y in Evens(10))
                        {
                            s += y;
                        }
                        IEnumerator it = Evens(4).GetEnumerator();
                        while (it.MoveNext())
                        {
                            s += (int)it.Current * 1000;
                        }
                        return "foreach/iterator: " + s;
                    }
                case 8:
                    {
                        object o = 42;
                        int unboxed = (int)o + 1;
                        var mixed = new List<object> { 1, "two", 3.0, 'c', null, Target.Color.Blue };
                        var sb = new StringBuilder("boxing:");
                        foreach (var m in mixed)
                        {
                            sb.Append(' ').Append(m is int ? "int" : m is string ? "str" : m == null ? "null"
                                : m.GetType().Name);
                        }
                        IComparable cmp = 5;
                        return sb.ToString() + " " + unboxed + " " + cmp.CompareTo(3) + " " + o.Equals(42);
                    }
                case 9:
                    {
                        var p = new Point(1, 2);
                        var q = p;
                        q.Move(3, 4);
                        Point[] arr = { p, q };
                        arr[0].Move(10, 0);
                        return "struct: " + p.X + "," + p.Y + " " + q.X + "," + q.Y + " " + arr[0].X + " "
                            + p.ManhattanTo(q);
                    }
                case 10:
                    {
                        int half, a = 1, b = 2;
                        bool even = Api.TryHalf(9, out half);
                        Api.Swap(ref a, ref b);
                        int parsed;
                        bool ok = int.TryParse("123", out parsed);
                        return "ref/out: " + even + " " + half + " " + a + "," + b + " " + ok + " " + parsed;
                    }
                case 11:
                    {
                        var sb = new StringBuilder("switch:");
                        foreach (var cmd in new[] { "attack", "defend", "heal", "flee", "unknown" })
                        {
                            sb.Append(' ').Append(Dispatch(cmd));
                        }
                        return sb.ToString();
                    }
                case 12:
                    {
                        int? a = 5;
                        int? b = null;
                        int? c = a + b;
                        int? d = a * 2;
                        return "nullable: " + c.HasValue + " " + d + " " + (b ?? -1) + " " + a.GetValueOrDefault();
                    }
                case 13:
                    {
                        var c = new Counter();
                        c.Value = 5;
                        c.Increment();
                        Counter.StaticValue = 7;
                        Counter.StaticValue += c.Value;
                        return "fields: " + c.Value + " " + Counter.StaticValue;
                    }
                case 14:
                    {
                        //Access through null: native code throws NullReferenceException, the VM must match
                        var log = new List<string>();
                        Counter counter = null;
                        Square square = null;
                        string str = null;
                        Animal animal = null;
                        Point[] points = null;
                        log.Add(nullAccess(() => counter.Increment()));
                        log.Add(nullAccess(() => square.Area(1)));
                        log.Add(nullAccess(() => str.Length));
                        log.Add(nullAccess(() => counter.Value));
                        log.Add(nullAccess(() => { counter.Value = 1; return 0; }));
                        log.Add(nullAccess(() => animal.Legs));
                        log.Add(nullAccess(() => points[0].X));
                        return "null access: " + string.Join(",", log.ToArray());
                    }
                default:
                    return "unknown";
            }
        }

        static string nullAccess(Func<int> f)
        {
            try
            {
                return "none" + f();
            }
            catch (Exception e)
            {
                return e.GetType().Name;
            }
        }

        static T Max<T>(T a, T b) where T : IComparable<T>
        {
            return a.CompareTo(b) >= 0 ? a : b;
        }

        static IEnumerable<int> Evens(int max)
        {
            for (int i = 0; i < max; i += 2)
            {
                yield return i;
            }
        }

        static string Dispatch(string cmd)
        {
            switch (cmd)
            {
                case "attack":
                    return "A";
                case "defend":
                    return "D";
                case "heal":
                    return "H";
                case "flee":
                    return "F";
                default:
                    return "?";
            }
        }

        //A lambda created in the VM is called back by native code (List.Sort)
        public static long PerfNativeCallback(int n)
        {
            var list = new List<int>(64);
            long r = 0;
            for (int round = 0; round < n / 64; round++)
            {
                list.Clear();
                for (int i = 0; i < 64; i++)
                {
                    list.Add((i * 37 + round) & 63);
                }
                list.Sort((x, y) => x - y);
                r += list[round & 63];
            }
            return r;
        }

        //LINQ: n elements
        public static long PerfLinq(int n)
        {
            var data = new int[100];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = i;
            }
            long r = 0;
            for (int round = 0; round < n / 100; round++)
            {
                r += data.Where(x => (x & 1) == 0).Select(x => x * 2).Sum();
            }
            return r;
        }

        //foreach List<T>: MoveNext/Current of the struct enumerator
        public static long PerfForeachList(int n)
        {
            var list = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 };
            long r = 0;
            for (int round = 0; round < n / 8; round++)
            {
                foreach (var x in list)
                {
                    r += x;
                }
            }
            return r;
        }

        //An event raised by native code calls back a lambda in the VM
        public static long PerfEventCallback(int n)
        {
            var emitter = new Emitter();
            long total = 0;
            emitter.OnValue += v => total += v;
            for (int i = 0; i < n; i++)
            {
                emitter.Emit(i & 7);
            }
            return total;
        }

        //Virtual call inside the VM (virtual method of an injected type)
        public static long PerfInternalVirtual(int n)
        {
            Animal a = new Dog();
            long r = 0;
            for (int i = 0; i < n; i++)
            {
                r += a.Legs;
            }
            return r;
        }
    }
}
