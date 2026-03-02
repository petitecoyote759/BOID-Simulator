using ShortTools.PlanetaryForge;
using Silk.NET.SDL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BOIDSimulator
{
    internal static class Renderer
    {
        public static bool running = true;

        private static ManualResetEvent tasksDone = new(false);
        internal static void MainLoop()
        {
            while (running)
            {
                foreach (Action task in tasks)
                {
                    task();
                }
                tasksDone.Reset();
                tasksDone.WaitOne();
            }
        }




        

        static Queue<Action> tasks = new Queue<Action>();
        internal static void RequestDrawGrid(int gridX, int gridY)
        {
            tasks.Enqueue(() => DrawGrid(gridX, gridY));
            tasksDone.Set();
        }


        public const int drawGridSize = 8;
        private static void DrawGrid(int gridX, int gridY)
        {
            if (Map.tileMap is null || Map.altitudeMap is null) { return; }

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
                    renderer.SetPixel(x * size, y * size, size, size, r, g, b);
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
    }
}
