using ShortTools.General;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Path = System.Collections.Generic.Queue<System.Numerics.Vector2>;

namespace BOIDSimulator.ECS_Components
{
    internal partial struct EC_BoidLogic : IEntityComponent
    {
        // <<Requirements>> //
        // EC_Entity //

        // <<Public Variabled>> //
        public Vector2 velocity = new Vector2(0, 0);
        public EntityReference? leaderReference;
        public float followMinDuration;

        public bool Active { get => active; set => active = value; }
        private bool active = true;


        public static int targetX = 0;
        public static int targetY = 0;


        public bool leader = false;




        // <<Private Variables>> //

        internal static List<int>[][] boidGrid = new List<int>[0][];

        // Cached counts
        internal static HashSet<int>[][] leaderGrid = Array.Empty<HashSet<int>[]>();



        // <<Constants>> //

        // Speeds
        const float leaderSpeed = 20f; // blocks per second
        const float followerSpeed = 1.15f * leaderSpeed;
        const float followerAcceleration = 25f;

        const float destroyZoneRadius = 10f; // how many blocks around the centre are the "kill zone", meaning the BOIDS will be deleted

        const int leaderDensityMin = 1; // If the leaders in a 3x3 area is less than this, then they will self promote,
        const int leaderDensityMax = 2; // however if over this value, it will stop being a leader

        const float alignmentConst = 0.2f;
        const int blockCheckRange = 6;
        const float seperationConst = 0.4f;

        // Leader Timegate
        private long leaderCreationTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        private long leaderFollowStartTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        const float minLeaderLifespanSeconds = 3; // minimum number of seconds a leader should live for
        const float minLeaderFollowSeconds = 3;

        const float totalBoidLifespanSeconds = 120;




        const float leaderNodeMinDistance = 0.5f; // How close a leader needs to be to a node for it to register as visited in blocks

        const float followerChargeRange = 80f;





        // <<Modified Constants>> //
        const float leaderNodeMinDistanceSquared = leaderNodeMinDistance * leaderNodeMinDistance; // Precompute this to speed up itterations

        const float followerSpeedSquared = followerSpeed * followerSpeed;



        const long minLeaderLifespan = (int)(1000 * minLeaderLifespanSeconds);
        const long minLeaderFollowDuration = (int)(1000 * minLeaderFollowSeconds);
        const long totalBoidLifespan = (int)(1000 * totalBoidLifespanSeconds);





        public EC_BoidLogic()
        {
            
        }






        public void Action(float dt, int uid)
        {
            EC_Entity? MeNullable = (EC_Entity?)ECSHandler.ECSs[typeof(EC_Entity)][uid];
            if (MeNullable is null) { ECSHandler.debugger.AddLog($"Error, entity {uid} has no entity data!", WarningLevel.Error); return; }
            EC_Entity Me = (EC_Entity)MeNullable;

            Vector2 position = Me.position;

            int tileX = Me.tileX;
            int tileY = Me.tileY;

            int gridX = tileX / General.boidGridSize;
            int gridY = tileY / General.boidGridSize;



            // <<Dynamic Leader Allocation>> //

            int leaderCount = CountLeaders(boidGrid, gridX, gridY);


            if (!leader && leaderCount < leaderDensityMin) // become leader
            {
                leaderGrid[gridX][gridY].Add(uid);
                this.leader = true;
                this.velocity = new Vector2();
                this.leaderCreationTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                this.leaderReference = null;
                ECSHandler.debugger.AddLog($"Stepping up...", WarningLevel.Debug);
            }
            else if (leader && leaderCount > leaderDensityMax && // become follower
                DateTimeOffset.Now.ToUnixTimeMilliseconds() - leaderCreationTime > minLeaderLifespan) // has lived for a required amount of time
            {
                ECSHandler.debugger.AddLog($"Stepping down...", WarningLevel.Debug);
                _ = leaderGrid[gridX][gridY].Remove(uid);
                this.leader = false;

                // To set the path to null, a copy needs to be made and then value changed, then put back
                EC_PathFinding? myPathFinding = (EC_PathFinding?)ECSHandler.ECSs[typeof(EC_PathFinding)][uid];
                if (myPathFinding is null) { ECSHandler.debugger.AddLog($"Error, entity {uid} has no pathing data, despite being a leader!", WarningLevel.Error); return; }

                EC_PathFinding myPathFindingNN = myPathFinding.Value; // not null
                myPathFindingNN.path = null;

                ECSHandler.ECSs[typeof(EC_PathFinding)][uid] = myPathFindingNN; 
            }



            // 2 options, leader or not.
            if (leader)
            {
                LeaderAction(boidGrid, General.boidGridSize, dt, uid, ref Me);
            }
            else
            {
                FollowerAction(boidGrid, General.boidGridSize, dt, uid, ref Me);
            }

            // <<Update Positions>> //
            

            // Grids
            int oldGridX = gridX;
            int oldGridY = gridY;
            gridX = (int)(position.X / General.boidGridSize);
            gridY = (int)(position.Y / General.boidGridSize);
            if (oldGridX != gridX || oldGridY != gridY)
            {
                _ = boidGrid[oldGridX][oldGridY].Remove(uid);
                boidGrid[gridX][gridY].Add(uid);

                if (leader)
                {
                    _ = leaderGrid[oldGridX][oldGridY].Remove(uid);
                    leaderGrid[gridX][gridY].Add(uid);
                }
            }

            tileX = (int)position.X; tileY = (int)position.Y;


            // Update EC
            ECSHandler.ECSs[typeof(EC_Entity)][uid] = Me;
        }




        // <<Misc Functions>> //

        // All grids nearby
        private static readonly (int, int)[] gridChecks = new (int, int)[] {
            (-1, -1), (0, -1), (1, -1),
            (-1,  0), (0,  0), (1,  0),
            (-1,  1), (0,  1), (1,  1) };
        private int CountLeaders(List<int>[][] boidGrid, int gridX, int gridY)
        {
            int leaderCount = 0;


            foreach ((int, int) gridRCoords in gridChecks)
            {
                // gridRCoords -> grid relative coordinates, just add them and check that grid
                int targetGridX = gridX + gridRCoords.Item1;
                int targetGridY = gridY + gridRCoords.Item2;

                // <<Bounds Checks>> //
                if (targetGridX < 0 || targetGridY < 0) { continue; }
                if (targetGridX >= boidGrid.Length || targetGridY >= boidGrid[0].Length) { continue; }

                leaderCount += leaderGrid[targetGridX][targetGridY].Count;
            }

            return leaderCount;
        }



        public void Cleanup(int uid)
        {
            EC_Entity? Me = (EC_Entity?)ECSHandler.ECSs[typeof(EC_Entity)][uid];
            if (Me is null) { ECSHandler.debugger.AddLog($"Error, entity {uid} has no entity data during cleanup!", WarningLevel.Error); return; }

            int gridX = Me.Value.tileX / General.boidGridSize;
            int gridY = Me.Value.tileY / General.boidGridSize;

            boidGrid[gridX][gridY].Remove(uid);
            if (leader) { leaderGrid[gridX][gridY].Remove(uid); }
        }
    }
}
