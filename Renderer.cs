using BOIDSimulator.ECS_Components;
using ShortTools.General;
using ShortTools.PlanetaryForge;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BOIDSimulator
{
    internal static class Renderer
    {
        public static bool running = true;

        public const int drawGridSize = 64;
        const int boidSize = 4;

        internal static Debugger debugger = new Debugger("Renderer", WarningLevel.Debug, DebuggerFlag.PrintLogs, DebuggerFlag.WriteLogsToFile, DebuggerFlag.DisplayThread);

        private const int MaxFPS = 120;
        private const long MaxMsPerFrame = 1000 / MaxFPS;
        private const int secondsPerFPSUpdate = 10;
        private const long ticksPerFPSUpdate = secondsPerFPSUpdate * 1000;
        private static int frameCount = 0;
        private static long FPSUpateTimer = 0;
        static long LFT = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        private const int PPT = 1;
        internal static void MainLoop()
        {
            if (Map.tileMap is null) { return; }
            if (running == false) { debugger.AddLog($"Shutting down renderer", WarningLevel.Info); debugger.Dispose(true); return; }



            // <<Frame Timing and Counting>> //
            long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            long delta = now - LFT;
            int makeupTime = (int)(MaxMsPerFrame - delta); // the amount of time 
            if (makeupTime > 0)
            {
                Thread.Sleep(makeupTime);
            }
            float dt = delta / 1000f;
            LFT = now;

            frameCount++;
            FPSUpateTimer += delta;
            if (FPSUpateTimer > ticksPerFPSUpdate)
            {
                FPSUpateTimer -= ticksPerFPSUpdate;
                debugger.AddLog($"Renderer Frame Count {frameCount} over {secondsPerFPSUpdate} giving {frameCount / secondsPerFPSUpdate} FPS", WarningLevel.Debug);
                frameCount = 0;
            }



            // <<Initial Rendering>> //
            int width = (int)MathF.Ceiling(Map.tileMap.Length / (float)drawGridSize);
            int height = (int)MathF.Ceiling(Map.tileMap[0].Length / (float)drawGridSize);

            if (General.refresh)
            {
                General.refresh = false;
                debugger.AddLog($"Starting full map render at {DateTimeOffset.Now.ToUnixTimeMilliseconds()} with dimentions {width}x{height}", WarningLevel.Debug);
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        DrawGrid(x, y);
                    }
                }
                debugger.AddLog($"Completed full map render at {DateTimeOffset.Now.ToUnixTimeMilliseconds()}", WarningLevel.Debug);
            }


            ECSHandler.DoEntityRenderTasks(dt);


            // <<Main Grid Rendering>> //
            HashSet<(int, int)> currentGridsToDraw;
            lock (gridsToDraw)
            {
                currentGridsToDraw = new HashSet<(int, int)>(gridsToDraw);
                gridsToDraw = new HashSet<(int, int)>();
            }
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (currentGridsToDraw.Contains((x, y)))
                    {
                        DrawGrid(x, y);
                    }
                }
            }
            DrawEntities();
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DrawEntities()
        {
            lock (entitiesToDraw)
            {
                foreach (KeyValuePair<(int, int), HashSet<int>> pair in entitiesToDraw)
                {
                    foreach (int uid in pair.Value)
                    {
                        RenderBoid(uid);
                    }
                }
                entitiesToDraw.Clear();
            }
        }



        static Dictionary<(int, int), HashSet<int>> entitiesToDraw = new Dictionary<(int, int), HashSet<int>>();
        static HashSet<(int, int)> gridsToDraw = new HashSet<(int, int)>();
        public static void RequestDrawGrid(int gridX, int gridY)
        {
            lock (gridsToDraw)
            {
                gridsToDraw.Add((gridX, gridY));
            }
        }
        public static void RequestEntityDraw(int gridX, int gridY, int uid)
        {
            if (entitiesToDraw.ContainsKey((gridX, gridY)) == false) 
            { entitiesToDraw.Add((gridX, gridY), new HashSet<int>()); }
            lock (entitiesToDraw[(gridX, gridY)])
            {
                entitiesToDraw[(gridX, gridY)].Add(uid);
            }
        }

        private static void DrawGrid(int gridX, int gridY)
        {
            if (Map.tileMap is null || Map.altitudeMap is null) { debugger.AddLog($"Attempted to draw grid {gridX}x{gridY} when map was null", WarningLevel.Warning); return; }

            int width = Map.tileMap.Length;
            int height = Map.tileMap[0].Length;
            int size = PPT; // pixels per tile

            for (int x = gridX * drawGridSize; x < (gridX + 1) * drawGridSize; x++)
            {
                for (int y = gridY * drawGridSize; y < (gridY + 1) * drawGridSize; y++)
                {
                    if (x >= Map.tileMap.Length || y >= Map.tileMap[0].Length) { continue; }
                    Tuple<byte, byte, byte> colours = TileColours[Map.tileMap[x][y]];
                    byte r = (byte)(colours.Item1 * ((3 + Map.altitudeMap[x][y]) / 4f));
                    byte g = (byte)(colours.Item2 * ((3 + Map.altitudeMap[x][y]) / 4f));
                    byte b = (byte)(colours.Item3 * ((3 + Map.altitudeMap[x][y]) / 4f));
                    General.renderer.SetPixel(x * size, y * size, size, size, r, g, b);
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
            if (entityData is null) { debugger.AddLog($"Entity {uid} has no entity data, flagged {ECSHandler.entities[uid]} in the entity table.", WarningLevel.Error); return; }

            EC_BoidLogic? boidLogic = (EC_BoidLogic?)ECSHandler.ECSs[typeof(EC_BoidLogic)][uid];
            if (boidLogic is not null)
            {
                // it is a boid
                if (boidLogic.Value.leader && General.showLeaders)
                {
                    General.renderer.SetPixel(
                    (int)((entityData.Value.position.X - (boidSize * 2)) * PPT),
                    (int)((entityData.Value.position.Y - (boidSize * 2)) * PPT),
                    boidSize * 4 * PPT,
                    boidSize * 4 * PPT,
                    150,
                    100,
                    255
                    );
                    return;
                }
            }

            General.renderer.SetPixel(
            (int)(entityData.Value.position.X * PPT),
            (int)(entityData.Value.position.Y * PPT),
            boidSize * PPT,
            boidSize * PPT,
            255,
            200,
            100
            );
        }
    }
}
