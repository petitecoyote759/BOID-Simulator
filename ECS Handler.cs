using BOIDSimulator.ECS_Components;
using System.Runtime.CompilerServices;




namespace BOIDSimulator
{
    internal class ECSHandler
    {
        // <<Public Variables>> //
        internal static List<bool> entities = new List<bool>();
        internal static Dictionary<Type, List<IEntityComponent?>> ECSs = CreateECSs();



        internal static HashSet<(int, int)> updatedGrids = new HashSet<(int, int)>();




        // <<Entity Management Functions>> //
        private static Dictionary<Type, List<IEntityComponent?>> CreateECSs()
        {
            return new Dictionary<Type, List<IEntityComponent?>>()
            {
              { typeof(EC_Entity), new List<IEntityComponent?>() },
              { typeof(EC_Despawning), new List<IEntityComponent?>() },
              { typeof(EC_BoidLogic), new List<IEntityComponent?>() },
              { typeof(EC_Pathfinding), new List<IEntityComponent?>() },
              { typeof(EC_Render), new List<IEntityComponent?>() },
            };
        }
        // <UID Functions> //
        public static int GetUID()
        {
            int length = entities.Count;

            for (int i = 0; i < length; i++)
            {
                if (entities[i] == false) { return i; } // see if that space is free
            }
            entities.Add(true);
            foreach (KeyValuePair<Type, List<IEntityComponent?>> pair in ECSs)
            {
                pair.Value.Add(null);
            }
            return length;
        }
        public static void FreeUID(int uid)
        {
            if (uid >= entities.Count) { return; }

            entities[uid] = false;
        }



        // <<Main Functions>> //
        public static void Run(float dt)
        {
            updatedGrids = new HashSet<(int, int)>();

            int length = entities.Count;

            for (int i = 0; i < length; i++)
            {
                if (entities[i] == false) { continue; } // entity is closed

                RunEntitiy(i, dt);
            }

            foreach ((int, int) coordinate in updatedGrids)
            {
                Renderer.DrawGrid(coordinate.Item1, coordinate.Item2);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RunEntitiy(int uid, float dt)
        {
            foreach (KeyValuePair<Type, List<IEntityComponent?>> pair in ECSs)
            {
                if (pair.Value[uid] is null) { continue; } // Entity does not have the component.
                if (pair.Value[uid]?.Active == false) { continue; } // Module is disabled
                pair.Value[uid]?.Action(dt, uid);
            }
        }
    }
}
