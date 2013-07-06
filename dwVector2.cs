using Lidgren.Network;

namespace DeterministicWorld
{
    public class dwVector2 : dwISerializable
    {
        public float x;
        public float y;

        public dwVector2() : this(0,0)
        {
        }

        public dwVector2(dwVector2 source) : this(source.x, source.y)
        {
        }

        public dwVector2(float xVal, float yVal)
        {
            x = xVal;
            y = yVal;
        }

        public static dwVector2 operator +(dwVector2 v1, dwVector2 v2)
        {
            return new dwVector2(v1.x + v2.x, v1.y + v2.y);
        }

        public static dwVector2 operator -(dwVector2 v1, dwVector2 v2)
        {
            return new dwVector2(v1.x - v2.x, v1.y - v2.y);
        }

        public static dwVector2 operator *(dwVector2 vec, float multiplier)
        {
            return new dwVector2(vec.x * multiplier, vec.y * multiplier);
        }

        public static dwVector2 operator /(dwVector2 vec, float multiplier)
        {
            return new dwVector2(vec.x / multiplier, vec.y / multiplier);
        }

        public float sqrMagnitude()
        {
            return x * x + y * y;
        }

        public float magnitude()
        {
            return (float)System.Math.Sqrt(x * x + y * y);
        }

        public dwVector2 normalized()
        {
            return new dwVector2(this/this.magnitude());
        }

        public void serialize(NetOutgoingMessage outMsg)
        {
            outMsg.Write(x);
            outMsg.Write(y);
        }

        public void deserialize(NetIncomingMessage inMsg)
        {
            x = inMsg.ReadFloat();
            y = inMsg.ReadFloat();
        }
    }
}
