using ShortTools.MagicContainer;
using System.Numerics;

namespace BOIDSimulator
{
    public interface IBoid
    {
        public Vector2 position { get; set; }
        public Vector2 velocity { get; set; }

        public void Action(SMContainer<IBoid>[][] boidGrid, int gridSize, float dt);
    }


    public interface ILeadable
    {
        public bool Leader { get; }
    }
}
