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
    internal static partial class RendererTools
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
        private static IntPtr screenTexture = IntPtr.Zero;

        // <<Drawing Variables>> //
        private static SDL_Rect targetRect = new SDL_Rect();
        private static SDL_Rect srcRect = new SDL_Rect();
        public static readonly Dictionary<string, IntPtr> images = new Dictionary<string, nint>();
        /// <summary>
        /// Other unmanaged textures apart from the images to be cleaned up when renderer is disposed.
        /// </summary>
        public static readonly List<IntPtr> textures = new List<IntPtr>();

        // <<Runtime Variables>> //
        private static long LFT = DateTimeOffset.Now.ToUnixTimeMilliseconds(); // last frame time
        public static bool Running = true;
        private static readonly Thread controllerThread = new Thread(new ThreadStart(SetupRenderer));
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

        }
        static ManualResetEvent setupComplete = new ManualResetEvent(false);
        public static void RequestSetupRenderer()
        {
            controllerThread.Start();
            setupComplete.WaitOne();
        }
        private static void SetupRenderer()
        { 
            // <<SDL Setup>> //
            debugger.AddLog($"Initialising SDL", WarningLevel.Info);
            // Initialised general SDL.
            SDL_Init(SDL_INIT_EVERYTHING | SDL_INIT_SENSOR);
            // png handling setup.
            SDL_image.IMG_Init(SDL_image.IMG_InitFlags.IMG_INIT_PNG);

            SDL_ttf.TTF_Init();
            LoadFonts();

            // Screen setup
            SDL_DisplayMode displayMode;
            if (SDL_GetCurrentDisplayMode(0, out displayMode) != 0)
            {
                debugger.AddLog($"SDL_GetCurrentDisplayMode errored! Activating backup. Error : {GetSDLError()}", WarningLevel.Error);

                // Fallback method
                if (SDL_GetDesktopDisplayMode(0, out displayMode) != 0)
                {
                    debugger.AddLog($"SDL_GetDesktopDisplayMode failed! Screensize could not be obtained. Quitting... Error : {GetSDLError()}");
                    SDL_Quit();
                    return;
                }
            }
            screenWidth = displayMode.w;
            screenHeight = displayMode.h;
            debugger.AddLog($"Monitor resolution obtained as {screenWidth}x{screenHeight}");

            SDLWindow = SDL_CreateWindow("Naturio Window",
                SDL_WINDOWPOS_CENTERED,
                SDL_WINDOWPOS_CENTERED, screenWidth, screenHeight,
                SDL_WindowFlags.SDL_WINDOW_BORDERLESS);

            SDLRenderer = SDL.SDL_CreateRenderer(SDLWindow, -1,
                SDL_RendererFlags.SDL_RENDERER_ACCELERATED |
                SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);



            SDL_SetRenderDrawColor(SDLRenderer, 60, 10, 70, 255); // set default colour to purple


            screenTexture = SDL_CreateTexture(
                SDLRenderer,
                SDL_PIXELFORMAT_RGBA8888,
                (int)SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET,
                screenWidth,
                screenHeight);
            textures.Add(screenTexture);


            setupComplete.Set();


            RunRenderer();
        }





        // <<Controller Thread Section>> //
        const long minMsPerFrame = 10;
        /// <summary>
        /// The state of the renderer, to change state use the <see cref="Pause"/> and <see cref="Resume"/> functions.
        /// </summary>
        public static bool Paused => paused;
        private static bool paused = true;
        private static void RunRenderer()
        {
            LoadImages();
            Setup();

            float dt;
            long dtMs;
            while (Running)
            {
                dt = GetDt(ref LFT, out dtMs);
                Handler.HandleEvents(dt);
                if (paused) { Thread.Sleep(10); continue; } // update LFT before skipping to avoid issues when unpausing
                if (dtMs < minMsPerFrame) { Thread.Sleep((int)(minMsPerFrame - dtMs)); } // caps the fps to a reasonable amount.

                // <<Main Drawing Code>> //

                SDL_RenderClear(SDLRenderer);

                SDL_SetRenderTarget(SDLRenderer, screenTexture);
                Render(dt);
                SDL_SetRenderTarget(SDLRenderer, IntPtr.Zero); // reset to screen

                SDL_RenderCopy(SDLRenderer, screenTexture, IntPtr.Zero, IntPtr.Zero);

                // <<Grid Render>> //
                if (General.renderLines)
                {
                    SDL_SetRenderDrawColor(SDLRenderer, 0, 0, 0, 255);
                    int width = (int)MathF.Ceiling(screenWidth / (float)drawGridTileSize);
                    int height = (int)MathF.Ceiling(screenHeight / (float)drawGridTileSize);
                    for (int x = 0; x < width; x++)
                    {
                        SDL_RenderDrawLine(SDLRenderer, x * drawGridTileSize, 0, x * drawGridTileSize, screenHeight);
                    }
                    for (int y = 0; y < height; y++)
                    {
                        SDL_RenderDrawLine(SDLRenderer, 0, y * drawGridTileSize, screenWidth, y * drawGridTileSize);
                    }
                    SDL_SetRenderDrawColor(SDLRenderer, 60, 10, 70, 255);
                }




                SDL_RenderPresent(SDLRenderer);
            }

            debugger.AddLog($"Running cycle complete", WarningLevel.Info);
            Dispose();
        }

        // <<Thread Controls>> //
        public static void Start() { paused = false; }
        public static void Pause() { paused = true; }
        public static void Resume() { paused = false; }
        public static void Stop() { Running = false; }














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


        // <<Loader Functions>> //
        private static void LoadImages()
        {
            // All images should be contained within the \Images folder
            string[] pngFiles = Directory.GetFiles(currentDirectory + $"\\Images\\", "*.png", SearchOption.AllDirectories);
            string[] bmpFiles = Directory.GetFiles(currentDirectory + $"\\Images\\", "*.bmp", SearchOption.AllDirectories);
            int directoriesToImages = currentDirectory.Split('\\').Length;

            foreach (string path in pngFiles)
            {
                string[] dividedPath = path.Split('\\');
                // Get image name
                StringBuilder builder = new StringBuilder();
                for (int i = directoriesToImages; i < dividedPath.Length; i++)
                {
                    builder.Append(dividedPath[i]);
                    if (i != dividedPath.Length - 1) { builder.Append('\\'); }
                }
                string imageName = builder.ToString();

                if (images.ContainsKey(imageName)) 
                { debugger.AddLog($"Attempted to add image {imageName} from {path}. There was already an entry with that name", WarningLevel.Error); continue;  }

                IntPtr imagePointer = SDL_image.IMG_LoadTexture(SDLRenderer, path);

                if (imagePointer == IntPtr.Zero)
                { debugger.AddLog($"Image {imageName} could not be loaded. Error : {GetSDLError()}", WarningLevel.Error); continue; }


                images.Add(imageName, imagePointer);
                debugger.AddLog($"Loaded \"{imageName}\" : {imagePointer}", WarningLevel.Info);
            }
        }

        private static void LoadFonts()
        {
            // All fonts contained in the \Fonts folder
            string[] files = Directory.GetFiles(currentDirectory + $"\\Fonts\\", "*.ttf", SearchOption.AllDirectories);
            foreach (string path in files)
            {
                string fileName = path.Split('\\').Last();
                string fontName = fileName.Split('.').First();
                IntPtr fontPointer = SDL_ttf.TTF_OpenFont(path, 128);
                // Font didnt load
                if (fontPointer == IntPtr.Zero) { debugger.AddLog($"Font at {fileName} couldnt be loaded. Reason : {GetSDLError()}", WarningLevel.Error); }
                // Font name already found
                else if (fonts.ContainsKey(fontName)) { debugger.AddLog($"Font at {fileName} has name {fontName}, one is already loaded.", WarningLevel.Error); }
                // Success
                else { fonts.Add(fontName, fontPointer); }
            }
        }


        private static string GetSDLError() { string error = SDL_GetError(); SDL_ClearError(); return error; }







        private static bool disposed = false;
        private static void Dispose()
        {
            if (disposed) { debugger.AddLog($"Attempted recall of dispose", WarningLevel.Warning); return; }
            disposed = true;
            debugger.AddLog($"Disposing...", WarningLevel.Info);

            // dispose images, font, and then close sdl
            while (images.Count > 0)
            {
                KeyValuePair<string, IntPtr> image = images.First();
                images.Remove(image.Key);
                SDL_DestroyTexture(image.Value);
            }
            while (textures.Count > 0)
            {
                IntPtr texture = textures.First();
                textures.RemoveAt(0);
                SDL_DestroyTexture(texture);
            }
            while (fonts.Count > 0)
            {
                KeyValuePair<string, IntPtr> font = fonts.First();
                fonts.Remove(font.Key);
                SDL_ttf.TTF_CloseFont(font.Value);
            }

            SDL_DestroyRenderer(SDLRenderer);
            SDL_DestroyWindow(SDLWindow);
            SDL_Quit();
        }
    
    
    
    
    
    
    
    
    
    
    
    
    
        public static void Main()
        {
            LoadImages();
        }
    }

}
