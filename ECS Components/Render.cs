using ShortTools.General;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using BOIDSimulator.Renderer;
using System.Threading.Tasks;

namespace BOIDSimulator.ECS_Components
{
    internal struct EC_Render : IEntityComponent
    {
        // <<Requirements>> //
        // EC_Entity //


        // <<Public Variables> //
        public double angle;
        public IntPtr image => imageName.Length == 0 ? IntPtr.Zero : RendererTools.images[imageName];
        string imageName = "";
        public float width;
        public float height;

        public bool Active { get => active; set => active = value; }
        private bool active = true;


        // <<Private Variables>> //
        private int prevGridX = 0;
        private int prevGridY = 0;



        public EC_Render() { imageName = ""; }
        public EC_Render(string imageName, int width, int height)
        {
            this.imageName = imageName;
            this.width = width;
            this.height = height;
        }

        public void Action(float dt, int uid)
        {
            EC_Entity? Me = (EC_Entity?)ECSHandler.ECSs[typeof(EC_Entity)][uid];
            if (Me is null) { ECSHandler.debugger.AddLog($"Error, entity {uid} has no entity data!", WarningLevel.Error); return; }

            int gridX = Me.Value.tileX / RendererTools.drawGridTileSize;
            int gridY = Me.Value.tileY / RendererTools.drawGridTileSize;

            int gridImageSize = (int)MathF.Ceiling((width + 1) / (float)(2 * RendererTools.drawGridTileSize));
            for (int x = gridX - gridImageSize; x <= gridX + gridImageSize; x++)
            {
                for (int y = gridY - gridImageSize; y <= gridY + gridImageSize; y++)
                {
                    RendererTools.RequestDrawGrid(x, y);
                }
            }

            RendererTools.RequestEntityDraw(gridX, gridY, uid);
        }

        public void Cleanup(int uid)
        {
            EC_Entity? Me = (EC_Entity?)ECSHandler.ECSs[typeof(EC_Entity)][uid];
            if (Me is null) { ECSHandler.debugger.AddLog($"Error, entity {uid} has no entity data!", WarningLevel.Error); return; }

            int gridX = Me.Value.tileX / RendererTools.drawGridTileSize;
            int gridY = Me.Value.tileY / RendererTools.drawGridTileSize;

            lock (ECSHandler.updatedGrids)
            {
                RendererTools.RequestDrawGrid(gridX, gridY);
            }
        }
    }
}
