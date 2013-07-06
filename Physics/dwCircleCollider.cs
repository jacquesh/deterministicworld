
namespace DeterministicWorld.Physics
{
    class dwCircleCollider : dwCollider2D
    {
        private int radius;

        public bool intersects(dwCircleCollider other)
        {
            return false;
        }
    }
}
