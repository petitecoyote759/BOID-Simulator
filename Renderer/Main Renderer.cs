using SDL2;
using ShortTools.General;
using ShortTools.PlanetaryForge;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static SDL2.SDL;

namespace BOIDSimulator.Renderer
{
    // Game specific functions.
    internal static partial class RendererTools
    {
        private static IntPtr mapTexture;
        private static readonly Dictionary<TileID, uint> TileColours = new Dictionary<TileID, uint>()
        {
            { TileID.Cliff, 0x4B4B4BFF },
            { TileID.Water, 0x14A0C8FF },
            { TileID.Sand, 0xC8C814FF },
            { TileID.Grass, 0x0A820AFF },
            { TileID.Forest, 0x0F7314FF }
        };
        public const int drawGridSize = 16;



        public static void Setup()
        {
            while (Map.tileMap is null) { Thread.Sleep(10); }

            // <<Map Image Generation>> //
            IntPtr surface = SDL_CreateRGBSurfaceWithFormat(0, screenWidth, screenHeight, 32, SDL_PIXELFORMAT_RGBA8888);
            debugger.AddLog($"Creating surface for map of size {screenWidth}x{screenHeight}", WarningLevel.Debug);
            SDL_LockSurface(surface);

            targetRect.w = 1;
            targetRect.h = 1;

            for (int x = 0; x < Map.tileMap.Length; x++)
            {
                targetRect.x = x;
                for (int y = 0; y < Map.tileMap[0].Length; y++)
                {
                    targetRect.y = y;

                    RGBA8888Colour colour = new RGBA8888Colour() { data = TileColours[Map.tileMap[x][y]] };

                    colour.r = (byte)(colour.r * ((5 + Map.altitudeMap[x][y]) / 6f));
                    colour.g = (byte)(colour.g * ((5 + Map.altitudeMap[x][y]) / 6f));
                    colour.b = (byte)(colour.b * ((5 + Map.altitudeMap[x][y]) / 6f));

                    SDL_FillRect(surface, ref targetRect, colour.data);
                }
            }

            SDL_UnlockSurface(surface);

            SDL_image.IMG_SavePNG(surface, "Test.png");
            mapTexture = SDL_CreateTextureFromSurface(SDLRenderer, surface);

            textures.Add(mapTexture);
        }






        private static void Render(float dt)
        {
            Handler.HandleEvents();

            srcRect.w = screenWidth;
            srcRect.h = screenHeight;
            srcRect.x = 0;
            srcRect.y = 0;

            targetRect.w = screenWidth;
            targetRect.h = screenHeight;
            targetRect.x = 0;
            targetRect.y = 0;
            SDL_RenderCopyEx(SDLRenderer, mapTexture, ref srcRect, ref targetRect, 0, 0, SDL_RendererFlip.SDL_FLIP_NONE);
        }
        public static void RequestDrawGrid(params dynamic[] args)
        {

        }
        public static void RequestEntityDraw(params dynamic[] args)
        {

        }

    }



    [StructLayout(LayoutKind.Explicit)]
    internal struct RGBA8888Colour
    {
        [FieldOffset(0)] public uint data;
        [FieldOffset(0)] public byte r;
        [FieldOffset(1)] public byte g;
        [FieldOffset(2)] public byte b;
        [FieldOffset(3)] public byte a;
    }
}
