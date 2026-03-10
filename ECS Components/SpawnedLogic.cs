using ShortTools.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BOIDSimulator.ECS_Components
{
    internal struct EC_SpawnedLogic : IEntityComponent
    {
        public bool Active { get => active; set => active = value; }
        private bool active = false;

        public int spawnerUid = -1;
        public EC_SpawnedLogic(int uid, int spawnerUid) 
        { 
            this.spawnerUid = spawnerUid; 
            if (spawnerUid == -1) { ECSHandler.debugger.AddLog($"Given spawner UID was -1, something went wrong", WarningLevel.Error); return; }

            
            bool success = ECSHandler.GetEntityComponent(spawnerUid, out EC_SpawnerLogic spawner);
            if (!success) { ECSHandler.debugger.AddLog($"Given spawner {spawnerUid} did not have a spawner logic component!", WarningLevel.Error); return; }

            lock (spawner.spawnedUids)
            {
                _ = spawner.spawnedUids.Add(uid);
            }
        }


        public void Action(float dt, int uid) { }

        public void Cleanup(int uid)
        {
            if (ECSHandler.entities[spawnerUid] == false || spawnerUid == -1) 
            { ECSHandler.debugger.AddLog($"Attempted to access closed spawner.", WarningLevel.Debug); return; } // spawner is closed

            EC_SpawnerLogic? spawnerLogicNullable = (EC_SpawnerLogic?)ECSHandler.ECSs[typeof(EC_SpawnerLogic)][spawnerUid];

            if (spawnerLogicNullable is null) { ECSHandler.debugger.AddLog($"Spawner logic was null?", WarningLevel.Warning); return; }

            EC_SpawnerLogic spawnerLogic = (EC_SpawnerLogic)spawnerLogicNullable;
            spawnerLogic.current--;
            ECSHandler.ECSs[typeof(EC_SpawnerLogic)][spawnerUid] = spawnerLogic;
        }
    }
}
