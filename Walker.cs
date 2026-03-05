using BOIDSimulator.ECS_Components;
using ShortTools.General;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BOIDSimulator
{
    // Colloquially 'Haustors'

    internal static class Walker
    {
        public const float walkSpeed = 2f;
        public const int deletionRadius = 10;

        private const int deletionRadiusSquared = deletionRadius * deletionRadius;

        public static int CreateWalker(Vector2 position, int spawnerUid = -1)
        {
            int uid = ECSHandler.GetUID();

            ECSHandler.entities[uid] = true;



            // <<Set Variables>> //
            EC_Entity Me = new EC_Entity();
            Me.position = position;


            // <<EAF Creation>> //
            ECSHandler.ECSs[typeof(EC_Entity)][uid] = Me;
            ECSHandler.ECSs[typeof(EC_Despawning)][uid] = new EC_Despawning(deletionRadiusSquared);
            ECSHandler.ECSs[typeof(EC_BoidLogic)][uid] = new EC_BoidLogic();
            ECSHandler.ECSs[typeof(EC_PathFinding)][uid] = new EC_PathFinding(Walkable);
            ECSHandler.ECSs[typeof(EC_Render)][uid] = new EC_Render(IntPtr.Zero);
            ECSHandler.ECSs[typeof(EC_SpawnedLogic)][uid] = new EC_SpawnedLogic(spawnerUid);

            // <<Disable the EAF modules used for leaders>> //
#pragma warning disable CS8602 // This should absolutely not be null as it is assigned just a few lines up
            ECSHandler.ECSs[typeof(EC_PathFinding)][uid].Active = false;
#pragma warning restore CS8602



            return uid;
        }


        private static bool Walkable(int x, int y)
        {
            // <<Bounds Checks>> //
            if (Map.tileMap is null) { return false; }
            if (x < 0 || y < 0) { return false; }
            if (x >= Map.tileMap.Length) { return false; }
            if (y >= Map.tileMap[0].Length) { return false; }

            return General.Walkable(Map.tileMap[x][y]);
        }
    }
}
