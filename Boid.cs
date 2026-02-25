using ShortTools.MagicContainer;
using System.Numerics;

namespace BOIDSimulator
{
    public class Boid : IBoid
    {
        public Vector2 position { get; set; }
        public Vector2 velocity { get; set; }

        const float coherenceConst = 0.03f;
        const float seperationConst = 0.1f;
        const float optimalDist = 30f;
        const float maxSpeedSquared = maxSpeed * maxSpeed;
        const float maxSpeed = 30f;
        const float alignConst = 0.000f;
        const float viewAngle = 1.5f; // rads
        const float variation = 0.05f;

        public Boid(float x, float y)
        {
            position = new Vector2(x, y);
        }

        public void Action(List<IBoid>[][] boidGrid, int gridSize, float dt)
        {

            int width = boidGrid.GetLength(0);
            int height = boidGrid.GetLength(1);



            if (position.X == float.NaN || position.Y == float.NaN)
            {
                Random random = new Random();
                position = new Vector2(random.Next(width * gridSize), random.Next(height * gridSize));
            }

            int gridX = (int)(position.X / gridSize);
            int gridY = (int)(position.Y / gridSize);


            // Coherence and Seperation
            RunCoherence(boidGrid[gridX][gridY], dt, true);


            if (gridX < width - 1)
            {
                RunCoherence(boidGrid[gridX + 1][gridY], dt);
                if (gridY > 0)
                {
                    RunCoherence(boidGrid[gridX + 1][gridY - 1], dt);
                }
                else if (gridY < height - 1)
                {
                    RunCoherence(boidGrid[gridX + 1][gridY + 1], dt);
                }
            }
            if (gridX > 0)
            {
                RunCoherence(boidGrid[gridX - 1][gridY], dt);
                if (gridY > 0)
                {
                    RunCoherence(boidGrid[gridX - 1][gridY - 1], dt);
                }
                else if (gridY < height - 1)
                {
                    RunCoherence(boidGrid[gridX - 1][gridY + 1], dt);
                }
            }
            if (gridY < height - 1)
            {
                RunCoherence(boidGrid[gridX][gridY + 1], dt);
            }
            if (gridY > 0)
            {
                RunCoherence(boidGrid[gridX][gridY - 1], dt);
            }

            position += dt * velocity;
            if (position.X < 0) { position = new Vector2((width - 1f) * gridSize, position.Y); }
            if (position.X > width * gridSize) { position = new Vector2(0, position.Y); }
            if (position.Y < 0) { position = new Vector2(position.X, (height -1f) * gridSize); }
            if (position.Y > height * gridSize) { position = new Vector2(0); }

            boidGrid[gridX][gridY].Remove(this);

            gridX = (int)(position.X / gridSize);
            gridY = (int)(position.Y / gridSize);

            boidGrid[gridX][gridY].Add(this);
        }



        private void RunCoherence(List<IBoid> boids, float dt, bool currentGrid = false)
        {
            Vector2 modifiedVelocity = new Vector2(velocity.Y, -velocity.X);

            foreach (IBoid boid in boids)
            {
                if (currentGrid)
                {
                    if (boid == this) { continue; }
                }


                Vector2 deltaPosition = Vector2.Normalize(boid.position - position);
                if (float.IsNaN(deltaPosition.X) || float.IsNaN(deltaPosition.Y)) 
                {
                    deltaPosition = Vector2.Normalize(boid.position - position + new Vector2(1, 1));
                }
                float dot = Vector2.Dot(deltaPosition, Vector2.Normalize(velocity));
                float angle = MathF.Acos(dot);
                if (angle > viewAngle)
                {
                    continue;
                }


                // Coherence

                velocity += coherenceConst * deltaPosition;
                //.WriteLine($"Coh {GetVectStr(coherenceConst * deltaPosition)}");


                // Seperation

                float seperationScale = seperationConst - ((seperationConst / optimalDist) * (boid.position - position).Length());

                velocity -= seperationScale * deltaPosition;



                // Alignment

                // linear
                //float alignScale = alignConst - ((alignConst / optimalDist) * (boid.position - position).Length());
                float alignScale = alignConst * MathF.Pow(((boid.position - position).Length() / optimalDist) - 1, 2);

                velocity = (velocity * (1 - alignScale)) + (boid.velocity * alignScale);
            }
            if (velocity.X == 0 && velocity.Y == 0) { velocity = new Vector2(0.1f); }

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
        




        public static string GetVectStr(Vector2 vect)
        {
            return $"({vect.X}, {vect.Y})";
        }
    }
}
