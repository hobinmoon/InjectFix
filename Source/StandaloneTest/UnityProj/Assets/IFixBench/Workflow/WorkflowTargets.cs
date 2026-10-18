using System;
using System.Collections.Generic;
using System.Linq;
using IFix;
using UnityEngine;

//Real IFix workflow: the player is built from the original (buggy) code, and the patch is generated from the code with IFIX_PATCHED defined ([Patch]/[Interpret]).
//WorkflowCfg (Editor) configures these types for injection.
namespace IFixBench.Workflow
{
    public class WfCalculator
    {
#if IFIX_PATCHED
        [Patch]
        public int Add(int a, int b)
        {
            return a + b;
        }
#else
        public int Add(int a, int b)
        {
            return a * b;
        }
#endif

        public int Sub(int a, int b)
        {
            return a - b;
        }
    }

    public static class WfMath
    {
#if IFIX_PATCHED
        [Patch]
        public static float Damage(float atk, float def)
        {
            return Mathf.Max(1f, atk * 1.5f - def);
        }
#else
        public static float Damage(float atk, float def)
        {
            return atk - def;
        }
#endif
    }

    //Method on a MonoBehaviour; the patch uses Unity APIs
    public class WfMover : MonoBehaviour
    {
        public Vector3 velocity = new Vector3(1, 0, 0);

#if IFIX_PATCHED
        [Patch]
        public void Step(float dt)
        {
            transform.position += velocity * dt;
            transform.rotation = Quaternion.LookRotation(velocity);
        }
#else
        public void Step(float dt)
        {
            transform.position += velocity;
        }
#endif
    }

    //The patch uses lambdas/LINQ
    public class WfInventory
    {
        public List<int> prices = new List<int> { 30, 10, 50, 20 };

#if IFIX_PATCHED
        [Patch]
        public string Cheapest(int count)
        {
            return string.Join(",", prices.OrderBy(p => p).Take(count).Select(p => p.ToString()).ToArray());
        }
#else
        public string Cheapest(int count)
        {
            return string.Join(",", prices.Take(count).Select(p => p.ToString()).ToArray());
        }
#endif

        //A Func<int, int> closure already exists in the original build, so OrderBy(p => p) in the patch can find an adapter
        public int Apply(Func<int, int> f)
        {
            return f(prices[0]);
        }

        public int Twice()
        {
            return Apply(p => p * 2);
        }
    }

    //Property getter
    public class WfPlayer
    {
        public int level = 3;

        public int MaxHp
        {
#if IFIX_PATCHED
            [Patch]
            get { return 100 + level * 20; }
#else
            get { return 100 + level * 2; }
#endif
        }
    }

#if IFIX_PATCHED
    //Class added by the patch
    [Interpret]
    public class WfBonus
    {
        public int Value(int lv)
        {
            return lv * lv;
        }
    }
#endif

    public class WfReward
    {
#if IFIX_PATCHED
        [Patch]
        public int Gold(int lv)
        {
            return 10 * lv + new WfBonus().Value(lv);
        }
#else
        public int Gold(int lv)
        {
            return 10 * lv;
        }
#endif
    }

    public class WfEnemyBase
    {
        public virtual string Describe()
        {
            return "enemy";
        }
    }

    //Override method; the patch calls base
    public class WfOrc : WfEnemyBase
    {
#if IFIX_PATCHED
        [Patch]
        public override string Describe()
        {
            return "orc<" + base.Describe() + ">";
        }
#else
        public override string Describe()
        {
            return "orc";
        }
#endif
    }

    //Same logic, just patched: compare the time of the same code run natively and by the VM
    public static class WfHot
    {
#if IFIX_PATCHED
        [Patch]
#endif
        public static float Accumulate(Vector3[] pts)
        {
            float r = 0;
            for (int i = 0; i < pts.Length; i++)
            {
                r += Vector3.Dot(pts[i], Vector3.up) + Mathf.Abs(pts[i].x);
            }
            return r;
        }

#if IFIX_PATCHED
        [Patch]
#endif
        public static int Mix(int a, int b)
        {
            return (a ^ b) + (a & 7);
        }
    }
}
