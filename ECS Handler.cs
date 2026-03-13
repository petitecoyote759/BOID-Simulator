using BOIDSimulator.ECS_Components;
using ShortTools.General;
using System.Runtime.CompilerServices;
using BOIDSimulator.Renderer;



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

        internal static Debugger debugger = new Debugger("ECS",
#if DEBUG
                WarningLevel.Debug,
#else
                WarningLevel.Info,
#endif
                DebuggerFlag.PrintLogs, DebuggerFlag.WriteLogsToFile, DebuggerFlag.DisplayThread);




        // <<Entity Management Functions>> //
        private static Dictionary<Type, List<IEntityComponent?>> CreateECSs()
        {
            return new Dictionary<Type, List<IEntityComponent?>>()
            {
              { typeof(EC_SpawnerLogic), new List<IEntityComponent?>() },
              { typeof(EC_SpawnedLogic), new List<IEntityComponent?>() },
              { typeof(EC_Despawning), new List<IEntityComponent?>() },
              { typeof(EC_BoidLogic), new List<IEntityComponent?>() },
              { typeof(EC_Entity), new List<IEntityComponent?>() },
              { typeof(EC_PathFinding), new List<IEntityComponent?>() },
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
            foreach (KeyValuePair<Type, List<IEntityComponent?>> pair in ECSs)
            {
                pair.Value[uid]?.Cleanup(uid);
            }
        }



        // <<Main Functions>> //
        public static long LFT = DateTimeOffset.Now.ToUnixTimeMilliseconds();// last frame time
        private const int MaxFPS = 120;
        private const long MaxMsPerFrame = 1000 / MaxFPS;
        private const int secondsPerFPSUpdate = 10;
        private const long ticksPerFPSUpdate = secondsPerFPSUpdate * 1000;
        private static int frameCount = 0;
        private static long FPSUpateTimer = 0;
        public static void RunLoop()
        {
            controllerThread.Name = "ECS Controller Thread";

            while (running)
            {
                long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                long delta = now - LFT;
                int makeupTime = (int)(MaxMsPerFrame - delta); // the amount of time 
                if (makeupTime > 0)
                {
                    Thread.Sleep(makeupTime);
                }
                float dt = delta / 1000f;
                LFT = now;

                frameCount++;
                FPSUpateTimer += delta;
                if (FPSUpateTimer > ticksPerFPSUpdate)
                {
                    FPSUpateTimer -= ticksPerFPSUpdate;
                    debugger.AddLog($"ECS Frame Count {frameCount} over {secondsPerFPSUpdate} giving {frameCount / secondsPerFPSUpdate} FPS", WarningLevel.Debug);
                    frameCount = 0;
                }

                Run(dt);
            }
            debugger.AddLog($"Shutting down ECS", WarningLevel.Info);
            debugger.Dispose(true);
        }
        
        public static bool IsClosed(int uid)
        {
            if (uid < 0 || uid >= entities.Count) { return false; }
            return entities[uid];
        }
        
        
        // Called by renderer thread for synchronisation
        public static void DoEntityRenderTasks(float dt)
        {
            int length = entities.Count;

            for (int uid = 0; uid < length; uid++)
            {
                if (entities[uid] == false) { continue; } // entity is closed

                if (ECSs[renderType][uid] is null) { continue; } // Entity does not have the component.
                if (ECSs[renderType][uid]?.Active == false) { continue; } // Module is disabled
                ECSs[renderType][uid]?.Action(dt, uid);
            }

            lock (updatedGrids)
            {
                foreach ((int, int) coordinate in updatedGrids)
                {
                    RendererTools.RequestDrawGrid(coordinate.Item1, coordinate.Item2);
                }
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
        }


        private static Type renderType = typeof(EC_Render);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RunEntitiy(int uid, float dt)
        {
            foreach (KeyValuePair<Type, List<IEntityComponent?>> pair in ECSs)
            {
                if (pair.Value[uid] is null) { continue; } // Entity does not have the component.
                if (pair.Value[uid]?.Active == false) { continue; } // Module is disabled
                if (pair.Key == renderType) { continue; } // Render is done on a seperate thread
                pair.Value[uid]?.Action(dt, uid);
            }
        }






        public static bool GetEntityComponent<T>(int uid, out T component) where T : IEntityComponent
        {
            T? nullableComponent = (T?)ECSs[typeof(T)][uid];
            if (nullableComponent is null) { component = default(T); return false; }

            component = (T)nullableComponent;
            return true;
        }

        public static bool SetEntitiyComponent<T>(int uid, T component) where T : IEntityComponent
        {
            Type componentType = typeof(T);

            if (ECSs[componentType] is null) { return false; }

            ECSs[typeof(T)][uid] = component;
            return true;
        }
    }

}
