using ShortTools.PlanetaryForge;
using ShortTools.General;
using ShortTools.MagicContainer;
using SimpleGraphicsLib;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Security.Cryptography;
using static ShortTools.General.Prints;
using Debugger = ShortTools.General.Debugger;
using BOIDSimulator.ECS_Components;
using System.Numerics;


namespace BOIDSimulator
{
    public static class General
    {
        public static GraphicsHandler renderer = null;
        static List<IBoid>[][] boidGrid = new List<IBoid>[0][];
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


        public const int PPT = 1; // pixels per tile

        public static void Main(string[] args)
        {
            Random random = new Random();
            debugger = new Debugger(DebuggerFlag.ShortDefault);
            debugger.DefaultLevel = WarningLevel.Info;

            using (renderer = new GraphicsHandler(1920, 1080,
               render: (Renderer.MainLoop),//Render,
               flags: RendererFlag.OutputToTerminal))
            {
                debugger.AddLog($"Starting Perlin Demo with {boids} boids");
                debugger.AddLog($"Dimensions = {renderer.screenwidth}x{renderer.screenheight}", WarningLevel.Debug);

                renderer.Pause();

                Map.CreateMap(renderer.screenwidth / PPT, renderer.screenheight / PPT);


                int boidGridw = (renderer.screenwidth / (boidGridSize * PPT)) + 1;
                int boidGridh = (renderer.screenheight / (boidGridSize * PPT)) + 1;

                debugger.AddLog($"Width of {boidGridw}x{boidGridh}", WarningLevel.Debug);
                boidGrid = new List<IBoid>[boidGridw][];
                for (int x = 0; x < boidGridw; x++)
                {
                    boidGrid[x] = new List<IBoid>[boidGridh];
                    for (int y = 0; y < boidGridh; y++)
                    {
                        boidGrid[x][y] = new List<IBoid>() { };
                        //Console.WriteLine($"creating new boid at ({x * boidGridSize}, {y * boidGridSize})");
                    }
                }
                EC_BoidLogic.targetX = Map.tileMap.Length / 2;
                EC_BoidLogic.targetY = Map.tileMap[0].Length / 2;

                AddBoids(random);


                starting = false;
                renderer.Resume();
                ECSHandler.controllerThread.Start();


                HandleUI();
            }
            Renderer.running = false;
            ECSHandler.running = false;


            debugger.Dispose();
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
                else if (input == "HELP")
                {
                    Console.WriteLine("Options:\n" +
                        "\nsr - switch renderer to or from grid rendering" +
                        "\npause - pause the movement of the boids" +
                        "\nh - highlight tiles" +
                        "\nsl - switch show leaders" +
                        "\nrl - switch rendering of grid lines" +
                        "\nrr - switch render random - renders a random boid");
                }
            }
        }














        private static void AddBoids(Random random)
        {
            for (int i = 0; i < boids; i++)
            {
                AddBoid();
            }
        }



        static readonly float dt = 1f / 60f;
        static bool starting = true;


        private static void AddBoid()
        {
            int position = random.Next(0, 4); // 0-3, going NESW

            int x = 0;
            int y = 0;

            if (position == 0) { y = (renderer.screenheight / PPT) - 1; x = random.Next(0, renderer.screenwidth / PPT); } // north
            if (position == 1) { y = random.Next(renderer.screenheight / PPT); x = (renderer.screenwidth / PPT) - 1; } // east
            if (position == 2) { y = 1; x = random.Next(0, renderer.screenwidth / PPT); } // south
            if (position == 3) { y = random.Next(0, renderer.screenheight / PPT); x = 1; } // west

            Walker.CreateWalker(new Vector2(x, y));
        }






        static bool gridRender = false;
        static bool paused = false;
        static bool highlight = false;
        static bool showLeaders = true;
        static bool renderRandom = false;
        static IBoid? randomBoid = null;
        internal static bool renderLines = false;
        static Random random = new Random();

        
    }
}