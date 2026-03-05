using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Path = System.Collections.Generic.Queue<System.Numerics.Vector2>;
using ShortTools.General;   

namespace BOIDSimulator.ECS_Components
{
    internal partial struct EC_BoidLogic
    {
        private void LeaderAction(List<int>[][] boidGrid, int gridSize, float dt, int uid, ref EC_Entity Me)
        {
            EC_PathFinding? pathFindingNullable = (EC_PathFinding?)ECSHandler.ECSs[typeof(EC_PathFinding)][uid];
            if (pathFindingNullable is null) { General.debugger.AddLog($"Leader boid {uid} didnt have a pathfinding module!", WarningLevel.Error); return; }
            EC_PathFinding pathFinding = pathFindingNullable.Value;

            pathFinding.targetX = targetX;
            pathFinding.targetY = targetY;
            ECSHandler.ECSs[typeof(EC_PathFinding)][uid] = pathFinding;


            if (pathFinding.path is null) 
            { 
                ECSHandler.ECSs[typeof(EC_PathFinding)][uid].Active = true;  
                return;
            }
            else { ECSHandler.ECSs[typeof(EC_PathFinding)][uid].Active = false; }


            // Path is there and has items
            Vector2 node = pathFinding.path.Peek();
            if ((Me.position - node).LengthSquared() < leaderNodeMinDistanceSquared)
            {
                _ = pathFinding.path.Dequeue(); // remove the node, and with that done we can return.
                return;
            }
            // Now we know we are not close enough to it
            Vector2 step = Vector2.Normalize(node - Me.position) * leaderSpeed * dt; // the distance to step.

            if (step.LengthSquared() > (Me.position - node).LengthSquared()) // if stepping too far, just go to the node
            {
                Me.position = node;
            }
            else
            { // else take that step.
                Me.position += step;
            }

            // Update the pathfinding module
            ECSHandler.ECSs[typeof(EC_PathFinding)][uid] = pathFinding;
        }
    }
}
