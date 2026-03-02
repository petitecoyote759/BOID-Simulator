using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BOIDSimulator.ECS_Components
{
    internal struct EC_BoidLogic : IEntityComponent
    {
        // <<Requirements>> //
        // EC_Entity //

        public Vector2 velocity = new Vector2(0, 0);
        public int? targetUid;
        public float followMinDuration;

        public bool Active { get => active; set => active = value; }
        private bool active = true;

        public EC_BoidLogic()
        {

        }

        public readonly void Action(float dt, int uid)
        {
            // Follow target for at least min duration.
        }
    }
}
