using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using ShortTools.General;
using System.Threading.Tasks;
using Path = System.Collections.Generic.Queue<System.Numerics.Vector2>;

namespace BOIDSimulator.ECS_Components
{
    internal partial struct EC_BoidLogic
    {
        private void FollowerAction(List<int>[][] boidGrid, int gridSize, float dt, ref EC_Entity Me)
        {
            // Check for charge
            // If not, follow leader boid
            // And avoid walls
            Vector2 position = Me.position;

            int tileX = Me.tileX;
            int tileY = Me.tileY;

            int gridX = tileX / General.boidGridSize;
            int gridY = tileY / General.boidGridSize;

            if (MathF.Abs(targetX - tileX) + MathF.Abs(targetY - tileY) < followerChargeRange) // Charging
            {
                Vector2 step = Vector2.Normalize(new Vector2(targetX, targetY) - position) * followerSpeed * dt; // the distance to step.
                position += step;
            }
            else
            {
                FollowerFollow(boidGrid, gridSize, dt, gridX, gridY, ref Me);
            }

        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FollowerFollow(List<int>[][] boidGrid, int gridSize, float dt, int gridX, int gridY, ref EC_Entity Me)
        {
            // <<Get Leader>> //
            

            if (targetUid is null || IsLeader(targetUid.Value) == false || DateTimeOffset.Now.ToUnixTimeMilliseconds() - leaderFollowStartTime > minLeaderFollowDuration)
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

                    foreach (int uid in leaderGrid[targetGridX][targetGridY])
                    {
                        if (IsLeader(uid) == false) { continue; }

                        EC_Entity? currentEntityInfo = (EC_Entity?)ECSHandler.ECSs[typeof(EC_Entity)][uid];
                        if (currentEntityInfo is null) { General.debugger.AddLog($"Entity {uid} had no entity info!", WarningLevel.Error); continue; }
                        float distance = MathF.Abs(Me.position.X - currentEntityInfo.Value.position.X) + MathF.Abs(Me.position.Y - currentEntityInfo.Value.position.Y);
                        leaderQueue.Enqueue(uid, distance);
                    }
                }

                // closest boid, should always be one due to DLA
                if (leaderQueue.Count == 0) { return; }
                targetUid = leaderQueue.Dequeue();
            }

            if (targetUid is null) { return; } // will never happen, just to satisfy compiler



#pragma warning disable CS8629 // Cant be null, it was checked previously
            EC_Entity targetEInfo = ((EC_Entity?)ECSHandler.ECSs[typeof(EC_Entity)][targetUid.Value]).Value;
            EC_BoidLogic targetELogic = ((EC_BoidLogic?)ECSHandler.ECSs[typeof(EC_BoidLogic)][targetUid.Value]).Value;
#pragma warning restore CS8629

            // <<Boid with Leader>> //
            Vector2 direction = Vector2.Normalize(targetEInfo.position - Me.position);
            if (float.IsNaN(direction.X)) { return; } // they are on the same point



            velocity += direction * followerAcceleration * dt;

            // Alignment
            velocity = (velocity) + (alignmentConst * dt * targetELogic.velocity);

            // <<Seperate from edges>> //
            for (int x = -blockCheckRange / 2; x < blockCheckRange / 2; x++)
            {
                for (int y = -blockCheckRange / 2; y < blockCheckRange / 2; y++)
                {
                    Vector2 target = new Vector2(Me.position.X + x, Me.position.Y + y);

                    if (target.X < 0 || target.X > (Map.tileMap.Length) - 1) { continue; }
                    if (target.Y < 0 || target.Y > (Map.tileMap[0].Length) - 1) { continue; }

                    if (General.Walkable(Map.tileMap[(int)(target.X)][(int)(target.Y)]) == false)
                    {
                        Vector2 push = Me.position - target;
                        if (push.LengthSquared() > 0.1f)
                        {
                            Vector2 normalVelocity = Vector2.Normalize(velocity);
                            Vector2 normalPush = Vector2.Normalize(push);

                            //velocity = normalPush + (Vector2.Dot(normalPush, normalVelocity) * normalVelocity);

                            velocity += normalPush * seperationConst * followerAcceleration * dt;
                        }
                    }
                }
            }



            if (velocity.LengthSquared() > followerSpeedSquared)
            {
                velocity = followerSpeed * Vector2.Normalize(velocity);
            }

            Me.position += velocity * dt;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsLeader(int uid)
        {
            return ((EC_BoidLogic?)ECSHandler.ECSs[typeof(EC_BoidLogic)][uid])?.leader == true;
        }

    }
}
