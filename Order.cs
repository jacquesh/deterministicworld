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
            //We do this extra write here because before we can deserialize the order, we need to get its type id
            outMsg.Write(OrderRegister.instance.orderToID(this.GetType()));

            owner.serialize(outMsg);
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
            owner = dwObject2D.deserialize(inMsg);
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
