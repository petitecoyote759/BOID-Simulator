using ShortTools.AStar;
using ShortTools.MagicContainer;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Path = System.Collections.Generic.Queue<System.Numerics.Vector2>; // Queue<Vector2>
using CacheCell = System.Collections.Generic.List<System.Collections.Generic.Queue<System.Numerics.Vector2>>; // List<Path>



#pragma warning disable IDE0130 // Folder structure does not match, I dont want to change the name of the folder
#pragma warning disable IDE0090 // Use new(), that looks bad to me.
#pragma warning disable IDE0003 // Remove 'this.', I think it labels code more clearly. 



namespace BOIDSimulator
{
    internal class NaturioBoid : IBoid, ILeadable
    {
        // <<Class Settings>> //

        const float leaderSpeed = 20f; // blocks per second
        const float followerSpeed = 1.3f * leaderSpeed;
        const float followerAcceleration = 20f;

        const float destroyZoneRadius = 10f; // how many blocks around the centre are the "kill zone", meaning the BOIDS will be deleted

        const int leaderDensityMin = 1; // If the leaders in a 3x3 area is less than this, then they will self promote,
        const int leaderDensityMax = 2; // however if over this value, it will stop being a leader

        const int pathCacheMax = 5;


        const float leaderNodeMinDistance = 0.5f; // How close a leader needs to be to a node for it to register as visited in blocks

        const float followerChargeRange = 50f;



        // <<Public Class Variables>> //

        public Vector2 position;
        Vector2 IBoid.position { get => position; set => position = value; }


        public Vector2 velocity;
        Vector2 IBoid.velocity { get => velocity; set => velocity = value; }


        public bool leader = true; // default to false normally
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


        // <<Modified Constants>> //
        const float leaderNodeMinDistanceSquared = leaderNodeMinDistance * leaderNodeMinDistance; // Precompute this to speed up itterations
        const float followerSpeedSquared = followerSpeed * followerSpeed;
        const float followerChargeRangeSquared = followerChargeRange * followerChargeRange;


        // <<Constructors>> //

        public static void SetupBoids(List<IBoid>[][] boidGrid)
        {
            cachedPaths = new CacheCell[boidGrid.Length][];
            for (int x = 0; x < cachedPaths.Length; x++)
            {
                cachedPaths[x] = new CacheCell[boidGrid[0].Length];
                for (int y = 0; y < cachedPaths[0].Length; y++)
                {
                    cachedPaths[x][y] = new CacheCell();
                }
            }
            if (pather is null)
            {
                pather = new PathFinder(Walkable, maxDist: 1000, useDiagonals: true);
                intraGridPather = new PathFinder(Walkable, maxDist: General.boidGridSize, useDiagonals: true);
            }
        }

        public NaturioBoid(float x, float y)
        {
            position = new Vector2(x, y);

            gridX = (int)(x / General.boidGridSize);
            gridY = (int)(y / General.boidGridSize);

            tileX = (int)x; tileY = (int)y;
        }

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


            if (!leader && leaderCount < leaderDensityMin)
            {
                //Console.WriteLine($"Becoming Leader (count {leaderCount}) At coords {position} Grid ({gridX}x{gridY}) " +
                //    $"should be ({(int)(position.X / General.boidGridSize)}x{(int)(position.Y / General.boidGridSize)}) time {DateTimeOffset.Now.ToUnixTimeMilliseconds()}");
                this.leader = true;
                this.velocity = new Vector2();
            }
            else if (leader && leaderCount > leaderDensityMax)
            {
                //Console.WriteLine($"Becoming Follower (count {leaderCount}) At coords {position} Grid ({gridX}x{gridY}) " +
                //    $"should be ({(int)(position.X / General.boidGridSize)}x{(int)(position.Y / General.boidGridSize)}) time {DateTimeOffset.Now.ToUnixTimeMilliseconds()}");
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
                if (General.Walkable(General.map[x][y]) == false) { return false; }
            }
            return true;
        }











        private void FollowerAction(List<IBoid>[][] boidGrid, int gridSize, float dt)
        {
            // Check for charge
            // If not, follow leader boid
            // And avoid walls

            if (MathF.Abs(targetX - tileX) + MathF.Abs(targetY - tileY) < followerChargeRange)
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
            PriorityQueue<IBoid, float> leaderQueue = new PriorityQueue<IBoid, float>();

            foreach ((int, int) gridRCoords in gridChecks)
            {
                // gridRCoords -> grid relative coordinates, just add them and check that grid
                int targetGridX = gridX + gridRCoords.Item1;
                int targetGridY = gridY + gridRCoords.Item2;

                // <<Bounds Checks>> //
                if (targetGridX < 0 || targetGridY < 0) { continue; }
                if (targetGridX >= boidGrid.Length || targetGridY >= boidGrid[0].Length) { continue; }

                foreach (IBoid boid in boidGrid[targetGridX][targetGridY])
                {
                    if (boid is not NaturioBoid nBoid) { continue; }
                    if (nBoid.leader == false) { continue; }

                    // only leaders
                    float distance = MathF.Abs(position.X - boid.position.X) + MathF.Abs(position.Y - boid.position.Y);
                    leaderQueue.Enqueue(boid, distance);
                }
            }

            // closest boid, should always be one due to DLA
            if (leaderQueue.Count == 0) { return; }
            IBoid leaderBoid = leaderQueue.Dequeue();




            // <<Boid with Leader>> //
            Vector2 direction = Vector2.Normalize(leaderBoid.position - position);
            if (float.IsNaN(direction.X)) { return; } // they are on the same point

            velocity += direction * followerAcceleration * dt;
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

                foreach (IBoid boid in boidGrid[targetGridX][targetGridY])
                {
                    if (boid == this) { continue; }
                    if (boid is ILeadable leadableBoid && leadableBoid.Leader == true)
                    {
                        leaderCount++;
                    }
                }
            }

            return leaderCount;
        }

        private void DestroySelf(List<IBoid>[][] boidGrid)
        {
            _ = General.allBoids.Remove(this);
            bool success = boidGrid[gridX][gridY].Remove(this);
            if (!success)
            {
                Console.WriteLine($"Tried to destroy self at {gridX}x{gridY} but failed");
                for (int x = 0; x < boidGrid.Length; x++)
                {
                    for (int y = 0; y < boidGrid[0].Length; y++)
                    {
                        if (x == gridX && y == gridY) { continue; }
                        if (boidGrid[x][y].Contains(this))
                        {
                            Console.WriteLine($"Found at {x}x{y}");
                        }
                    }
                }
            }
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
