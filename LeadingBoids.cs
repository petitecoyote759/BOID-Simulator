using ShortTools.AStar;
using ShortTools.MagicContainer;
using System.Numerics;


namespace BOIDSimulator
{
    public class LeadingBoid : IBoid, ILeadable
    {
        public Vector2 position { get; set; }
        public Vector2 velocity { get; set; }
        public bool leader = false;
        public bool Leader { get => leader; }

        
        
        const float maxSpeedSquared = maxSpeed * maxSpeed;
        const float maxSpeed = 50f;
        const float variation = 0.05f;
        
        const int blockChecks = 6;
        public const int killZone = 50 * 50;

        const int leaderDensity = 1;


        const float leaderAttraction = 20f; // how many boids it counts as
        const float minDistance = 20f;
        const float seperationConst = 0.04f;
        const float coherenceConst = 0.05f;
        const float alignmentConst = 0.01f;
        const float maxDistance = 300f;
        const float maxDistanceSquared = maxDistance * maxDistance;





        int nearbyLeaders = 0;

        public LeadingBoid(float x, float y)
        {
            position = new Vector2(x, y);
        }

        public void Action(List<IBoid>[][] boidGrid, int gridSize, float dt)
        {
            
            nearbyLeaders = 0;

            int width = boidGrid.GetLength(0);
            int height = boidGrid.GetLength(1);

            position = new Vector2(
                float.Clamp(position.X, 0, General.map.GetLength(0) - 1),
                float.Clamp(position.Y, 0, General.map.GetLength(1) - 1)
            );



            if (General.Walkable(General.map[(int)(position.X)][(int)(position.Y)]) == false && !leader)
            {
                while (true)
                {
                    int x = random.Next(0, (width - 1) * General.boidGridSize);
                    int y = random.Next(0, (height - 1) * General.boidGridSize);

                    if (General.Walkable(General.map[x][y]) == true) { position = new Vector2(x, y); return; }
                }
            }







            if (float.IsNaN(position.X) || float.IsNaN(position.Y))
            {
                Random random = new Random();
                position = new Vector2(random.Next(width * gridSize), random.Next(height * gridSize));
            }

            int gridX = (int)(position.X / gridSize);
            int gridY = (int)(position.Y / gridSize);


            if (leader)
            {
                RunLeader(gridX, gridY, width, height, boidGrid);
            }
            else
            {




                averagePosition = new Vector2();
                nearbyBoids = 0;
                // Coherence and Seperation
                RunBoidActions(boidGrid[gridX][gridY], dt, true);


                if (gridX < width - 1)
                {
                    RunBoidActions(boidGrid[gridX + 1][gridY], dt);
                    if (gridY > 0)
                    {
                        RunBoidActions(boidGrid[gridX + 1][gridY - 1], dt);
                    }
                    else if (gridY < height - 1)
                    {
                        RunBoidActions(boidGrid[gridX + 1][gridY + 1], dt);
                    }
                }
                if (gridX > 0)
                {
                    RunBoidActions(boidGrid[gridX - 1][gridY], dt);
                    if (gridY > 0)
                    {
                        RunBoidActions(boidGrid[gridX - 1][gridY - 1], dt);
                    }
                    else if (gridY < height - 1)
                    {
                        RunBoidActions(boidGrid[gridX - 1][gridY + 1], dt);
                    }
                }
                if (gridY < height - 1)
                {
                    RunBoidActions(boidGrid[gridX][gridY + 1], dt);
                }
                if (gridY > 0)
                {
                    RunBoidActions(boidGrid[gridX][gridY - 1], dt);
                }



                // do cohesion stuff
                if (nearbyBoids > 0)
                {
                    velocity += coherenceConst * ((averagePosition / nearbyBoids) - position);
                }
            }

            

            position += dt * velocity;

            int oldGridX = gridX;
            int oldGridY = gridY;

            gridX = (int)(position.X / gridSize);
            gridY = (int)(position.Y / gridSize);

            if (oldGridX != gridX || oldGridY != gridY)
            {
                lock (boidGrid[oldGridX][oldGridY])
                {
                    boidGrid[oldGridX][oldGridY].Remove(this);
                    boidGrid[gridX][gridY].Add(this);
                }
            }

            CountLeaders(gridX, gridY, width, height, boidGrid);
            if (nearbyLeaders < leaderDensity) { leader = true; }
            if (nearbyLeaders > 2 * leaderDensity) { leader = false; }

            if (
                ((position.X - (General.boidGridSize * width / 2)) * (position.X - (General.boidGridSize * width / 2))) + 
                ((position.Y - (General.boidGridSize * height / 2)) * (position.Y - (General.boidGridSize * height / 2))) < killZone) 
            {
                _ = boidGrid[gridX][gridY].Remove(this);
                _ = General.allBoids.Remove(this);
            }
        }








        Vector2 averagePosition = new Vector2();
        float nearbyBoids = 0;
        private void RunBoidActions(List<IBoid> boids, float dt, bool currentGrid = false)
        {
            Vector2 modifiedVelocity = new Vector2(velocity.Y, -velocity.X);

            lock (boids)
            {
                foreach (LeadingBoid boid in boids)
                {
                    if (currentGrid)
                    {
                        if (boid == this) { continue; }
                    }
                    Vector2 deltaPosition = boid.position - position;
                    if (deltaPosition.LengthSquared() > maxDistanceSquared) { continue; }



                    // coherance stuff, try to fly towards centre w/ leader boid counting as more
                    if (boid.leader)
                    {
                        averagePosition += boid.position * leaderAttraction;
                        nearbyBoids += leaderAttraction;
                    }
                    else
                    {
                        averagePosition += boid.position;
                        nearbyBoids++;
                    }


                    // seperation stuff
                    if (deltaPosition.Length() < minDistance)
                    {
                        velocity -= deltaPosition * seperationConst;
                    }




                    // Alignment
                    velocity += alignmentConst * (boid.velocity - velocity);



                    // <<Seperate from edges>> //
                    for (int x = -blockChecks / 2; x < blockChecks / 2; x++)
                    {
                        for (int y = -blockChecks / 2; y < blockChecks / 2; y++)
                        {
                            Vector2 target = new Vector2(position.X + x, position.Y + y);

                            if (target.X < 0 || target.X > (General.map.GetLength(0)) - 1) { continue; }
                            if (target.Y < 0 || target.Y > (General.map.GetLength(1)) - 1) { continue; }

                            if (General.Walkable(General.map[(int)(target.X)][(int)(target.Y)]) == false)
                            {
                                Vector2 push = position - target;
                                if (push.LengthSquared() > 0.001f)
                                {
                                    velocity += seperationConst * Vector2.Normalize(push);
                                }
                            }
                        }
                    }

                    
                }
            }


            //velocity = Vector2.Transform(velocity, Matrix3x2.CreateRotation(((float)random.NextDouble() * 2f - 1f) * variation)); // will have a far larger effect with noone else

            if (velocity.LengthSquared() > maxSpeedSquared)
            {
                velocity = (velocity / velocity.Length()) * maxSpeed;
            }
            else if (velocity.LengthSquared() < 0.5f)
            {
                velocity *= 1.2f;
            }
        }






        static Random random = new Random();
        static PathFinder? pather = null;
        Queue<Vector2>? path = null;
        const float leaderVel = maxSpeed / 4;
        const float pathDist = 50f;
        private void RunLeader(int gridX, int gridY, int width, int height, List<IBoid>[][] boidGrid)
        {
            if (pather is null)
            {
                pather = new PathFinder(
                    (int x, int y) =>
                    {
                        if (x < 0 || y < 0) return false;
                        if (x >= General.map.GetLength(0) ||
                            y >= General.map.GetLength(1)) return false;
                        return General.Walkable(General.map[x][y]); 
                    },
                    maxDist: 2000,
                    useDiagonals: false);
            }

            if (path is null || path.Count == 0)
            {
                int targetX = General.boidGridSize * width / 2;
                int targetY = General.boidGridSize * height / 2;

                //Console.WriteLine($"Path getting started from {position} to <{targetX},{targetY}>");
                path = pather.GetPath((int)position.X, (int)position.Y, targetX, targetY);
                //Console.WriteLine($"Path gotten! {path.Count()} long");
                if (path is null) 
                {
                    //Console.WriteLine("Cant path there, destroying self.");
                    _ = boidGrid[gridX][gridY].Remove(this);
                    return;
                }
            }
            if (path.Count == 0)
            {
                return;
                while (true)
                {
                    int x = random.Next(0, (width - 1) * General.boidGridSize);
                    int y = random.Next(0, (height - 1) * General.boidGridSize);

                    if (General.Walkable(General.map[x][y]) == true) { position = new Vector2(x, y); return; }
                }
            }

            Vector2 nextNode = path.Peek();

            if ((nextNode - position).Length() < 1f) { path.Dequeue(); }
            else
            {
                //Console.WriteLine($"Len {path.Count}, target {nextNode}, pos {position}");
                Vector2 deltaPosition = Vector2.Normalize(nextNode - position);

                velocity = leaderVel * deltaPosition;
            }
            
            //velocity += new Vector2(-1, 0);


            if (velocity.LengthSquared() > maxSpeedSquared / 4)
            {
                velocity = (velocity / velocity.Length()) * maxSpeed / 2;
            }
        }


        private void CountLeaders(int gridX, int gridY, int width, int height, List<IBoid>[][] boidGrid)
        {
            nearbyLeaders = 0;

            CountLeadersInGrid(boidGrid[gridX][gridY]);

            if (gridX < width - 1)
            {
                CountLeadersInGrid(boidGrid[gridX + 1][gridY]);
                if (gridY > 0)
                {
                    CountLeadersInGrid(boidGrid[gridX + 1][gridY - 1]);
                }
                else if (gridY < height - 1)
                {
                    CountLeadersInGrid(boidGrid[gridX + 1][gridY + 1]);
                }
            }
            if (gridX > 0)
            {
                CountLeadersInGrid(boidGrid[gridX - 1][gridY]);
                if (gridY > 0)
                {
                    CountLeadersInGrid(boidGrid[gridX - 1][gridY - 1]);
                }
                else if (gridY < height - 1)
                {
                    CountLeadersInGrid(boidGrid[gridX - 1][gridY + 1]);
                }
            }
            if (gridY < height - 1)
            {
                CountLeadersInGrid(boidGrid[gridX][gridY + 1]);
            }
            if (gridY > 0)
            {
                CountLeadersInGrid(boidGrid[gridX][gridY - 1]);
            }
        }
        private void CountLeadersInGrid(List<IBoid> boids)
        {
            foreach (IBoid boid in boids)
            {
                if (boid == this) { continue; }
                //Vector2 deltaPos = position - boid.position;
                //if (deltaPos.LengthSquared() > maxDistance) { continue; }
                if (boid is LeadingBoid lBoid && lBoid.leader)
                {
                    nearbyLeaders++;
                }
            }
        }














        public static string GetVectStr(Vector2 vect)
        {
            return $"({vect.X}, {vect.Y})";
        }
    }
}
