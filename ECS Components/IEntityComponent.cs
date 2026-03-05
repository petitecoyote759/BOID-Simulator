using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BOIDSimulator.ECS_Components
{
    internal interface IEntityComponent
    {
        public bool Active { get; set; }

        public void Action(float dt, int uid);

        public void Cleanup(int uid);
    }
}
