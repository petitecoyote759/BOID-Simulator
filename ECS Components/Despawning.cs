using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ShortTools.General;

namespace BOIDSimulator.ECS_Components
{
    internal struct EC_Despawning : IEntityComponent
    {
        // <<Requirements>> //
        // EC_Entity //

        // <<Public Variables>> //

        public float rangeSquared;

        public bool Active { get => active; set => active = value; }
        private bool active = true;



        // <<Constants>> //

        const float totalBoidLifespanSeconds = 120;



        // <<Modified Constants>> //

        const long totalBoidLifespan = (int)(1000 * totalBoidLifespanSeconds);




        // <<Private Variables>> //

        private readonly long creationTime;






        public EC_Despawning(float rangeSquared)
        {
            this.rangeSquared = rangeSquared;
            this.creationTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }


        public readonly void Action(float dt, int uid)
        {
            EC_Entity? Me = (EC_Entity?)ECSHandler.ECSs[typeof(EC_Entity)][uid];
            if (Me is null) { General.debugger.AddLog($"Error, entity {uid} has no entity data!", WarningLevel.Error); return; }

            int tileX = Me.Value.tileX;
            int tileY = Me.Value.tileY;


            // <<Destroy at Centre>> //

            EC_BoidLogic? boidLogic = (EC_BoidLogic?)ECSHandler.ECSs[typeof(EC_BoidLogic)][uid];
            if (boidLogic is not null)
            {
                int targetX = boidLogic.Value.targetX;
                int targetY = boidLogic.Value.targetY;
                if (MathF.Abs(targetX - tileX) + MathF.Abs(targetY - tileY) < rangeSquared)
                {
                    ECSHandler.FreeUID(uid);
                    return;
                }
            }
            


            // <<Destroy After Lifespan>> //

            if (DateTimeOffset.Now.ToUnixTimeMilliseconds() - creationTime > totalBoidLifespan)
            {
                ECSHandler.FreeUID(uid);
                return;
            }
        }
    }
}
