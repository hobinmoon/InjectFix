using System.Collections.Generic;
using System.Text;
using UnityEngine;

//Mixed workload close to real patches: entity movement, combat, inventory, sorting, string building
#if IFIX_INTERP
namespace IFixBench.Interp
#else
namespace IFixBench.Native
#endif
{
    public class Entity
    {
        public int Id;
        public string Name;
        public Vector3 Pos;
        public Vector3 Vel;
        public int Hp;
        public int Atk;
        public Dictionary<string, int> Inventory = new Dictionary<string, int>();

        public virtual int Attack(Entity target, float mult)
        {
            return Mathf.Max(1, Mathf.RoundToInt(Atk * mult) - target.Id % 3);
        }

        public void AddItem(string item, int count)
        {
            int old;
            Inventory.TryGetValue(item, out old);
            Inventory[item] = old + count;
        }
    }

    public class Boss : Entity
    {
        public override int Attack(Entity target, float mult)
        {
            return base.Attack(target, mult) * 2 + 1;
        }
    }

    public static class GameCases
    {
        public static int CheckCount()
        {
            return 3;
        }

        public static string Check(int id)
        {
            switch (id)
            {
                case 0:
                    {
                        var world = Spawn(50);
                        for (int t = 0; t < 20; t++)
                        {
                            Tick(world, 0.1f);
                            Combat(world, t);
                        }
                        return "simulation: " + Summary(world);
                    }
                case 1:
                    {
                        var e = new Entity { Id = 1, Name = "bag" };
                        string[] items = { "gold", "potion", "arrow", "gold", "gem", "arrow", "gold" };
                        for (int i = 0; i < items.Length; i++)
                        {
                            e.AddItem(items[i], i + 1);
                        }
                        var keys = new List<string>(e.Inventory.Keys);
                        keys.Sort(string.CompareOrdinal);
                        var sb = new StringBuilder("inventory:");
                        foreach (var k in keys)
                        {
                            sb.Append(' ').Append(k).Append('=').Append(e.Inventory[k]);
                        }
                        return sb.ToString();
                    }
                case 2:
                    return "formatting: " + string.Format("{0,-6}|{1:D4}|{2:F2}|{3}", "hero", 42, 3.14159f, true)
                        + " " + 255.ToString("X") + " " + string.Join(",", new[] { "a", "b", "c" })
                        + " " + $"{7 * 6} {Vector3.up}";
                default:
                    return "unknown";
            }
        }

        static List<Entity> Spawn(int count)
        {
            var list = new List<Entity>(count);
            for (int i = 0; i < count; i++)
            {
                Entity e = i % 10 == 0 ? new Boss() : new Entity();
                e.Id = i;
                e.Name = "e" + i;
                e.Pos = new Vector3(i % 7, 0, i % 5);
                e.Vel = new Vector3((i % 3) - 1, 0, (i % 4) - 1.5f);
                e.Hp = 100 + i % 17;
                e.Atk = 5 + i % 6;
                list.Add(e);
            }
            return list;
        }

        static void Tick(List<Entity> world, float dt)
        {
            for (int i = 0; i < world.Count; i++)
            {
                var e = world[i];
                e.Pos += e.Vel * dt;
                if (Mathf.Abs(e.Pos.x) > 10)
                {
                    e.Vel = new Vector3(-e.Vel.x, e.Vel.y, e.Vel.z);
                }
            }
        }

        static void Combat(List<Entity> world, int turn)
        {
            int n = world.Count;
            for (int i = 0; i < n; i++)
            {
                var attacker = world[i];
                var target = world[(i * 7 + turn) % n];
                if (attacker == target || attacker.Hp <= 0 || target.Hp <= 0)
                {
                    continue;
                }
                float mult = Mathf.Lerp(0.5f, 1.5f, (Vector3.Distance(attacker.Pos, target.Pos) % 10) / 10f);
                target.Hp -= attacker.Attack(target, mult);
                if (target.Hp <= 0)
                {
                    attacker.AddItem("gold", target.Atk);
                }
            }
        }

        static string Summary(List<Entity> world)
        {
            int alive = 0;
            int gold = 0;
            for (int i = 0; i < world.Count; i++)
            {
                if (world[i].Hp > 0)
                {
                    alive++;
                }
                int g;
                if (world[i].Inventory.TryGetValue("gold", out g))
                {
                    gold += g;
                }
            }
            var sorted = new List<Entity>(world);
            sorted.Sort((x, y) => x.Hp != y.Hp ? y.Hp.CompareTo(x.Hp) : x.Id.CompareTo(y.Id));
            var sb = new StringBuilder();
            sb.AppendFormat("alive={0} gold={1} top=", alive, gold);
            for (int i = 0; i < 3; i++)
            {
                sb.Append(sorted[i].Name).Append(':').Append(sorted[i].Hp).Append(' ');
            }
            return sb.ToString();
        }

        //n entity updates (100 entities, n/100 frames)
        public static long PerfGameTick(int n)
        {
            var world = Spawn(100);
            int frames = n / 100;
            for (int t = 0; t < frames; t++)
            {
                Tick(world, 0.016f);
                Combat(world, t);
                for (int i = 0; i < world.Count; i++)
                {
                    if (world[i].Hp <= 0)
                    {
                        world[i].Hp = 100;
                    }
                }
            }
            long r = 0;
            for (int i = 0; i < world.Count; i++)
            {
                r += world[i].Hp;
            }
            return r;
        }
    }
}
