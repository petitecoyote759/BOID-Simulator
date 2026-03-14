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
            SDL_SetTextureBlendMode(mapTexture, SDL_BlendMode.SDL_BLENDMODE_NONE);

            textures.Add(mapTexture);

            SDL_FreeSurface(surface);


            // <<Full Map Render>> //
            int width = (int)MathF.Ceiling(screenWidth / (float)drawGridTileSize);
            int height = (int)MathF.Ceiling(screenHeight / (float)drawGridTileSize);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    gridDrawRequest.Add((x, y));
                }
            }
        }





        static HashSet<(int, int)> gridDrawRequest = new HashSet<(int, int)>();
        static HashSet<int> entityDrawRequests = new HashSet<int>();
        private static void Render(float dt)
        {
            if (General.refresh)
            {
                // <<Full Map Render>> //
                int width = (int)MathF.Ceiling(screenWidth / (float)drawGridTileSize);
                int height = (int)MathF.Ceiling(screenHeight / (float)drawGridTileSize);
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        gridDrawRequest.Add((x, y));
                    }
                }
                General.refresh = false;
            }


            ECSHandler.DoEntityRenderTasks(dt);

            srcRect.w = drawGridTileSize + 2;
            srcRect.h = srcRect.w;
            srcRect.x = 0; srcRect.y = 0;

            targetRect.w = (int)((drawGridTileSize + 2) * Camera.zoom);
            targetRect.h = targetRect.w;

            SDL_SetTextureBlendMode(screenTexture, SDL_BlendMode.SDL_BLENDMODE_NONE);
            lock (gridDrawRequest)
            {
                foreach ((int, int) coordinate in gridDrawRequest)
                {
                    targetRect.x = GetPx((coordinate.Item1 * drawGridTileSize) - 1); // draw with an extra pixel of buffer to help with zoom float issues
                    targetRect.y = GetPy((coordinate.Item2 * drawGridTileSize) - 1);
                    if (targetRect.x < -targetRect.w || targetRect.y < -targetRect.w ||
                        targetRect.x >= screenWidth || targetRect.y >= screenHeight)
                    {
                        continue;
                    }

                    srcRect.x = (coordinate.Item1 * drawGridTileSize) - 1; srcRect.y = (coordinate.Item2 * drawGridTileSize) - 1;
                    //SDL_RenderCopy(SDLRenderer, mapTexture, ref srcRect, ref srcRect);
                    SDL_RenderCopy(SDLRenderer, mapTexture, ref srcRect, ref targetRect);
                }
                gridDrawRequest.Clear();
            }
            SDL_SetTextureBlendMode(screenTexture, SDL_BlendMode.SDL_BLENDMODE_BLEND);
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
                        targetRect.x = GetPx(entityData.position.X);
                        targetRect.y = GetPy(entityData.position.Y);

                        if (Walker.Walkable(targetRect.x, targetRect.y))
                        {
                            SDL_SetRenderDrawColor(SDLRenderer, 60, 10, 70, 255);
                        }
                        else
                        {
                            SDL_SetRenderDrawColor(SDLRenderer, 60, 10, 70, 20);
                        }

                        SDL_RenderDrawPoint(SDLRenderer, targetRect.x, targetRect.y);
                    }
                    else
                    {
                        targetRect.x = GetPx(entityData.position.X);
                        targetRect.y = GetPy(entityData.position.Y);
                        targetRect.w = (int)(renderComponent.width * Camera.zoom);
                        targetRect.h = (int)(renderComponent.height * Camera.zoom);
                        byte oldAlpha = 0;
                        bool notWalkable = !Walker.Walkable((int)entityData.position.X, (int)entityData.position.Y);
                        if (notWalkable)
                        {
                            SDL_GetTextureAlphaMod(renderComponent.image, out oldAlpha);
                            SDL_SetTextureAlphaMod(renderComponent.image, 40);
                        }
                        SDL_RenderCopyEx(SDLRenderer, renderComponent.image, IntPtr.Zero, ref targetRect, renderComponent.angle, IntPtr.Zero, SDL_RendererFlip.SDL_FLIP_NONE);
                    
                        if (notWalkable)
                        {
                            SDL_SetTextureAlphaMod(renderComponent.image, oldAlpha);
                        }
                    }
                }
                entityDrawRequests.Clear();
            }


            // <<FPS Drawing>> //
            Write(0, 0, 20, 30, currentFPS.ToString());
            Write(0, 40, 20, 30, ECSHandler.currentFPS.ToString());
        }


        private static Dictionary<string, IntPtr> textCache = new Dictionary<string, IntPtr>();
        private static void Write(int posX, int posY, int widthPerChar, int height, string text, string font = "Aller_Bd")
        {
            IntPtr textImage;
            if (textCache.ContainsKey(text))
            {
                textImage = textCache[text];
            }
            else
            {
                IntPtr surface = SDL_ttf.TTF_RenderText_Solid(fonts[font], text, Black);
                textImage = SDL_CreateTextureFromSurface(SDLRenderer, surface);
                SDL_FreeSurface(surface);
                textCache.Add(text, textImage);
                textures.Add(textImage);
            }

            targetRect.x = posX; targetRect.y = posY;
            targetRect.w = widthPerChar * text.Length; targetRect.h = height;
            SDL_RenderCopy(SDLRenderer, textImage, IntPtr.Zero, ref targetRect);
        }




        private static int GetPx(float x)
        {
            return (int)(Camera.zoom * (x - Camera.x));
        }
        private static int GetPy(float y)
        {
            return (int)(Camera.zoom * (y - Camera.y));
        }










        // <<Requests>> //
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
