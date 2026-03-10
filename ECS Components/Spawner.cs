using ILGPU.Util;
using ShortTools.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BOIDSimulator.ECS_Components
{
    internal struct EC_SpawnerLogic : IEntityComponent
    {
        public bool Active { get => active; set => active = value; }
        private bool active = true;

        public const int max = 10000;
        public int current = 0;

        public HashSet<int> spawnedUids = new HashSet<int>();

        public float MaxSpawnsPerSecond 
        { 
            get => maxSpawnsPerSecond; 
            set 
            { 
                maxSpawnsPerSecond = value;
                secondsPerSpawn = 1f / value;
            } 
        }
        private float maxSpawnsPerSecond = 10f; // should only be set via the property

        private Func<int> creatorFunc;
        private float spawnTimer = 0;
        private float secondsPerSpawn = 0;

        public EC_SpawnerLogic(Func<int> creatorFunc, float maxSpawnsPerSecond) 
        { 
            this.creatorFunc = creatorFunc;
            this.MaxSpawnsPerSecond = maxSpawnsPerSecond;
        }

        public void Action(float dt, int uid)
        {
            spawnTimer += dt;
            if (current >= max) { return; }
            while (spawnTimer > secondsPerSpawn)
            {
                spawnTimer -= secondsPerSpawn;
                int spawnedUid = creatorFunc();
                _ = spawnedUids.Add(spawnedUid);

                current++;
            }
        }

        public void Cleanup(int uid)
        {
            lock (spawnedUids)
            {
                foreach (int spawnedUid in spawnedUids)
                {
                    bool success = ECSHandler.GetEntityComponent(spawnedUid, out EC_SpawnedLogic logic);
                    if (!success) { ECSHandler.debugger.AddLog($"Spawned entity reference has an issue!", WarningLevel.Error); }
                    logic.spawnerUid = -1;
                }
            }
        }
    }
}
