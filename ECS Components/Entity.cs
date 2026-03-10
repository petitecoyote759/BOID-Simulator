using ShortTools.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BOIDSimulator.ECS_Components
{
    public class EntityReference
    {
        public int targetUid;
        public int refereeUid;
        public bool closed;

        public EntityReference(int targetUid, int refereeUid)
        {
            this.targetUid = targetUid;
            this.refereeUid = refereeUid;
            this.closed = false;
        }

        public void Close(bool calledFromTarget)
        {
            if (calledFromTarget == false)
            {
                bool success = ECSHandler.GetEntityComponent(targetUid, out EC_Entity entityData);
                if (!success) { ECSHandler.debugger.AddLog($"Entity {targetUid} had no entity data!", WarningLevel.Warning); return; }
                _ = entityData.selfReferences.Remove(this);
            }

            targetUid = -1;
            refereeUid = -1;
            closed = true;
        }
    }



    internal struct EC_Entity : IEntityComponent
    {
        public bool Active { get => active; set => active = value; }
        private bool active = true;

        // <<Variables>> //
        public Vector2 position = new Vector2(0, 0);
        public int tileX = 0;
        public int tileY = 0;

        public HashSet<EntityReference> selfReferences = new HashSet<EntityReference>(); // all references to me

        public EC_Entity()
        {

        }

        public void Action(float dt, int uid) 
        {
            if (Map.tileMap is null) { return; }

            // <<Update Positions>> //

            // Bounds checks
            position.X = float.Clamp(position.X, 0, Map.tileMap.Length);
            position.Y = float.Clamp(position.Y, 0, Map.tileMap[0].Length);

            tileX = (int)position.X;
            tileY = (int)position.Y;


        }

        public void Cleanup(int uid) 
        { 
            foreach (EntityReference reference in selfReferences)
            {
                reference.Close(true);
            }
            selfReferences.Clear();
        }
    }
}
