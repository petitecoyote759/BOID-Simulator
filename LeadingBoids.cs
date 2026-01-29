using Short_Tools;
using Silk.NET.SDL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;


namespace BOIDSimulator
{
    public class LeadingBoid : IBoid
    {
        public Vector2 position { get; set; }
        public Vector2 velocity { get; set; }
        public bool leader = false;

        
        
        const float maxSpeedSquared = maxSpeed * maxSpeed;
        const float maxSpeed = 50f;
        const float variation = 0.05f;
        
        const int blockChecks = 6;
        public const int killZone = 50;

        const int leaderDensity = 1;


        const float leaderAttraction = 20f; // how many boids it counts as
        const float minDistance = 20f;
        const float seperationConst = 0.1f;
        const float coherenceConst = 0.05f;
        const float maxDistance = 300f;
        const float maxDistanceSquared = maxDistance * maxDistance;





        int nearbyLeaders = 0;

        public LeadingBoid(float x, float y)
        {
            position = new Vector2(x, y);
        }

        public void Action(List<IBoid>[,] boidGrid, int gridSize, float dt)
        {
            
            nearbyLeaders = 0;

            int width = boidGrid.GetLength(0);
            int height = boidGrid.GetLength(1);

            if (position.X >= General.map.GetLength(0)) { position = new Vector2(General.map.GetLength(0) - 1, position.Y); }
            if (position.X < 0) { position = new Vector2(General.map.GetLength(0) - 1, position.Y); }
            if (position.Y >= General.map.GetLength(1)) { position = new Vector2(position.X, General.map.GetLength(1) - 1); }
            if (position.Y < 0) { position = new Vector2(position.X, General.map.GetLength(1) - 1); }



            if (General.map[(int)(position.X), (int)(position.Y)] == false && !leader)
            {
                while (true)
                {
                    int x = random.Next(0, (width - 1) * General.boidGridSize);
                    int y = random.Next(0, (height - 1) * General.boidGridSize);

                    if (General.map[x, y] == true) { position = new Vector2(x, y); return; }
                }
            }







            if (position.X == float.NaN || position.Y == float.NaN)
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
                RunBoidActions(boidGrid[gridX, gridY], dt, true);


                if (gridX < width - 1)
                {
                    RunBoidActions(boidGrid[gridX + 1, gridY], dt);
                    if (gridY > 0)
                    {
                        RunBoidActions(boidGrid[gridX + 1, gridY - 1], dt);
                    }
                    else if (gridY < height - 1)
                    {
                        RunBoidActions(boidGrid[gridX + 1, gridY + 1], dt);
                    }
                }
                if (gridX > 0)
                {
                    RunBoidActions(boidGrid[gridX - 1, gridY], dt);
                    if (gridY > 0)
                    {
                        RunBoidActions(boidGrid[gridX - 1, gridY - 1], dt);
                    }
                    else if (gridY < height - 1)
                    {
                        RunBoidActions(boidGrid[gridX - 1, gridY + 1], dt);
                    }
                }
                if (gridY < height - 1)
                {
                    RunBoidActions(boidGrid[gridX, gridY + 1], dt);
                }
                if (gridY > 0)
                {
                    RunBoidActions(boidGrid[gridX, gridY - 1], dt);
                }



                // do cohesion stuff
                if (nearbyBoids > 0)
                {
                    velocity += coherenceConst * (position - (averagePosition / nearbyBoids));
                }
            }

            

            position += dt * velocity;

            boidGrid[gridX, gridY].Remove(this);

            gridX = (int)(position.X / gridSize);
            gridY = (int)(position.Y / gridSize);

            boidGrid[gridX, gridY].Add(this);

            if (nearbyLeaders < leaderDensity) { leader = true; }
            if (nearbyLeaders > 2 * leaderDensity) { leader = false; }

            if (position.X < killZone) 
            {
                boidGrid[gridX, gridY].Remove(this);
                General.allBoids.Remove(this);
            }
        }








        Vector2 averagePosition = new Vector2();
        float nearbyBoids = 0;
        private void RunBoidActions(List<IBoid> boids, float dt, bool currentGrid = false)
        {
            Vector2 modifiedVelocity = new Vector2(velocity.Y, -velocity.X);

            foreach (LeadingBoid boid in boids)
            {
                if (currentGrid)
                {
                    if (boid == this) { continue; }
                }
                Vector2 deltaPosition = boid.position - position;
                if (deltaPosition.LengthSquared() > maxDistanceSquared) { continue; } 


                if (boid.leader)
                {
                    nearbyLeaders++;
                }
                



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





                for (int x = 0; x < blockChecks; x++)
                {
                    for (int y = 0; y < blockChecks; y++)
                    {
                        Vector2 target = new Vector2(position.X - x + (blockChecks / 2), position.Y - y + (blockChecks / 2));
                
                        if (target.X < 0 || target.X > (General.map.GetLength(0) * General.boidGridSize) - 1) { continue; }
                        if (target.Y < 0 || target.Y > (General.map.GetLength(1) * General.boidGridSize) - 1) { continue; }
                
                        if (General.map[(int)(target.X), (int)(target.Y)] == false)
                        {
                            velocity += seperationConst * Vector2.Normalize(position - target);
                        }
                    }
                }
            }


            velocity = Vector2.Transform(velocity, Matrix3x2.CreateRotation(((float)random.NextDouble() * 2f - 1f) * variation)); // will have a far larger effect with noone else

            if (velocity.LengthSquared() > maxSpeedSquared)
            {
                velocity = (velocity / velocity.Length()) * maxSpeed;
            }
            else if (velocity.LengthSquared() < 0.5f)
            {
                velocity *= 1.2f;
            }



        }
        Random random = new Random();








        Short_Tools.ST_Pather? pather = null;
        Queue<Vector2>? path = null;
        const float leaderVel = maxSpeed / 2;
        const float pathDist = 50f;
        private void RunLeader(int gridX, int gridY, int width, int height, List<IBoid>[,] boidGrid)
        {
            if (pather is null)
            {
                pather = new ST_Pather(
                    (int x, int y) =>
                    {
                        if (x < 0 || y < 0) { return false; }
                        if (x >= (width - 1) * General.boidGridSize || y >= (height - 1) * General.boidGridSize) { return false; }
                        return General.map[x, y]; 
                    }
                    , 2000);
            }

            if (path is null || path.Count == 0)
            {
                for (int i = 0; i < 100; i++)
                {
                    Vector2 target = new Vector2(MathF.Max(position.X - pathDist, 0), position.Y + i);
                    if (target.X < 0 || target.Y < 0) { continue; }
                    if (target.X >= (width - 1) * General.boidGridSize || target.Y >= (height - 1) * General.boidGridSize) { continue; }
                    if (General.map[(int)(target.X), (int)(target.Y)] == false) { continue; }
                    //Console.WriteLine($"Path getting started from {position} to {target}");
                    path = pather.GetPath(position, target);
                    //Console.WriteLine($"Path gotten! {path.Count()} long");
                    if (path.Count > 0) { break; }
                }
                if (path is null) { return; }
            }
            if (path.Count == 0)
            {
                return;
                while (true)
                {
                    int x = random.Next(0, (width - 1) * General.boidGridSize);
                    int y = random.Next(0, (height - 1) * General.boidGridSize);

                    if (General.map[x, y] == true) { position = new Vector2(x, y); return; }
                }
            }

            Vector2 nextNode = path.Peek();

            if ((nextNode - position).Length() < 0.5f) { path.Dequeue(); }
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

            CountLeaders(gridX, gridY, width, height, boidGrid);
        }


        private void CountLeaders(int gridX, int gridY, int width, int height, List<IBoid>[,] boidGrid)
        {
            if (gridX < width - 1)
            {
                CountLeadersInGrid(boidGrid[gridX + 1, gridY]);
                if (gridY > 0)
                {
                    CountLeadersInGrid(boidGrid[gridX + 1, gridY - 1]);
                }
                else if (gridY < height - 1)
                {
                    CountLeadersInGrid(boidGrid[gridX + 1, gridY + 1]);
                }
            }
            if (gridX > 0)
            {
                CountLeadersInGrid(boidGrid[gridX - 1, gridY]);
                if (gridY > 0)
                {
                    CountLeadersInGrid(boidGrid[gridX - 1, gridY - 1]);
                }
                else if (gridY < height - 1)
                {
                    CountLeadersInGrid(boidGrid[gridX - 1, gridY + 1]);
                }
            }
            if (gridY < height - 1)
            {
                CountLeadersInGrid(boidGrid[gridX, gridY + 1]);
            }
            if (gridY > 0)
            {
                CountLeadersInGrid(boidGrid[gridX, gridY - 1]);
            }
        }
        private void CountLeadersInGrid(List<IBoid> boids)
        {
            foreach (IBoid boid in boids)
            {
                Vector2 deltaPos = position - boid.position;
                if (deltaPos.LengthSquared() > maxDistance) { continue; }
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
