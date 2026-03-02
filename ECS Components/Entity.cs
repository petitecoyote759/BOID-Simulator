using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BOIDSimulator.ECS_Components
{
    internal struct EC_Entity : IEntityComponent
    {
        public bool Active { get => active; set => active = value; }
        private bool active = false;

        // <<Variables>> //
        public Vector2 position = new Vector2(0, 0);

        public EC_Entity()
        {

        }

        public void Action(float dt, int uid) { }
    }
}
