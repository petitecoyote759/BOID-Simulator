using ShortTools.MagicContainer;
using SimpleGraphicsLib;
using System.ComponentModel.Design;
using static ShortTools.General.Prints;


namespace BOIDSimulator
{
    public static class General
    {
        static GraphicsHandler renderer = null;
        static SMContainer<IBoid>[][] boidGrid = new SMContainer<IBoid>[0][];
        public static SMContainer<IBoid> allBoids = new SMContainer<IBoid>();
        public const int boidGridSize = 32;
        const int boids = 10;
        const bool leadingBoids = true;
        public static TileID[][] map;






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


        public const int PPT = 2; // pixels per tile

        public static void Main(string[] args)
        {
            Random random = new Random();

            using (renderer = new GraphicsHandler(1920, 1080,
               render: (() => { MapRenderer.Render(renderer, map); RenderBoids(); }),//Render,
               flags: RendererFlag.OutputToTerminal))
            {
                Console.WriteLine("Starting Perlin Demo");
                Console.WriteLine($"Dimensions = {renderer.screenwidth}x{renderer.screenheight}");

                renderer.Pause();

                map = MapRenderer.CreateMap(renderer.screenwidth / PPT, renderer.screenheight / PPT);



                int boidGridw = (renderer.screenwidth / (boidGridSize * PPT)) + 1;
                int boidGridh = (renderer.screenheight / (boidGridSize * PPT)) + 1;

                Console.WriteLine($"Width of {boidGridw}x{boidGridh}");
                boidGrid = new SMContainer<IBoid>[boidGridw][];
                for (int x = 0; x < boidGridw; x++)
                {
                    boidGrid[x] = new SMContainer<IBoid>[boidGridh];
                    for (int y = 0; y < boidGridh; y++)
                    {
                        boidGrid[x][y] = new SMContainer<IBoid>() { };
                        //Console.WriteLine($"creating new boid at ({x * boidGridSize}, {y * boidGridSize})");
                    }
                }

                AddBoids(random);


                starting = false;
                renderer.Resume();

                string input = "";
                while (input != "Q")
                {
                    input = Console.ReadLine()?.ToUpperInvariant() ?? "";
                    if (input == "RESET")
                    {
                        Console.WriteLine("Resetting");
                        for (int x = 0; x < boidGridw; x++)
                        {
                            for (int y = 0; y < boidGridh; y++)
                            {
                                boidGrid[x][y] = new SMContainer<IBoid>();
                            }
                        }
                        allBoids = new SMContainer<IBoid>();
                        AddBoids(random);
                        continue;
                    }
                    else if (input == "SR") // Switch Render
                    {
                        gridRender = !gridRender;
                        Console.WriteLine($"Switching renderer to {(gridRender ? "grid mode" : "normal mode")}");
                        continue;
                    }
                    else if (input == "PAUSE")
                    {
                        Console.WriteLine(paused ? "Unpausing" : "Pausing");
                        paused = !paused;
                    }
                    else if (input == "H")
                    {
                        Console.WriteLine(highlight ? "Unhighlighting" : "Highlighting");
                        highlight = !highlight;
                    }
                    else if (input == "HELP")
                    {
                        Console.WriteLine("Options:\n\nReset - resets the sim" +
                            "\nsr - switch renderer to or from grid rendering" +
                            "\npause - pause the movement of the boids" +
                            "\nh - highlight tiles");
                    }
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




        const int boidSize = 2;
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

            IBoid boid;
            if (leadingBoids)
            {
                boid = new NaturioBoid(x, y);
            }
            else
            {
                boid = new Boid(x, y);
            }

            int boidGridIndex = boidGrid[(int)(boid.position.X / boidGridSize)][(int)(boid.position.Y / boidGridSize)].Add(boid);
            int allBoidsIndex = allBoids.Add(boid);

            if (boid is NaturioBoid nBoid)
            {
                nBoid.SetIndexes(allBoidsIndex, boidGridIndex);
            }
        }






        static bool gridRender = false;
        static bool paused = false;
        static bool highlight = true;
        static Random random = new Random();
        private static void RenderBoids()
        {
            if (boidGrid is null || map is null) { return; }

            int currentBoids = allBoids.Length;
            for (int i = 0; i < boids - currentBoids; i++)
            {
                AddBoid();
            }

            if (!paused)
            {
                for (int i = 0; i < allBoids.Length; i++)
                {
                    IBoid boid = allBoids[i];
                    if (boid is NaturioBoid nBoid) { nBoid.SetIndexes(allBoidsIndex: i); }

                    boid.Action(boidGrid, boidGridSize, dt);
                }
            }

            if (gridRender)
            {
                for (int x = 0; x < boidGrid.Length; x++)
                {
                    for (int y = 0; y < boidGrid[0].Length; y++)
                    {
                        foreach (IBoid boid in boidGrid[x][y])
                        {
                            RenderBoid(boid);
                        }
                    }
                }
            }
            else
            {
                foreach (IBoid boid in allBoids)
                {
                    RenderBoid(boid);
                }
            }
        }


        private static void RenderBoid(IBoid boid)
        {
            if (highlight && boid is NaturioBoid nBoid)
            {
                renderer.SetPixel(
                    nBoid.gridX * boidGridSize * PPT,
                    nBoid.gridY * boidGridSize * PPT,
                    boidGridSize * PPT,
                    boidGridSize * PPT,
                    0,
                    100,
                    200,
                    80
                    );
            }
            if (boid is ILeadable leaderBoid && leaderBoid.Leader)
            {
                renderer.SetPixel(
                    (int)((boid.position.X - (boidSize * 2)) * PPT),
                    (int)((boid.position.Y - (boidSize * 2)) * PPT),
                    boidSize * 4 * PPT,
                    boidSize * 4 * PPT,
                    150,
                    100,
                    255
                    );
            }
            else
            {
                renderer.SetPixel(
                    (int)(boid.position.X * PPT),
                    (int)(boid.position.Y * PPT),
                    boidSize * PPT,
                    boidSize * PPT,
                    255,
                    200,
                    100
                    );
            }
        }
    }
}