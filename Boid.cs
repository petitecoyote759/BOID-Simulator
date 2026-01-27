using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BOIDSimulator
{
    public class Boid
    {
        public Vector2 position;
        public Vector2 velocity;

        const float coseperationConst = 1f;
        const float optimalDist = 20f;
        const float maxSpeedSquared = maxSpeed * maxSpeed;
        const float maxSpeed = 20f;

        public Boid(float x, float y)
        {
            position = new Vector2(x, y);
        }

        public void Action(List<Boid>[,] boidGrid, int gridSize, float dt)
        {
            int gridX = (int)(position.X / gridSize);
            int gridY = (int)(position.Y / gridSize);

            int width = boidGrid.GetLength(0);
            int height = boidGrid.GetLength(1);

            // Coherence and Seperation
            RunCoherence(boidGrid[gridX, gridY], dt, true);

            if (gridX != width - 1)
            {
                RunCoherence(boidGrid[gridX + 1, gridY], dt);
            }
            if (gridX != 0)
            {
                RunCoherence(boidGrid[gridX - 1, gridY], dt);
            }
            if (gridY != height - 1)
            {
                RunCoherence(boidGrid[gridX, gridY + 1], dt);
            }
            if (gridY != 0)
            {
                RunCoherence(boidGrid[gridX, gridY - 1], dt);
            }

            position += dt * velocity;
            if (position.X < 0) { position.X = width * gridSize - 1f; }
            if (position.X > width * gridSize) { position.X = 0; }
            if (position.Y < 0) { position.Y = height * gridSize - 1f; }
            if (position.Y > height * gridSize) { position.Y = 0; }

            boidGrid[gridX, gridY].Remove(this);

            gridX = (int)(position.X / gridSize);
            gridY = (int)(position.Y / gridSize);

            boidGrid[gridX, gridY].Add(this);
        }



        private void RunCoherence(List<Boid> boids, float dt, bool currentGrid = false)
        {
            Vector2 modifiedVelocity = new Vector2(velocity.Y, -velocity.X);

            foreach (Boid boid in boids)
            {
                if (currentGrid)
                {
                    if (boid == this) { continue; }
                }

                // negative is to the left / counter clockwise

                /*
                int direction = MathF.Sign(Vector2.Dot(modifiedVelocity, position - boid.position));

                float angleModifier = direction * coseperationConst * MathF.Log2((position - boid.position).LengthSquared() / optimalDist);

                velocity = new Vector2(
                    (velocity.X * MathF.Cos(angleModifier)) - (velocity.Y * MathF.Sin(angleModifier)),
                    (velocity.X * MathF.Sin(angleModifier)) + (velocity.Y * MathF.Cos(angleModifier))
                    );
                */
                float distScale = 0f;
                if ((position - boid.position).LengthSquared() < 0.01f) 
                {
                    //Console.WriteLine("too close");
                    distScale = 1000f; 
                }
                else 
                { 
                    distScale = dt * coseperationConst * MathF.Log2((position - boid.position).LengthSquared() / optimalDist); 
                }

                velocity += distScale * (boid.position - position);
                //Console.WriteLine($"{distScale}, {(boid.position - position).Length()}, ({velocity.X}, {velocity.Y})");
            }


            if (velocity.LengthSquared() > maxSpeedSquared)
            {
                velocity = (velocity / velocity.Length()) * maxSpeed;
            }
        }

    }
}
