using ShortTools.General;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SDL2.SDL;

namespace BOIDSimulator.Renderer
{
    internal static class Handler
    {
        public static Dictionary<SDL_Keycode, bool> keys = new Dictionary<SDL_Keycode, bool>()
        {
            { SDL_Keycode.SDLK_w, false },
            { SDL_Keycode.SDLK_a, false },
            { SDL_Keycode.SDLK_s, false },
            { SDL_Keycode.SDLK_d, false },
            { SDL_Keycode.SDLK_LSHIFT, false }
        };

        public static void HandleEvents(float dt)
        {
            float oldX = Camera.x;
            float oldY = Camera.y;
            float oldZoom = Camera.zoom;

            while (SDL_PollEvent(out SDL_Event e) == 1)
            {
                switch (e.type)
                {
                    case SDL_EventType.SDL_QUIT: // ensures that quitting works and runs cleanup code
                        RendererTools.Stop();
                        break;

                    case SDL_EventType.SDL_KEYDOWN:
                        //RendererTools.debugger.AddLog($"{e.key.keysym.sym}");
                        if (keys.ContainsKey(e.key.keysym.sym)) { keys[e.key.keysym.sym] = true; }
                        break;
                    case SDL_EventType.SDL_KEYUP:
                        //RendererTools.debugger.AddLog($"{e.key.keysym.sym}");
                        if (keys.ContainsKey(e.key.keysym.sym)) { keys[e.key.keysym.sym] = false; }
                        break;

                    case SDL_EventType.SDL_WINDOWEVENT:
                        // RendererTools.debugger.AddLog($"{e.window.windowEvent}");
                        // SDL_WindowEvent_LEAVE
                        // SDL_WindowEvent_Focus_Gained
                        break;

                    case SDL_EventType.SDL_MOUSEWHEEL:
                        //RendererTools.debugger.AddLog($"{e.wheel.preciseX} {e.wheel.preciseY}", WarningLevel.Debug);
                        // preciseY to get it, + is up, - is down
                        if (e.wheel.preciseY < 0)
                        {
                            Camera.zoom /= Camera.zoomSpeed * -e.wheel.preciseY;
                        }
                        else if (e.wheel.preciseY > 0)
                        {
                            Camera.zoom *= Camera.zoomSpeed * e.wheel.preciseY;
                        }
                        // Zoom normalisation
                        // Makes the camera zoom based on the middle
                        Camera.zoom = float.Clamp(Camera.zoom, 1, Camera.zoomMax);
                        float dx = RendererTools.ScreenWidth * (Camera.zoom - oldZoom) / (2 * Camera.zoom * oldZoom);
                        float dy = RendererTools.ScreenHeight * (Camera.zoom - oldZoom) / (2 * Camera.zoom * oldZoom);
                        Camera.x += dx;
                        Camera.y += dy;
                        break;

                    default:

                        RendererTools.debugger.AddLog($"{e.type}", WarningLevel.Debug);
                        break;
                }
            }

            Camera.currentSpeed = Camera.speed;

            if (keys[SDL_Keycode.SDLK_LSHIFT])
            {
                Camera.currentSpeed *= 2;
            }
            if (keys[SDL_Keycode.SDLK_w])
            {
                Camera.y -= Camera.currentSpeed * dt;
            }
            if (keys[SDL_Keycode.SDLK_a])
            {
                Camera.x -= Camera.currentSpeed * dt;
            }
            if (keys[SDL_Keycode.SDLK_s])
            {
                Camera.y += Camera.currentSpeed * dt;
            }
            if (keys[SDL_Keycode.SDLK_d])
            {
                Camera.x += Camera.currentSpeed * dt;
            }
            

            if (oldX != Camera.x || oldY != Camera.y || oldZoom != Camera.zoom)
            {
                int width = (int)(RendererTools.ScreenWidth / Camera.zoom);
                int height = (int)(RendererTools.ScreenHeight / Camera.zoom);
                Camera.x = float.Clamp(Camera.x, 0, Map.tileMap.Length - width);
                Camera.y = float.Clamp(Camera.y, 0, Map.tileMap[0].Length - height);

                General.refresh = true;
            }
        }
    }
}
