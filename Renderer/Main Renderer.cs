using BOIDSimulator.ECS_Components;
using SDL2;
using ShortTools.General;
using ShortTools.PlanetaryForge;
using System;
using System.Collections.Concurrent;
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
        public const int drawGridTileSize = 16;

        public static int drawGridWidth = -1;
        public static int drawGridHeight = -1;

        public static void Setup()
        {
            while (Map.tileMap is null) { Thread.Sleep(10); }

            // <<Map Image Generation>> //

            debugger.AddLog($"Creating surface for map of size {screenWidth}x{screenHeight}", WarningLevel.Debug);
            

            targetRect.w = 1;
            targetRect.h = 1;

            // <<Main Image Creation>> //
            IntPtr surface = SDL_CreateRGBSurfaceWithFormat(0, screenWidth, screenHeight, 32, SDL_PIXELFORMAT_RGBA8888);
            SDL_LockSurface(surface);
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

            SDL_FreeSurface(surface);
        }





        static HashSet<(int, int)> gridDrawRequest = new HashSet<(int, int)>();
        static HashSet<int> entityDrawRequests = new HashSet<int>();
        private static void Render(float dt)
        {
            Handler.HandleEvents();
            ECSHandler.DoEntityRenderTasks(dt);

            srcRect.w = drawGridTileSize;
            srcRect.h = drawGridTileSize;
            srcRect.x = 0; srcRect.y = 0;

            targetRect.w = drawGridTileSize;
            targetRect.h = drawGridTileSize;

            lock (gridDrawRequest)
            {
                foreach ((int, int) coordinate in gridDrawRequest)
                {
                    srcRect.x = coordinate.Item1 * drawGridTileSize; srcRect.y = coordinate.Item2 * drawGridTileSize;
                    SDL_RenderCopy(SDLRenderer, mapTexture, ref srcRect, ref srcRect);
                }
                gridDrawRequest.Clear();
            }
            lock (entityDrawRequests)
            {
                foreach (int uid in entityDrawRequests)
                {
                    bool success = ECSHandler.GetEntityComponent(uid, out EC_Render renderComponent);
                    success &= ECSHandler.GetEntityComponent(uid, out EC_Entity entityData);
                    if (!success) { continue; }

                    if (renderComponent.image == IntPtr.Zero) 
                    {
                        targetRect.w = 1; targetRect.h = 1;
                        targetRect.x = (int)entityData.position.X;
                        targetRect.y = (int)entityData.position.Y;

                        SDL_RenderDrawPoint(SDLRenderer, targetRect.x, targetRect.y);
                    }
                }
                entityDrawRequests.Clear();
            }
        }
        public static void RequestDrawGrid(int gridX, int gridY)
        {
            lock (gridDrawRequest)
            {
                gridDrawRequest.Add((gridX, gridY));
            }
        }
        public static void RequestEntityDraw(int gridX, int gridY, int uid)
        {
            lock (entityDrawRequests)
            {
                entityDrawRequests.Add(uid);
            }
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
