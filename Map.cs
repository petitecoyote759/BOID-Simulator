using ShortTools.Perlin;
using SimpleGraphicsLib;
using System.Net.Mail;
using ShortTools.PlanetaryForge;
using System.Runtime.CompilerServices;

namespace BOIDSimulator
{
    internal static class Map
    {
        public static float[][]? altitudeMap;
        public static TileID[][]? tileMap;



        const int scale = 1;
        public static void CreateMap(int width, int height)
        {
            (TileID[][], float[][]) mapData = MapGenerator.CreateMap(width / scale, height / scale);
            tileMap = mapData.Item1; altitudeMap = mapData.Item2;
        }
    }
}
