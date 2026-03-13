using BOIDSimulator.ECS_Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BOIDSimulator
{
    internal static class WalkerSpawner
    {
        public const float walkSpeed = 2f;
        public const int deletionRadius = 10;

        private const int deletionRadiusSquared = deletionRadius * deletionRadius;

        public static int CreateWalkerSpawner()
        {
            int uid = ECSHandler.GetUID();

            ECSHandler.entities[uid] = true;



            // <<Set Variables>> //
            EC_Entity Me = new EC_Entity();
            Me.position = new Vector2(0, 0);


            // <<EAF Creation>> //
            ECSHandler.ECSs[typeof(EC_Entity)][uid] = Me;
            ECSHandler.ECSs[typeof(EC_SpawnerLogic)][uid] = new EC_SpawnerLogic(() => CreateWalker(uid), 100);




            return uid;
        }


        private static Random random = new Random();
        private static int CreateWalker(int spawnerUid)
        {
            if (Map.tileMap is null) { return -1; }

            int position = random.Next(0, 4); // 0-3, going NESW

            int x = 0;
            int y = 0;

            if (position == 0) { y = Map.tileMap[0].Length - 1; x = random.Next(0, Map.tileMap.Length); } // north
            if (position == 1) { y = random.Next(Map.tileMap[0].Length); x = Map.tileMap.Length - 1; } // east
            if (position == 2) { y = 1; x = random.Next(0, Map.tileMap.Length); } // south
            if (position == 3) { y = random.Next(0, Map.tileMap[0].Length); x = 1; } // west

            int walkerUid = Walker.CreateWalker(new Vector2(x, y), spawnerUid);

            return walkerUid;
        }
    }
}
