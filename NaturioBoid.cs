
using ShortTools.AStar;
using ShortTools.MagicContainer;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Path = System.Collections.Generic.Queue<System.Numerics.Vector2>; // Queue<Vector2>
using CacheCell = System.Collections.Generic.List<System.Collections.Generic.Queue<System.Numerics.Vector2>>;
using System.Reflection.Metadata;
using System.Diagnostics.CodeAnalysis; // List<Path>


#pragma warning disable IDE0130 // Folder structure does not match, I dont want to change the name of the folder
#pragma warning disable IDE0090 // Use new(), that looks bad to me.
#pragma warning disable IDE0003 // Remove 'this.', I think it labels code more clearly. 



namespace BOIDSimulator
{
    internal class NaturioBoid : IBoid, ILeadable
    {
        // <<Class Settings>> //

        

        

        



        

        


        



        // <<Public Class Variables>> //

        public Vector2 position;
        Vector2 IBoid.position { get => position; set => position = value; }


        public Vector2 velocity;
        Vector2 IBoid.velocity { get => velocity; set => velocity = value; }


        public bool leader = false; // default to false normally
        public bool Leader => leader;



        // <<Private Class Variables>> //

        // Boid Grid Coordinates
        public int gridX = 0;
        public int gridY = 0;


        // Tile Coordinates
        private int tileX = 0;
        private int tileY = 0;


        // Magic Container Indexes
        private int allBoidsIndex = -1;
        private int boidGridIndex = -1;


        

        
        


        
        


        // Current Leader
        private NaturioBoid? currentLeader = null;


        // <<Modified Constants>> //
        


        // <<Constructors>> //

        public static void SetupBoids(List<IBoid>[][] boidGrid)
        {
            cachedPaths = new CacheCell[boidGrid.Length][];
            leaderGrid = new List<NaturioBoid>[boidGrid.Length][];
            for (int x = 0; x < cachedPaths.Length; x++)
            {
                cachedPaths[x] = new CacheCell[boidGrid[0].Length];
                leaderGrid[x] = new List<NaturioBoid>[boidGrid[0].Length];
                for (int y = 0; y < cachedPaths[0].Length; y++)
                {
                    cachedPaths[x][y] = new CacheCell();
                    leaderGrid[x][y] = new List<NaturioBoid>();
                }
            }
            if (pather is null)
            {
                pather = new PathFinder(Walkable, maxDist: 1000, useDiagonals: true);
                intraGridPather = new PathFinder(Walkable, maxDist: startPathDistance, useDiagonals: true);
            }
        }

        public NaturioBoid(float x, float y)
        {
            position = new Vector2(x, y);

            gridX = (int)(x / General.boidGridSize);
            gridY = (int)(y / General.boidGridSize);

            tileX = (int)x; tileY = (int)y;

        }

        // Not currently used, useful for constant time deletion with the Magic Container
        public void SetIndexes(int? allBoidsIndex = null, int? boidGridIndex = null)
        {
            this.allBoidsIndex = allBoidsIndex ?? this.allBoidsIndex;
            this.boidGridIndex = boidGridIndex ?? this.boidGridIndex;
        }






        public void Action(List<IBoid>[][] boidGrid, int gridSize, float dt)
        {


            
        }





        // <<Leader Action Variables>> //
        Path? path = null;

        static readonly int targetX = General.map.Length / 2; 
        static readonly int targetY = General.map[0].Length / 2; // Centre of the map
        private void LeaderAction(List<IBoid>[][] boidGrid, int gridSize, float dt)
        {
            

            // Path is there and has items
            Vector2 node = path.Peek();
            if ((position - node).LengthSquared() < leaderNodeMinDistanceSquared)
            {
                _ = path.Dequeue(); // remove the node, and with that done we can return.
                return;
            }
            // Now we know we are not close enough to it
            Vector2 step = Vector2.Normalize(node - position) * leaderSpeed * dt; // the distance to step.
            
            if (step.LengthSquared() > (position - node).LengthSquared()) // if stepping too far, just go to the node
            {
                position = node;
            }
            else
            { // else take that step.
                position += step;
            }
        }
        











        private void FollowerAction(List<IBoid>[][] boidGrid, int gridSize, float dt)
        {
            // Check for charge
            // If not, follow leader boid
            // And avoid walls

            if (MathF.Abs(targetX - tileX) + MathF.Abs(targetY - tileY) < followerChargeRange) // Charging
            {
                Vector2 step = Vector2.Normalize(new Vector2(targetX, targetY) - position) * followerSpeed * dt; // the distance to step.
                position += step;
            }
            else
            {
                FollowerFollow(boidGrid, gridSize, dt);
            }
            
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FollowerFollow(List<IBoid>[][] boidGrid, int gridSize, float dt)
        {
            // <<Get Leader>> //
            if (currentLeader is null || currentLeader.leader == false || DateTimeOffset.Now.ToUnixTimeMilliseconds() - leaderFollowStartTime > minLeaderFollowDuration)
            {
                PriorityQueue<IBoid, float> leaderQueue = new PriorityQueue<IBoid, float>();

                foreach ((int, int) gridRCoords in gridChecks)
                {
                    // gridRCoords -> grid relative coordinates, just add them and check that grid
                    int targetGridX = gridX + gridRCoords.Item1;
                    int targetGridY = gridY + gridRCoords.Item2;

                    // <<Bounds Checks>> //
                    if (targetGridX < 0 || targetGridY < 0) { continue; }
                    if (targetGridX >= boidGrid.Length || targetGridY >= boidGrid[0].Length) { continue; }

                    foreach (NaturioBoid boid in leaderGrid[targetGridX][targetGridY])
                    {
                        if (boid.leader == false) { continue; }

                        float distance = MathF.Abs(position.X - boid.position.X) + MathF.Abs(position.Y - boid.position.Y);
                        leaderQueue.Enqueue(boid, distance);
                    }
                }

                // closest boid, should always be one due to DLA
                if (leaderQueue.Count == 0) { return; }
                currentLeader = leaderQueue.Dequeue() as NaturioBoid;
            }

            if (currentLeader is null) { return; } // will never happen, just to satisfy compiler



            // <<Boid with Leader>> //
            Vector2 direction = Vector2.Normalize(currentLeader.position - position);
            if (float.IsNaN(direction.X)) { return; } // they are on the same point


            
            velocity += direction * followerAcceleration * dt;
            
            // Alignment
            velocity = (velocity) + (alignmentConst * dt * currentLeader.velocity);

            // <<Seperate from edges>> //
            for (int x = -blockCheckRange / 2; x < blockCheckRange / 2; x++)
            {
                for (int y = -blockCheckRange / 2; y < blockCheckRange / 2; y++)
                {
                    Vector2 target = new Vector2(position.X + x, position.Y + y);

                    if (target.X < 0 || target.X > (General.map.Length) - 1) { continue; }
                    if (target.Y < 0 || target.Y > (General.map[0].Length) - 1) { continue; }

                    if (General.Walkable(General.map[(int)(target.X)][(int)(target.Y)]) == false)
                    {
                        Vector2 push = position - target;
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

            position += velocity * dt;
        }








        


        private void DestroySelf(List<IBoid>[][] boidGrid)
        {
            if (leader)
            {
                leaderGrid[gridX][gridY].Remove(this);
            }
            _ = General.allBoids.Remove(this);
            _ = boidGrid[gridX][gridY].Remove(this);
            leader = false;
        }
        
    }
}