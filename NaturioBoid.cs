using System.Numerics;
using ShortTools.AStar;
using ShortTools.MagicContainer;


#pragma warning disable IDE0130 // Folder structure does not match, I dont want to change the name of the folder
#pragma warning disable IDE0090 // Use new(), that looks bad to me.
#pragma warning disable IDE0003 // Remove 'this.', I think it labels code more clearly. 



namespace BOIDSimulator
{
    internal class NaturioBoid : IBoid, ILeadable
    {
        // <<Class Settings>> //

        const float leaderSpeed = 20f; // blocks per second

        const float destroyZoneRadius = 10f; // how many blocks around the centre are the "kill zone", meaning the BOIDS will be deleted

        const int leaderDensityMin = 2; // If the leaders in a 3x3 area is less than this, then they will self promote,
        const int leaderDensityMax = 3; // however if over this value, it will stop being a leader



        const float leaderNodeMinDistance = 0.5f; // How close a leader needs to be to a node for it to register as visited in blocks



        // <<Public Class Variables>> //

        public Vector2 position;
        Vector2 IBoid.position { get => position; set => position = value; }


        public Vector2 velocity;
        Vector2 IBoid.velocity { get => velocity; set => velocity = value; }


        public bool leader = true; // default to false normally
        public bool Leader => leader;



        // <<Private Class Variables>> //

        // Boid Grid coordinates
        private int gridX = 0;
        private int gridY = 0;


        // Tile coordinates
        private int tileX = 0;
        private int tileY = 0;


        // Magic Container Indexes
        private int allBoidsIndex = -1;
        private int boidGridIndex = -1;



        // <<Modified Constants>> //
        const float leaderNodeMinDistanceSquared = leaderNodeMinDistance * leaderNodeMinDistance; // Precompute this to speed up itterations


        // <<Constructors>> //

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






        public void Action(SMContainer<IBoid>[][] boidGrid, int gridSize, float dt)
        {
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
            
            bool success = boidGrid[gridX][gridY].Remove(this);
            if (!success)
            {
                Console.WriteLine("Could not remove, ruh roh");
            }
            gridX = (int)(position.X / General.boidGridSize);
            gridY = (int)(position.Y / General.boidGridSize);
            boidGridIndex = boidGrid[gridX][gridY].Add(this);

            tileX = (int)position.X; tileY = (int)position.Y;


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
            }
            else if (leader && leaderCount > leaderDensityMax)
            {
                //Console.WriteLine($"Becoming Follower (count {leaderCount}) At coords {position} Grid ({gridX}x{gridY}) " +
                //    $"should be ({(int)(position.X / General.boidGridSize)}x{(int)(position.Y / General.boidGridSize)}) time {DateTimeOffset.Now.ToUnixTimeMilliseconds()}");
                this.leader = false;
            }
        }





        // <<Leader Action Variables>> //
        [ThreadStatic]
        private static PathFinder pather = new PathFinder(Walkable, maxDist: 1000, useDiagonals: true);
        Queue<Vector2>? path = null;

        static readonly int targetX = General.map.Length / 2; 
        static readonly int targetY = General.map[0].Length / 2; // Centre of the map
        private void LeaderAction(SMContainer<IBoid>[][] boidGrid, int gridSize, float dt)
        {
            // Simply path to centre
            if (path is null || path.Count == 0)
            {
                // create new path if none there
                path = pather.GetPath(tileX, tileY, targetX, targetY);
                if (path is null) { DestroySelf(boidGrid); return; } // no path could be found, so it should not be there.
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





        private void FollowerAction(SMContainer<IBoid>[][] boidGrid, int gridSize, float dt)
        {
            // Check for charge
            // If not, follow leader boid
            // And avoid walls
        }








        // <<Misc Functions>> //

        // All grids nearby
        private static readonly (int, int)[] gridChecks = new (int, int)[] { 
            (-1, -1), (0, -1), (1, -1), 
            (-1,  0), (0,  0), (1,  0), 
            (-1,  1), (0,  1), (1,  1) };
        private int CountLeaders(SMContainer<IBoid>[][] boidGrid)
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
                        if ((int)(boid.position.X / General.boidGridSize) != targetGridX || (int)(boid.position.Y / General.boidGridSize) != targetGridY)
                        {
                            Console.WriteLine($"Issue, boid is at incorrect position. Grid ({targetGridX}x{targetGridY}) " +
                    $"should be ({(int)(boid.position.X / General.boidGridSize)}x{(int)(boid.position.Y / General.boidGridSize)})");
                        }
                        leaderCount++;
                    }
                }
            }

            return leaderCount;
        }

        private void DestroySelf(SMContainer<IBoid>[][] boidGrid)
        {
            _ = General.allBoids.RemoveAt(allBoidsIndex);
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
