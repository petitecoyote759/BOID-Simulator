using BOIDSimulator.ECS_Components;
using ShortTools.PlanetaryForge;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ShortTools.General;
using System.Threading.Tasks;

namespace BOIDSimulator
{
    internal static class Renderer
    {
        public static bool running = true;

        private static bool first = true;
        internal static void MainLoop()
        {
            if (Map.tileMap is null) { return; }
            if (running == false) { return; }

            int width = Map.tileMap.Length / drawGridSize;
            int height = Map.tileMap[0].Length / drawGridSize;

            if (first)
            {
                first = false;
                General.debugger.AddLog($"Starting full map render at {DateTimeOffset.Now.ToUnixTimeMilliseconds()} with dimentions {width}x{height}", WarningLevel.Debug);
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        DrawGrid(x, y);
                    }
                }
                General.debugger.AddLog($"Completed full map render at {DateTimeOffset.Now.ToUnixTimeMilliseconds()}", WarningLevel.Debug);
            }

            while (tasks.Count > 0)
            {
                Action task;
                lock (tasks)
                {
                    task = tasks.Dequeue();
                }
                task();
            }
        }





        static Dictionary<(int, int), HashSet<int>> entitiesToDraw = new Dictionary<(int, int), HashSet<int>>();
        static Queue<Action> tasks = new Queue<Action>();
        public static void RequestDrawGrid(int gridX, int gridY)
        {
            Action task = () => DrawGrid(gridX, gridY);
            lock (tasks)
            {
                tasks.Enqueue(task);
            }
        }
        public static void RequestEntityDraw(int gridX, int gridY, int uid)
        {
            if (entitiesToDraw.ContainsKey((gridX, gridY)) == false) { entitiesToDraw.Add((gridX, gridY), new HashSet<int>()); }
            entitiesToDraw[(gridX, gridY)].Add(uid);
        }


        public const int drawGridSize = 8;
        const int boidSize = 2;
        private static void DrawGrid(int gridX, int gridY)
        {
            if (Map.tileMap is null || Map.altitudeMap is null) { General.debugger.AddLog($"Attempted to draw grid {gridX}x{gridY} when map was null", WarningLevel.Warning); return; }

            int width = Map.tileMap.Length;
            int height = Map.tileMap[0].Length;
            int size = General.PPT; // pixels per tile

            for (int x = gridX * drawGridSize; x < (gridX + 1) * drawGridSize; x++)
            {
                for (int y = gridY * drawGridSize; y < (gridY + 1) * drawGridSize; y++)
                {
                    Tuple<byte, byte, byte> colours = TileColours[Map.tileMap[x][y]];
                    byte r = (byte)(colours.Item1 * ((3 + Map.altitudeMap[x][y]) / 4f));
                    byte g = (byte)(colours.Item2 * ((3 + Map.altitudeMap[x][y]) / 4f));
                    byte b = (byte)(colours.Item3 * ((3 + Map.altitudeMap[x][y]) / 4f));
                    General.renderer.SetPixel(x * size, y * size, size, size, r, g, b);
                }
            }

            if (entitiesToDraw.ContainsKey((gridX, gridY)))
            {
                HashSet<int> entities = entitiesToDraw[(gridX, gridY)];
                foreach (int uid in entities)
                {
                    RenderBoid(uid);
                }
            }
        }

        private static readonly Dictionary<TileID, Tuple<byte, byte, byte>> TileColours = new Dictionary<TileID, Tuple<byte, byte, byte>>()
        {
            { TileID.Cliff, new Tuple<byte, byte, byte>(75, 75, 75) },
            { TileID.Water, new Tuple<byte, byte, byte>(20, 160, 200) },
            { TileID.Sand, new Tuple<byte, byte, byte>(200, 200, 20) },
            { TileID.Grass, new Tuple<byte, byte, byte>(10, 130, 10) },
            { TileID.Forest, new Tuple<byte, byte, byte>(15, 115, 20) }
        };




        private static void RenderBoid(int uid)
        {
            EC_Entity? entityData = (EC_Entity?)ECSHandler.ECSs[typeof(EC_Entity)][uid];
            if (entityData is null) { General.debugger.AddLog($"Entity {uid} has no entity data, flagged {ECSHandler.entities[uid]} in the entity table.", WarningLevel.Error); return; }

            General.renderer.SetPixel(
            (int)(entityData.Value.position.X * General.PPT),
            (int)(entityData.Value.position.Y * General.PPT),
            boidSize * General.PPT,
            boidSize * General.PPT,
            255,
            200,
            100
            );
        }
    }
}
