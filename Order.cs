using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

using Lidgren.Network;

namespace DeterministicWorld
{
    public enum TargetType
    {
        Instant,
        Point,
        Object,
    }

    public abstract class Order : dwISerializable
    {
        internal dwObject2D owner;

        public TargetType targetType;
        public dwVector2 targetPoint;
        public dwObject2D targetObject;

        internal void execute()
        {
            OnStart();
        }

        protected void complete()
        {
            OnComplete();
            owner.orderComplete();
        }

        public virtual void OnStart()
        {
        }

        public virtual void OnUpdate()
        {
        }

        public virtual void OnComplete()
        {
        }

        public virtual void serialize(NetOutgoingMessage outMsg)
        {
            //serialize owner
            //owner.serialize(outMsg);
            outMsg.Write((byte)targetType);

            switch (targetType)
            {
                case(TargetType.Instant):
                    break;

                case (TargetType.Point):
                    targetPoint.serialize(outMsg);
                    break;

                case(TargetType.Object):
                    targetObject.serialize(outMsg);
                    break;
            }
        }

        public virtual void deserialize(NetIncomingMessage inMsg)
        {
            //deserialize owner
            //owner.deserialize(inMsg)
            targetType = (TargetType)inMsg.ReadByte();

            switch (targetType)
            {
                case (TargetType.Instant):
                    break;

                case (TargetType.Point):
                    targetPoint = new dwVector2();
                    targetPoint.deserialize(inMsg);
                    break;

                case (TargetType.Object):
                    //targetObject = new dwObject2D();
                    //targetObject.deserialize(inMsg);
                    break;
            }
        }
    }
}
