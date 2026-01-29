using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using ShortTools.Perlin;
using SimpleGraphicsLib;



namespace BOIDSimulator
{
    public static class General
    {
        static GraphicsHandler renderer = null;
        static List<IBoid>[,] boidGrid = new List<IBoid>[0,0];
        public static List<IBoid> allBoids = new List<IBoid>();
        public const int boidGridSize = 100;
        const int boids = 100;
        const bool leadingBoids = true;
        public static bool[,] map;

        public static void Main(string[] args)
        {
            Random random = new Random();

            using (renderer = new GraphicsHandler(1920, 1080,
               render: Render,
               flags: RendererFlag.OutputToTerminal))
            {
                Console.WriteLine("Starting Perlin Demo");
                Console.WriteLine($"Dimensions = {renderer.screenwidth}x{renderer.screenheight}");

                renderer.Pause();

                map = new bool[renderer.screenwidth, renderer.screenheight];
                float[,] perlinMap = Perlin.GeneratePerlinMap(renderer.screenwidth, renderer.screenheight, 16);
                for (int x = 0; x < renderer.screenwidth; x++)
                {
                    for (int y = 0; y < renderer.screenheight; y++)
                    {
                        if (x < 10 || x >= renderer.screenwidth - 10) { map[x, y] = false; continue; }
                        if (y < 10 || y >= renderer.screenheight - 10) { map[x, y] = false; continue; }

                        map[x, y] = perlinMap[x, y] < 0.5f;
                    }
                }



                int boidGridw = (renderer.screenwidth / boidGridSize) + 1;
                int boidGridh = (renderer.screenheight / boidGridSize) + 1;

                Console.WriteLine($"Width of {boidGridw}x{boidGridh}");
                boidGrid = new List<IBoid>[boidGridw, boidGridh];
                for (int x = 0; x < boidGridw; x++)
                {
                    for (int y = 0; y < boidGridh; y++)
                    {
                        boidGrid[x, y] = new List<IBoid>() { };
                        //Console.WriteLine($"creating new boid at ({x * boidGridSize}, {y * boidGridSize})");
                    }
                }

                for (int i = 0; i < boids; i++)
                {
                    IBoid boid;
                    if (leadingBoids)
                    {
                        boid = new LeadingBoid(random.Next(renderer.screenwidth), random.Next(renderer.screenheight));
                    }
                    else
                    {
                        boid = new Boid(random.Next(renderer.screenwidth), random.Next(renderer.screenheight));
                    }
                    boidGrid[0, 0].Add(boid);
                    allBoids.Add(boid);
                }
                Console.WriteLine("\n\n\n\nCOMPLETED LOOP\n\n\n\n\n");

                starting = false;
                renderer.Resume();

                Console.ReadLine();
            }
        }




        const int boidSize = 2;
        static readonly float dt = 1f / 60f;
        static bool starting = true;
        private static void Render()
        {
            if (starting) { return; }
            if (boidGrid is null) { Console.WriteLine("Breaking"); return; }

            int currentBoids = allBoids.Count;
            Random random = new Random();
            for (int i = 0; i < boids - currentBoids; i++)
            {
                allBoids.Add(new LeadingBoid(renderer.screenwidth - LeadingBoid.killZone, random.Next(LeadingBoid.killZone, renderer.screenheight - LeadingBoid.killZone)));
            }


            
            for (int x = 0; x < renderer.screenwidth; x++)
            {
                for (int y = 0; y < renderer.screenheight; y++)
                {
                    byte r = 0;
                    byte g = 0;
                    byte b = 0;

                    if (!map[x, y]) // not walkable
                    {
                        r = 50; g = 50; b = 50;
                    }

                    renderer.SetPixel(x, y, 1, 1, r, g, b);
                }
            }

            IBoid[] allBoidArray = allBoids.ToArray();
            foreach (IBoid boid in allBoidArray)
            {
                boid.Action(boidGrid, boidGridSize, dt);

                if (boid is LeadingBoid leaderBoid && leaderBoid.leader)
                {
                    renderer.SetPixel(
                        (int)(boid.position.X),
                        (int)(boid.position.Y),
                        boidSize * 2,
                        boidSize * 2,
                        150,
                        100,
                        255
                        );
                }
                else
                {
                    renderer.SetPixel(
                        (int)(boid.position.X),
                        (int)(boid.position.Y),
                        boidSize,
                        boidSize,
                        20,
                        200,
                        255
                        );
                }
            }
        }
    }
}