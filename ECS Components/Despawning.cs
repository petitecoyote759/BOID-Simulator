using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BOIDSimulator.ECS_Components
{
    internal struct EC_Despawning : IEntityComponent
    {
        // <<Requirements>> //
        // EC_Entity //

        public float rangeSquared;

        public bool Active { get => active; set => active = value; }
        private bool active = true;

        public EC_Despawning(float rangeSquared)
        {
            this.rangeSquared = rangeSquared;
        }


        public readonly void Action(float dt, int uid)
        {
            // Delete at centre
        }
    }
}
