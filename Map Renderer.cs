using ShortTools.Perlin;
using SimpleGraphicsLib;
using System.Net.Mail;
using ShortTools.PlanetaryForge;

namespace BOIDSimulator
{
    internal static class MapRenderer
    {
        static float[][] altitudeMap;
        static Tuple<int, int> centre = new Tuple<int, int>(0, 0);


        const int scale = 1;
        static bool first = true;

        public static void Render(GraphicsHandler renderer, TileID[][] tileMap, List<IBoid>[][] boidGrid)
        {
            if (tileMap is null) { return; }

            int width = tileMap.Length;
            int height = tileMap[0].Length;
            int size = General.PPT; // pixels per tile

            for (int x = 0; x < width; x++)
            {
                int gridX = x / General.boidGridSize;
                for (int y = 0; y < height; y++)
                {
                    int gridY = y / General.boidGridSize;
                    if (boidGrid[gridX][gridY].Count == 0 && !first) { continue; }

                    if (General.renderLines && (y % General.boidGridSize == 0 || x % General.boidGridSize == 0)) { renderer.SetPixel(x * size, y * size, size, size, 0, 0, 0); continue; }
                    Tuple<byte, byte, byte> colours = TileColours[tileMap[x][y]];
                    byte r = (byte)(colours.Item1 * ((3 + altitudeMap[x][y]) / 4f));
                    byte g = (byte)(colours.Item2 * ((3 + altitudeMap[x][y]) / 4f));
                    byte b = (byte)(colours.Item3 * ((3 + altitudeMap[x][y]) / 4f));
                    renderer.SetPixel(x * size, y * size, size, size, r, g, b);
                }
            }

            first = false;
        }

        private static readonly Dictionary<TileID, Tuple<byte, byte, byte>> TileColours = new Dictionary<TileID, Tuple<byte, byte, byte>>()
        {
            { TileID.Cliff, new Tuple<byte, byte, byte>(75, 75, 75) },

            { TileID.Water, new Tuple<byte, byte, byte>(20, 160, 200) },

            { TileID.Sand, new Tuple<byte, byte, byte>(200, 200, 20) },

            { TileID.Grass, new Tuple<byte, byte, byte>(10, 130, 10) },

            { TileID.Forest, new Tuple<byte, byte, byte>(15, 115, 20) }
    };




        public static TileID[][] CreateMap(int width, int height)
        {
            (TileID[][], float[][]) mapData = MapGenerator.CreateMap(width / scale, height / scale);

            altitudeMap = mapData.Item2;
            return mapData.Item1;
        }
    }
}
