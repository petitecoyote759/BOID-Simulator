using ShortTools.AStar;
using ShortTools.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using CacheCell = System.Collections.Generic.List<System.Collections.Generic.Queue<System.Numerics.Vector2>>;
using Path = System.Collections.Generic.Queue<System.Numerics.Vector2>; // Queue<Vector2>



namespace BOIDSimulator.ECS_Components
{
    internal struct EC_Pathfinding : IEntityComponent
    {
        // <<Requires>> //
        // EC_Entity //

        // <<Public Variables>> //
        [ThreadStatic]
        public static PathFinder? pather;
        [ThreadStatic]
        private static PathFinder? intraGridPather;
        public Path? path;

        public bool Active { get => active; set => active = value; }
        private bool active = true;


        public int targetX = 0;
        public int targetY = 0;




        // <<Private Variables>> //

        // Cached Paths
        private static CacheCell[][] cachedPaths = Array.Empty<CacheCell[]>();


        // <<Constants>> //
        const float startPathDistanceRatio = 0.4f; // what portion of a boidGrid the leaders would be willing to walk to find a preset path
        const int pathCacheMax = 5;


        // <<Modified Constants> //
        const int startPathDistance = (int)(startPathDistanceRatio * General.boidGridSize);





        public EC_Pathfinding(Func<int, int, bool> Walkable)
        {
            if (Map.tileMap is null) { General.debugger.AddLog("Tilemap was null during pathfinder initialisation.", WarningLevel.Error); return; }

            cachedPaths = new CacheCell[Map.tileMap.Length / General.boidGridSize][];
            for (int x = 0; x < cachedPaths.Length; x++)
            {
                cachedPaths[x] = new CacheCell[Map.tileMap[0].Length / General.boidGridSize];
                for (int y = 0; y < cachedPaths[0].Length; y++)
                {
                    cachedPaths[x][y] = new CacheCell();
                }
            }
            if (pather is null)
            {
                pather = new PathFinder(Walkable, maxDist: 1000, useDiagonals: true);
                intraGridPather = new PathFinder(Walkable, maxDist: startPathDistance, useDiagonals: true);
            }

            path = null;
        }



        public void Action(float dt, int uid)
        {
            EC_Entity? Me = (EC_Entity?)ECSHandler.ECSs[typeof(EC_Entity)][uid];
            if (Me is null) { General.debugger.AddLog($"Error, entity {uid} has no entity data!", WarningLevel.Error); return; }

            int tileX = Me.Value.tileX;
            int tileY = Me.Value.tileY;

            int gridX = tileX / General.boidGridSize;
            int gridY = tileY / General.boidGridSize;



            // Pathfind, and set path if required
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

                    Path? toStartPath = intraGridPather?.GetPath(tileX, tileY, pathStartX, pathStartY);

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
                    path = pather?.GetPath(tileX, tileY, targetX, targetY);
                    if (path is null || path.Count == 0) { ECSHandler.FreeUID(uid); return; } // no path could be found, so it should not be there.
                    // path is not null
                    if (currentCache.Count < pathCacheMax)
                    {
                        cachedPaths[gridX][gridY].Add(new Path(path));
                    }
                }
            }
        }


        private static bool PathIsValid(Path path)
        {
            if (Map.tileMap is null) { return false; }

            Path testPath = new Path(path);
            while (testPath.Count != 0)
            {
                Vector2 node = testPath.Dequeue();
                int x = (int)node.X;
                int y = (int)node.Y;
                if (0 > x || x >= Map.tileMap.Length) { return false; }
                if (0 > y || y >= Map.tileMap.Length) { return false; }
                if (General.Walkable(Map.tileMap[x][y]) == false) { return false; }
            }
            return true;
        }
    }
}
