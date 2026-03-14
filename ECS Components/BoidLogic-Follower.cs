using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using ShortTools.General;
using System.Threading.Tasks;
using Path = System.Collections.Generic.Queue<System.Numerics.Vector2>;
using System.Security.Cryptography;

namespace BOIDSimulator.ECS_Components
{
    internal partial struct EC_BoidLogic
    {
        private void FollowerAction(List<int>[][] boidGrid, int gridSize, float dt, int uid, ref EC_Entity Me)
        {
            // Check for charge
            // If not, follow leader boid
            // And avoid walls
            Vector2 position = Me.position;

            int tileX = Me.tileX;
            int tileY = Me.tileY;

            int gridX = tileX / General.boidGridSize;
            int gridY = tileY / General.boidGridSize;

            if (MathF.Abs(targetX - tileX) + MathF.Abs(targetY - tileY) < followerChargeRange + destroyZoneRadius) // Charging
            {
                Vector2 step = Vector2.Normalize(new Vector2(targetX, targetY) - position) * followerSpeed * dt; // the distance to step.
                Me.position += step;
            }
            else
            {
                FollowerFollow(boidGrid, gridSize, dt, gridX, gridY, uid, ref Me);
            }

        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FollowerFollow(List<int>[][] boidGrid, int gridSize, float dt, int gridX, int gridY, int uid, ref EC_Entity Me)
        {
            // <<Get Leader>> //
            

            if (
                leaderReference is null || 
                leaderReference.closed ||
                IsLeader(leaderReference.targetUid) == false || 
                ECSHandler.LFT - leaderFollowStartTime > minLeaderFollowDuration)
            {
                GetNewLeader(gridX, gridY, ref Me, uid);
            }

            if (leaderReference is null) { ECSHandler.debugger.AddLog($"Follower targetUID was still null", WarningLevel.Error); return; } // will never happen, just to satisfy compiler



            // Cant be null, it was checked previously
            _ = ECSHandler.GetEntityComponent(leaderReference.targetUid, out EC_Entity targetEInfo);
            _ = ECSHandler.GetEntityComponent(leaderReference.targetUid, out EC_BoidLogic targetBLogic);


            // <<Boid with Leader>> //
            Vector2 direction = Vector2.Normalize(targetEInfo.position - Me.position);
            if (float.IsNaN(direction.X)) { return; } // they are on the same point



            velocity += direction * followerAcceleration * dt;

            // Alignment
            velocity = (velocity) + (alignmentConst * dt * targetBLogic.velocity);

            // <<Seperate from edges>> //
            for (int x = -blockCheckRange / 2; x < blockCheckRange / 2; x++)
            {
                for (int y = -blockCheckRange / 2; y < blockCheckRange / 2; y++)
                {
                    Vector2 target = new Vector2(Me.position.X + x, Me.position.Y + y);

                    if (target.X < 0 || target.X > (Map.tileMap.Length) - 1) { continue; }
                    if (target.Y < 0 || target.Y > (Map.tileMap[0].Length) - 1) { continue; }

                    if (General.Walkable(Map.tileMap[(int)(target.X)][(int)(target.Y)])) { continue; }
                    Vector2 push = Me.position - target;
                    if (push.LengthSquared() > 0.1f)
                    {
                        Vector2 normalVelocity = Vector2.Normalize(velocity);
                        Vector2 normalPush = Vector2.Normalize(push);

                        velocity += normalPush * seperationConst * followerAcceleration * dt;
                    }
                }
            }



            if (velocity.LengthSquared() > followerSpeedSquared)
            {
                velocity = followerSpeed * Vector2.Normalize(velocity);
            }

            // Vary movement to try and have the boid move side to side.
            float variation = angleVariation * ((2 * random.NextSingle()) - 1f); // between -1, and 1
            velocity = new Vector2(
                velocity.X - (velocity.Y * variation),
                (velocity.X * variation) + velocity.Y); // simplified version of transformation matrix using small angle (cos(a) = 1)

            Me.position += velocity * dt;
        }




        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetNewLeader(int gridX, int gridY, ref EC_Entity Me, int uid)
        {
            PriorityQueue<int, float> leaderQueue = new PriorityQueue<int, float>();

            foreach ((int, int) gridRCoords in gridChecks)
            {
                // gridRCoords -> grid relative coordinates, just add them and check that grid
                int targetGridX = gridX + gridRCoords.Item1;
                int targetGridY = gridY + gridRCoords.Item2;

                // <<Bounds Checks>> //
                if (targetGridX < 0 || targetGridY < 0) { continue; }
                if (targetGridX >= boidGrid.Length || targetGridY >= boidGrid[0].Length) { continue; }

                foreach (int leaderUid in leaderGrid[targetGridX][targetGridY])
                {
                    if (IsLeader(leaderUid) == false) { continue; }

                    bool success = ECSHandler.GetEntityComponent(leaderUid, out EC_Entity currentEntityInfo);
                    success &= ECSHandler.GetEntityComponent(leaderUid, out EC_PathFinding pathfindingInfo);
                    if (success == false) { ECSHandler.debugger.AddLog($"Entity {leaderUid} had no entity or pathfinding info!", WarningLevel.Error); continue; }
                    float distance = MathF.Abs(Me.position.X - currentEntityInfo.position.X) + MathF.Abs(Me.position.Y - currentEntityInfo.position.Y);
                    leaderQueue.Enqueue(leaderUid, distance * (pathfindingInfo.path?.Count ?? 1));
                }
            }

            // closest boid, should always be one due to DLA
            if (leaderQueue.Count == 0) { ECSHandler.debugger.AddLog($"Could not find a leader in the area?", WarningLevel.Warning); return; }
            leaderReference = new EntityReference(leaderQueue.Dequeue(), uid);
            leaderFollowStartTime = ECSHandler.LFT;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsLeader(int uid)
        {
            return ((EC_BoidLogic?)ECSHandler.ECSs[typeof(EC_BoidLogic)][uid])?.leader == true;
        }

    }
}
