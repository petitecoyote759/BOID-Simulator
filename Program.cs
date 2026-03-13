using ShortTools.PlanetaryForge;
using ShortTools.General;
using ShortTools.MagicContainer;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Security.Cryptography;
using static ShortTools.General.Prints;
using Debugger = ShortTools.General.Debugger;
using BOIDSimulator.ECS_Components;
using System.Numerics;
using BOIDSimulator.Renderer;


namespace BOIDSimulator
{
    public static class General
    {
        public static List<IBoid> allBoids = new List<IBoid>();
        public const int boidGridSize = 48;
        const int boids = 10000;
        const bool leadingBoids = true;
        public static Debugger debugger;





        public static bool Walkable(TileID tile)
        {
            return tile switch
            {
                TileID.Water => false,
                TileID.Grass => true,
                TileID.Sand => true,
                TileID.Forest => true,
                TileID.Cliff => false,
                _ => false
            };
        }

        public static void Main(string[] args)
        {
            Random random = new Random();
            Thread.CurrentThread.Name = "Render Thread";

            debugger = new Debugger("Main",
#if DEBUG
                WarningLevel.Debug,
#else
                WarningLevel.Info,
#endif
                DebuggerFlag.PrintLogs, DebuggerFlag.WriteLogsToFile, DebuggerFlag.DisplayThread);
            debugger.DefaultLevel = WarningLevel.Info;



            debugger.AddLog($"Starting Perlin Demo with {boids} boids");
            debugger.AddLog($"Dimensions = {RendererTools.ScreenWidth}x{RendererTools.ScreenHeight}", WarningLevel.Debug);

            RendererTools.RequestSetupRenderer();
            Map.CreateMap(RendererTools.ScreenWidth, RendererTools.ScreenHeight);

            int boidGridw = (RendererTools.ScreenWidth / (boidGridSize)) + 1;
            int boidGridh = (RendererTools.ScreenHeight / (boidGridSize)) + 1;
            debugger.AddLog($"Boidgrid initialising with dimensions of {boidGridw}x{boidGridh}");

            debugger.AddLog($"Width of {boidGridw}x{boidGridh}", WarningLevel.Debug);
            EC_BoidLogic.boidGrid = new List<int>[boidGridw][];
            EC_BoidLogic.leaderGrid = new HashSet<int>[boidGridw][];
            EC_PathFinding.cachedPaths = new List<Queue<Vector2>>[boidGridw][];
            for (int x = 0; x < boidGridw; x++)
            {
                EC_BoidLogic.boidGrid[x] = new List<int>[boidGridh];
                EC_BoidLogic.leaderGrid[x] = new HashSet<int>[boidGridh];
                EC_PathFinding.cachedPaths[x] = new List<Queue<Vector2>>[boidGridh];
                for (int y = 0; y < boidGridh; y++)
                {
                    EC_BoidLogic.boidGrid[x][y] = new List<int>() { };
                    EC_BoidLogic.leaderGrid[x][y] = new HashSet<int>();
                    EC_PathFinding.cachedPaths[x][y] = new List<Queue<Vector2>>();
                }
            }
            EC_BoidLogic.targetX = Map.tileMap.Length / 2;
            EC_BoidLogic.targetY = Map.tileMap[0].Length / 2;

            int spawnerUid = WalkerSpawner.CreateWalkerSpawner();


            starting = false;
            RendererTools.Start();
            ECSHandler.controllerThread.Start();


            HandleUI();
            


            RendererTools.Running = false;
            debugger.AddLog($"Shutting down renderer", WarningLevel.Info); 
            debugger.Dispose(true);
            ECSHandler.running = false;


            debugger.Dispose(true);
        }

        private static void HandleUI()
        {
            string input = "";
            while (input != "Q")
            {
                string rawInput = Console.ReadLine() ?? "";
                input = rawInput.ToUpperInvariant();
                debugger.AddLog($"User inputted: \"{rawInput}\"", WarningLevel.Debug);
                if (input == "SR") // Switch Render
                {
                    gridRender = !gridRender;
                    debugger.AddLog($"Switching renderer to {(gridRender ? "grid mode" : "normal mode")}");
                    continue;
                }
                else if (input == "PAUSE")
                {
                    debugger.AddLog(paused ? "Unpausing" : "Pausing");
                    paused = !paused;
                }
                else if (input == "H")
                {
                    debugger.AddLog(highlight ? "Unhighlighting" : "Highlighting");
                    highlight = !highlight;
                }
                else if (input == "SL")
                {
                    debugger.AddLog(showLeaders ? "Hiding Leaders" : "Showing Leaders");
                    showLeaders = !showLeaders;
                }
                else if (input == "RL")
                {
                    debugger.AddLog(renderLines ? "Hiding Grid Lines" : "Showing Grid Lines");
                    renderLines = !renderLines;
                }
                else if (input == "RR")
                {
                    debugger.AddLog(renderRandom ? "Disabling Render Random" : "Rendering Random");
                    renderRandom = !renderRandom;
                }
                else if (input == "SLR")
                {
                    debugger.AddLog(showLeadingReason ? "Disabling Show Leading Reason" : "Enabling Show Leading Reason");
                    showLeadingReason = !showLeadingReason;
                }
                else if (input == "REFRESH")
                {
                    refresh = true;
                    debugger.AddLog($"Refreshing!");
                }
                else if (input == "HELP")
                {
                    Console.WriteLine("Options:\n" +
                        "\nsr - switch renderer to or from grid rendering" +
                        "\npause - pause the movement of the boids" +
                        "\nh - highlight tiles" +
                        "\nsl - switch show leaders" +
                        "\nrl - switch rendering of grid lines" +
                        "\nrr - switch render random - renders a random boid" +
                        "\nslr - show leading reason (debugging)" +
                        "\nrefresh - refresh the screen");
                }
            }
        }














        static readonly float dt = 1f / 60f;
        static bool starting = true;


        






        public static bool gridRender = false;
        public static bool paused = false;
        public static bool highlight = false;

        public static bool showLeaders =
#if DEBUG
            true;
#else
            false;
#endif



        public static bool renderRandom = false;
        public static bool showLeadingReason = false;
        public static bool refresh = true;
        static IBoid? randomBoid = null;
        internal static bool renderLines = false;
        static Random random = new Random();

        
    }
}