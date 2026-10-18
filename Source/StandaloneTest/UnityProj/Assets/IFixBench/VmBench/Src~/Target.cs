using System;

//External methods called by the tested code (Cases.cs); not injected, so calls from the VM are extern calls
namespace IFixBench.Target
{
    public interface IShape
    {
        int Area(int scale);
    }

    public abstract class ShapeBase : IShape
    {
        public abstract int Area(int scale);

        public virtual string Name()
        {
            return "base";
        }
    }

    public class Square : ShapeBase
    {
        public int Side;

        public Square(int side)
        {
            Side = side;
        }

        public override int Area(int scale)
        {
            return Side * Side * scale;
        }

        public override string Name()
        {
            return "square";
        }
    }

    public struct V3
    {
        public float x, y, z;

        public V3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public float Length()
        {
            return (float)Math.Sqrt(x * x + y * y + z * z);
        }

        public override string ToString()
        {
            return "(" + x + "," + y + "," + z + ")";
        }
    }

    public enum Color
    {
        Red = 1,
        Blue = 7
    }

    public enum Big : long
    {
        A = 1L << 40
    }

    public static class Api
    {
        public static int Counter { get; set; }

        public static int Add(int a, int b)
        {
            return a + b;
        }

        public static double Mix(long a, double b, bool c, byte d)
        {
            return a + b + (c ? 100 : 0) + d;
        }

        public static uint U(uint a, short b, char c)
        {
            return a + (uint)b + c;
        }

        public static V3 Scale(V3 v, float f)
        {
            return new V3(v.x * f, v.y * f, v.z * f);
        }

        public static float Sum(V3 v)
        {
            return v.x + v.y + v.z;
        }

        public static Color Next(Color c)
        {
            return c == Color.Red ? Color.Blue : Color.Red;
        }

        public static Big Twice(Big b)
        {
            return (Big)((long)b * 2);
        }

        public static int? Nul(int? a)
        {
            return a.HasValue ? a * 2 : null;
        }

        public static string Join(object a, string b)
        {
            return a + "|" + b;
        }

        public static void Inc(ref int a)
        {
            a++;
        }

        public static T Echo<T>(T t)
        {
            return t;
        }

        public static Func<int, int> MakeDoubler()
        {
            return x => x * 2;
        }

        public static void Throw()
        {
            throw new InvalidOperationException("boom");
        }

        public static long Id64(long a)
        {
            return a;
        }

        public static bool TryHalf(int a, out int half)
        {
            half = a / 2;
            return a % 2 == 0;
        }

        public static void Swap(ref int a, ref int b)
        {
            int t = a;
            a = b;
            b = t;
        }
    }

    //Field reads/writes (FieldInfo in the VM)
    public class Counter
    {
        public static int StaticValue;

        public int Value;

        public int Increment()
        {
            return ++Value;
        }
    }

    public class Emitter
    {
        public event Action<int> OnValue;

        public void Emit(int v)
        {
            if (OnValue != null)
            {
                OnValue(v);
            }
        }
    }

    public class Box<T>
    {
        T value;

        public Box(T value)
        {
            this.value = value;
        }

        public T Get()
        {
            return value;
        }

        public void Set(T v)
        {
            value = v;
        }
    }
}
