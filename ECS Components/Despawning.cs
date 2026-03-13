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

        public float deletionRange;

        public bool Active { get => active; set => active = value; }
        private bool active = true;
        private int frameCount = 0;


        // <<Constants>> //

        const float totalBoidLifespanSeconds = 120;
        const int framesPerCheck = 5;


        // <<Modified Constants>> //

        const long totalBoidLifespan = (int)(1000 * totalBoidLifespanSeconds);




        // <<Private Variables>> //

        private readonly long creationTime;






        public EC_Despawning(float deletionRange)
        {
            this.deletionRange = deletionRange;
            this.creationTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }


        public void Action(float dt, int uid)
        {
            frameCount++;
            if (frameCount < framesPerCheck) { return; }
            frameCount -= framesPerCheck; 


            bool success = ECSHandler.GetEntityComponent(uid, out EC_Entity Me);
            if (!success) { ECSHandler.debugger.AddLog($"Error, entity {uid} has no entity data!", WarningLevel.Error); return; }

            int tileX = Me.tileX;
            int tileY = Me.tileY;


            // <<Destroy at Centre>> //

            success = ECSHandler.GetEntityComponent(uid, out EC_BoidLogic boidLogic);
            if (success)
            {
                int targetX = EC_BoidLogic.targetX;
                int targetY = EC_BoidLogic.targetY;
                if (MathF.Abs(targetX - tileX) + MathF.Abs(targetY - tileY) < deletionRange)
                {
                    ECSHandler.FreeUID(uid);
                    return;
                }
            }
            


            // <<Destroy After Lifespan>> //

            if (ECSHandler.LFT - creationTime > totalBoidLifespan)
            {
                ECSHandler.FreeUID(uid);
                return;
            }
        }



        public void Cleanup(int uid) { }
    }
}
