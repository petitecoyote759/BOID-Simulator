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
    internal struct EC_PathFinding : IEntityComponent
    {
        // <<Requires>> //
        // EC_Entity //

        // <<Public Variables>> //
        public static PathFinder? pather;
        private static PathFinder? intraGridPather;
        public Path? path;

        public bool Active { get => active; set => active = value; }
        private bool active = true;


        public int targetX = 0;
        public int targetY = 0;




        // <<Private Variables>> //

        // Cached Paths
        internal static CacheCell[][] cachedPaths = Array.Empty<CacheCell[]>();


        // <<Constants>> //
        const float startPathDistanceRatio = 0.4f; // what portion of a boidGrid the leaders would be willing to walk to find a preset path
        const int pathCacheMax = 5;


        // <<Modified Constants> //
        const int startPathDistance = (int)(startPathDistanceRatio * General.boidGridSize);



        Func<int, int, bool> Walkable;

        public EC_PathFinding(Func<int, int, bool> Walkable)
        {
            this.Walkable = Walkable;
            if (Map.tileMap is null) { ECSHandler.debugger.AddLog("Tilemap was null during pathfinder initialisation.", WarningLevel.Error); return; }

            if (pather is null)
            {
                CreatePathers();
            }

            path = null;
        }


        private void CreatePathers()
        {
            pather = new PathFinder(Walkable, maxDist: 1000, useDiagonals: true);
            intraGridPather = new PathFinder(Walkable, maxDist: startPathDistance, useDiagonals: true);
        }




        public void Action(float dt, int uid)
        {
            if (pather is null)
            {
                ECSHandler.debugger.AddLog($"Adding pather...", WarningLevel.Debug);
                CreatePathers();
            }

            EC_Entity? Me = (EC_Entity?)ECSHandler.ECSs[typeof(EC_Entity)][uid];
            if (Me is null) { ECSHandler.debugger.AddLog($"Error, entity {uid} has no entity data!", WarningLevel.Error); return; }

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
                    ECSHandler.debugger.AddLog($"Creating new path...", WarningLevel.Debug);
                    // create new path if none there
                    path = pather.GetPath(tileX, tileY, targetX, targetY);
                    if (path is null || path.Count == 0) 
                    { 
                        ECSHandler.debugger.AddLog($"No path could be found from ({tileX},{tileY}) to ({targetX},{targetY}), self destructing...", WarningLevel.Debug); 
                        ECSHandler.FreeUID(uid); 
                        return; 
                    } // no path could be found, so it should not be there.
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


        public void Cleanup(int uid) { path = null; }
    }
}
