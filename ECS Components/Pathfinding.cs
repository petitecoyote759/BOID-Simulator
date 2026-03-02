using ShortTools.AStar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BOIDSimulator.ECS_Components
{
    internal struct EC_Pathfinding : IEntityComponent
    {
        // <<Requires>> //
        // EC_Entity //

        [ThreadStatic]
        public static PathFinder? pather;
        public Queue<Vector2>? path;

        public bool Active { get => active; set => active = value; }
        private bool active = true;

        public EC_Pathfinding()
        {
            if (pather is null)
            {
                pather = new PathFinder((x, y) => true, maxDist: 1000, useDiagonals: true);
            }

            path = null;
        }

        public readonly void Action(float dt, int uid)
        {
            // Pathfind, and set path if required
        }
    }
}
