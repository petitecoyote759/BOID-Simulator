using System.Numerics;
using System.Reflection;
using SimpleGraphicsLib;



namespace BOIDSimulator
{
    public static class General
    {
        static GraphicsHandler renderer = null;
        static List<Boid>[,] boidGrid = new List<Boid>[0,0];
        static List<Boid> allBoids = new List<Boid>();
        const int boidGridSize = 100;
        const int boids = 100;

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

                int boidGridw = (renderer.screenwidth / boidGridSize) + 1;
                int boidGridh = (renderer.screenheight / boidGridSize) + 1;

                Console.WriteLine($"Width of {boidGridw}x{boidGridh}");
                boidGrid = new List<Boid>[boidGridw, boidGridh];
                for (int x = 0; x < boidGridw; x++)
                {
                    for (int y = 0; y < boidGridh; y++)
                    {
                        boidGrid[x, y] = new List<Boid>() { };
                        //Console.WriteLine($"creating new boid at ({x * boidGridSize}, {y * boidGridSize})");
                    }
                }

                for (int i = 0; i < boids; i++)
                {
                    Boid boid = new Boid(random.Next(renderer.screenwidth), random.Next(renderer.screenheight));
                    boidGrid[0, 0].Add(boid);
                    allBoids.Add(boid);
                }



                renderer.Resume();

                Console.ReadLine();
            }
        }




        const int boidSize = 2;
        static readonly float dt = 1f / 60f;
        private static void Render()
        {
            if (boidGrid is null) { Console.WriteLine("Breaking"); return; }
            
            renderer.SetPixel(0, 0, renderer.screenwidth, renderer.screenheight, 0, 0, 0);

            foreach (Boid boid in allBoids)
            {
                boid.Action(boidGrid, boidGridSize, dt);

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