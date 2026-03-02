using ShortTools.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BOIDSimulator.ECS_Components
{
    internal struct EC_Render : IEntityComponent
    {
        // <<Requirements>> //
        // EC_Entity //


        // <<Public Variables> //
        public double angle;
        public IntPtr image;

        public bool Active { get => active; set => active = value; }
        private bool active = true;


        // <<Private Variables>> //
        private int prevGridX = 0;
        private int prevGridY = 0;




        public EC_Render(IntPtr image, double angle = 0d)
        {
            this.image = image;
            this.angle = angle;
        }

        public void Action(float dt, int uid)
        {
            EC_Entity? Me = (EC_Entity?)ECSHandler.ECSs[typeof(EC_Entity)][uid];
            if (Me is null) { General.debugger.AddLog($"Error, entity {uid} has no entity data!", WarningLevel.Error); return; }

            int gridX = Me.Value.tileX / Renderer.drawGridSize;
            int gridY = Me.Value.tileY / Renderer.drawGridSize;

            if (gridX != prevGridX || gridY != prevGridY)
            {
                ECSHandler.updatedGrids.Add((prevGridX, prevGridY));
            }
            ECSHandler.updatedGrids.Add((gridX, gridY));
            Renderer.RequestEntityDraw(gridX, gridY, uid);
        }
    }
}
