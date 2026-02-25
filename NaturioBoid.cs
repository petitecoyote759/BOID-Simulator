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

        const float leaderSpeed = 20f; // blocks per second
        const float followerSpeed = 1.1f * leaderSpeed;
        const float followerAcceleration = 30f;

        const float destroyZoneRadius = 10f; // how many blocks around the centre are the "kill zone", meaning the BOIDS will be deleted

        const int leaderDensityMin = 1; // If the leaders in a 3x3 area is less than this, then they will self promote,
        const int leaderDensityMax = 2; // however if over this value, it will stop being a leader

        const int pathCacheMax = 5;


        const float leaderNodeMinDistance = 0.5f; // How close a leader needs to be to a node for it to register as visited in blocks

        const float followerChargeRange = 50f;



        const float startPathDistanceRatio = 0.4f; // what portion of a boidGrid the leaders would be willing to walk to find a preset path

        const float minLeaderLifespanSeconds = 3; // minimum number of seconds a leader should live for
        const float minLeaderFollowSeconds = 3;


        const float alignmentConst = 0.2f;
        const int blockCheckRange = 4;
        const float seperationConst = 1f;



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


        // Cached Paths
        private static CacheCell[][] cachedPaths = Array.Empty<CacheCell[]>();

        
        // Cached counts
        private static List<NaturioBoid>[][] leaderGrid = Array.Empty<List<NaturioBoid>[]>();


        // Leader Timegate
        private long leaderCreationTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        private long leaderFollowStartTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();


        // Current Leader
        private NaturioBoid? currentLeader = null;


        // <<Modified Constants>> //
        const float leaderNodeMinDistanceSquared = leaderNodeMinDistance * leaderNodeMinDistance; // Precompute this to speed up itterations

        const float followerSpeedSquared = followerSpeed * followerSpeed;

        const int startPathDistance = (int)(startPathDistanceRatio * General.boidGridSize);

        const long minLeaderLifespan = (int)(1000 * minLeaderLifespanSeconds);
        const long minLeaderFollowDuration = (int)(1000 * minLeaderFollowSeconds);


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
            // <<Destroy at Centre>> //

            if (MathF.Abs(targetX - tileX) + MathF.Abs(targetY - tileY) < destroyZoneRadius)
            {
                DestroySelf(boidGrid);
            }



            // <<Dynamic Leader Allocation>> //

            int leaderCount = CountLeaders(boidGrid);


            if (!leader && leaderCount < leaderDensityMin) // become leader
            {
                leaderGrid[gridX][gridY].Add(this);
                this.leader = true;
                this.velocity = new Vector2();
                this.leaderCreationTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                this.currentLeader = null;
            }
            else if (leader && leaderCount > leaderDensityMax && // become follower
                DateTimeOffset.Now.ToUnixTimeMilliseconds() - leaderCreationTime > minLeaderLifespan) // has lived for a required amount of time
            {
                _ = leaderGrid[gridX][gridY].Remove(this);
                this.leader = false;
                path = null;
            }



            // 2 options, leader or not.
            if (leader)
            {
                LeaderAction(boidGrid, gridSize, dt);
            }
            else
            {
                FollowerAction(boidGrid, gridSize, dt);
            }

            // <<Update Positions>> //
            // Bounds checks
            position.X = float.Clamp(position.X, 0, General.map.Length);
            position.Y = float.Clamp(position.Y, 0, General.map[0].Length);

            // Grids
            int oldGridX = gridX;
            int oldGridY = gridY;
            gridX = (int)(position.X / General.boidGridSize);
            gridY = (int)(position.Y / General.boidGridSize);
            if (oldGridX != gridX || oldGridY != gridY)
            {
                _ = boidGrid[oldGridX][oldGridY].Remove(this);
                boidGrid[gridX][gridY].Add(this);

                if (leader)
                {
                    _ = leaderGrid[oldGridX][oldGridY].Remove(this);
                    leaderGrid[gridX][gridY].Add(this);
                }
            }

            tileX = (int)position.X; tileY = (int)position.Y;
        }





        // <<Leader Action Variables>> //
        private static PathFinder pather;
        private static PathFinder intraGridPather; // used to path inside of a cell
        Path? path = null;

        static readonly int targetX = General.map.Length / 2; 
        static readonly int targetY = General.map[0].Length / 2; // Centre of the map
        private void LeaderAction(List<IBoid>[][] boidGrid, int gridSize, float dt)
        {
            // Simply path to centre
            if (path is null || path.Count == 0)
            {
                // <<Get Path From Cache>> //
                CacheCell currentCache = cachedPaths[gridX][gridY];
                
                for (int i = 0; i < currentCache.Count; i++)
                {
                    Path cachedPath = new Path(currentCache[i]); // makes a deep copy

                    Vector2 pathStart = cachedPath.Peek();
                    int pathStartX = (int)pathStart.X;
                    int pathStartY = (int)pathStart.Y;

                    Path? toStartPath = intraGridPather.GetPath(tileX, tileY, pathStartX, pathStartY);

                    if (toStartPath is null) { continue; }
                    if (!PathIsValid(cachedPath)) { currentCache.RemoveAt(i); i--; continue; }

                    path = toStartPath;
                    int length = cachedPath.Count;
                    for (int j = 0; j < length; j++)
                    {
                        path.Enqueue(cachedPath.Dequeue());
                    }
                    break;
                }
                // <<Generate New Path>> //
                if (path is null || path.Count == 0)
                {
                    // create new path if none there
                    path = pather.GetPath(tileX, tileY, targetX, targetY);
                    if (path is null || path.Count == 0) { DestroySelf(boidGrid); return; } // no path could be found, so it should not be there.
                    // path is not null
                    if (currentCache.Count < pathCacheMax)
                    {
                        cachedPaths[gridX][gridY].Add(new Path(path));
                    }
                }
            }

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
        private static bool PathIsValid(Path path)
        {
            Path testPath = new Path(path);
            while (testPath.Count != 0)
            {
                Vector2 node = testPath.Dequeue();
                int x = (int)node.X;
                int y = (int)node.Y;
                if (0 > x || x >= General.map.Length) { return false; }
                if (0 > y || y >= General.map[0].Length) { return false; }
                if (General.Walkable(General.map[x][y]) == false) { return false; }
            }
            return true;
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
            if (currentLeader is null || DateTimeOffset.Now.ToUnixTimeMilliseconds() - leaderFollowStartTime > minLeaderFollowDuration)
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








        // <<Misc Functions>> //

        // All grids nearby
        private static readonly (int, int)[] gridChecks = new (int, int)[] { 
            (-1, -1), (0, -1), (1, -1), 
            (-1,  0), (0,  0), (1,  0), 
            (-1,  1), (0,  1), (1,  1) };
        private int CountLeaders(List<IBoid>[][] boidGrid)
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


        private void DestroySelf(List<IBoid>[][] boidGrid)
        {
            if (leader)
            {
                leaderGrid[gridX][gridY].Remove(this);
            }
            _ = General.allBoids.Remove(this);
            _ = boidGrid[gridX][gridY].Remove(this);
        }
        private static bool Walkable(int x, int y)
        {
            // <<Bounds Checks>> //
            if (x < 0 || y < 0) { return false; }
            if (x >= General.map.Length) { return false; }
            if (y >= General.map[0].Length) { return false; }

            return General.Walkable(General.map[x][y]);
        }
    }
}
