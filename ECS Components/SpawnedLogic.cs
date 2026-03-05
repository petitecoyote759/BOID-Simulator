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

        int spawnerUid = 0;
        public EC_SpawnedLogic(int spawnerUid) { this.spawnerUid = spawnerUid; }


        public void Action(float dt, int uid) { }

        public void Cleanup(int uid)
        {
            if (ECSHandler.entities[spawnerUid] == false) { General.debugger.AddLog($"Attempted to access closed spawner.", WarningLevel.Debug); return; } // spawner is closed

            EC_SpawnerLogic? spawnerLogicNullable = (EC_SpawnerLogic?)ECSHandler.ECSs[typeof(EC_SpawnerLogic)][spawnerUid];

            if (spawnerLogicNullable is null) { General.debugger.AddLog($"Spawner logic was null? Has it been replaced?", WarningLevel.Debug); return; }

            EC_SpawnerLogic spawnerLogic = (EC_SpawnerLogic)spawnerLogicNullable;
            spawnerLogic.current--;
            ECSHandler.ECSs[typeof(EC_SpawnerLogic)][spawnerUid] = spawnerLogic;
        }
    }
}
