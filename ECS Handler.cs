using BOIDSimulator.ECS_Components;
using System.Runtime.CompilerServices;




namespace BOIDSimulator
{
    internal static class ECSHandler
    {
        // <<Public Variables>> //
        internal static List<bool> entities = new List<bool>();
        internal static Dictionary<Type, List<IEntityComponent?>> ECSs = CreateECSs();

        internal static Thread controllerThread = new Thread(new ThreadStart(RunLoop));

        internal static HashSet<(int, int)> updatedGrids = new HashSet<(int, int)>();

        internal static bool running = true;




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
        private static long LFT = DateTimeOffset.Now.ToUnixTimeMilliseconds();// last frame time
        private const int MaxFPS = 60;
        private const long MaxMsPerFrame = 1000 / MaxFPS;
        public static void RunLoop()
        {
            while (running)
            {
                long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                float dt = (now - LFT) / 1000f;
                LFT = now;

                Run(dt);
            }
        }
        
        
        
        
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
                Renderer.RequestDrawGrid(coordinate.Item1, coordinate.Item2);
            }
        }


        private static Type renderType = typeof(EC_Render);
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
