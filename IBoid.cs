using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BOIDSimulator
{
    public interface IBoid
    {
        public Vector2 position { get; set; }
        public Vector2 velocity { get; set; }

        public void Action(List<IBoid>[,] boidGrid, int gridSize, float dt);
    }
}
