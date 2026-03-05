using ShortTools.General;
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
        private bool active = true;

        // <<Variables>> //
        public Vector2 position = new Vector2(0, 0);
        public int tileX = 0;
        public int tileY = 0;

        public EC_Entity()
        {

        }

        public void Action(float dt, int uid) 
        {
            if (Map.tileMap is null) { return; }

            // <<Update Positions>> //

            // Bounds checks
            position.X = float.Clamp(position.X, 0, Map.tileMap.Length);
            position.Y = float.Clamp(position.Y, 0, Map.tileMap[0].Length);

            tileX = (int)position.X;
            tileY = (int)position.Y;


        }

        public void Cleanup(int uid) { }
    }
}
