using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SDL2;
using ShortTools.General;
using static SDL2.SDL;

namespace BOIDSimulator.Renderer
{
    internal static class RendererTools
    {
        // <<SDL Pointers>> //
        private static IntPtr SDLRenderer;
        private static IntPtr SDLWindow;

        // <<Fonts>> //
        private static Dictionary<string, IntPtr> fonts = new Dictionary<string, IntPtr>()
        {

        };
        public static readonly SDL_Color Black = new SDL_Color() { r = 0, g = 0, b = 0, a = 255 };
        public static readonly SDL_Color White = new SDL_Color() { r = 255, g = 255, b = 255, a = 255 };

        // <<Screen Info>> //
        public static int ScreenWidth => screenWidth;
        private static int screenWidth;
        public static int ScreenHeight => screenHeight;
        private static int screenHeight;

        // <<Drawing Variables>> //
        private static SDL_Rect targetRect;
        private static SDL_Rect srcRect;
        private static readonly Dictionary<string, IntPtr> images = new Dictionary<string, nint>();

        // <<Runtime Variables>> //
        private static long LFT = DateTimeOffset.Now.ToUnixTimeMilliseconds(); // last frame time
        public static bool Running = true;
        private static readonly Thread controllerThread = new Thread(new ThreadStart(ControllerEntryPoint));
        private static string currentDirectory;
        public static readonly Debugger debugger;

        // <<Synchronisation>> //
        // ConcurrentQueue is completely thread safe, so no locks are needed.
        // Loading
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RequestImageLoad(string path, string? name = null)
        {
            imageLoadRequests.Enqueue((path, name ?? path.Split('\\').Last()));
        }
        private static ConcurrentQueue<(string, string)> imageLoadRequests = new ConcurrentQueue<(string, string)>();
        // Deletion
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RequestImageDeletion(string name)
        {
            imageDeleteRequests.Enqueue(name);
        }
        private static ConcurrentQueue<string> imageDeleteRequests = new ConcurrentQueue<string>();





        static RendererTools()
        {
            // <<Misc Variables Setup>> //
            currentDirectory = Directory.GetCurrentDirectory();

            debugger = new Debugger("Naturio Renderer",
#if DEBUG
                WarningLevel.Debug,
#else //      Compiles with debug output if in debug mode, or info output if in release mode.
                WarningLevel.Info,
#endif
                DebuggerFlag.PrintLogs, DebuggerFlag.WriteLogsToFile, DebuggerFlag.DisplayThread);

            controllerThread.Name = "Naturio Renderer Thread";


            // <<SDL Setup>> //
            // Initialised general SDL.
            SDL_Init(SDL_INIT_EVERYTHING | SDL_INIT_SENSOR);
            // png handling setup.
            SDL_image.IMG_Init(SDL_image.IMG_InitFlags.IMG_INIT_PNG);

            // Screen setup
            SDL_DisplayMode displayMode;
            if (SDL_GetCurrentDisplayMode(0, out displayMode) != 0)
            {
                debugger.AddLog($"SDL_GetCurrentDisplayMode errored! Activating backup. Error : {SDL_GetError()}", WarningLevel.Error);

                // Fallback method
                if (SDL_GetDesktopDisplayMode(0, out displayMode) != 0)
                {
                    debugger.AddLog($"SDL_GetDesktopDisplayMode failed! Screensize could not be obtained. Quitting... Error : {SDL_GetError()}");
                    SDL_Quit();
                    return;
                }
            }

            SDLWindow = SDL_CreateWindow("Naturio Window",
                SDL_WINDOWPOS_CENTERED,
                SDL_WINDOWPOS_CENTERED, screenWidth, screenHeight,
                SDL_WindowFlags.SDL_WINDOW_BORDERLESS);

            SDLRenderer = SDL.SDL_CreateRenderer(SDLWindow, -1,
                SDL_RendererFlags.SDL_RENDERER_ACCELERATED |
                SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);



            SDL_SetRenderDrawColor(SDLRenderer, 60, 10, 70, 255); // set default colour to purple
        }






        const long minMsPerFrame = 10;
        private static void ControllerEntryPoint()
        {
            LoadImages();

            float dt;
            long dtMs;
            while (Running)
            {
                dt = GetDt(ref LFT, out dtMs);
                if (dtMs < minMsPerFrame) { Thread.Sleep((int)(minMsPerFrame - dtMs)); } // caps the fps to a reasonable amount.


            }
        }



        /// <summary>
        /// Gets the delta time in seconds since the last frame time as given, and updates the last frame time.
        /// Also returns the delta time in milliseconds as an out variable
        /// </summary>
        private static float GetDt(ref long LFT, out long dtMs)
        {
            long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            dtMs = (now - LFT);
            LFT = now;
            return dtMs / 1000f;
        }



        private static void LoadImages()
        {
            // All images should be contained within the \Images folder
        }
    }

}
