using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BOIDSimulator.ECS_Components
{
    internal struct EC_Render : IEntityComponent
    {
        // <<Requirements>> //
        // EC_Entity //


        public double angle;
        public IntPtr image;

        public bool Active { get => active; set => active = value; }
        private bool active = true;


        public EC_Render(IntPtr image, double angle = 0d)
        {
            this.image = image;
            this.angle = angle;
        }

        public void Action(float dt, int uid)
        {

        }
    }
}
