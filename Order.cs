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
        protected internal dwObject2D owner;

        public TargetType targetType
        {
            get { return _targetType; }

            internal set
            {
                _targetType = value;
            }
        }

        public dwVector2 targetPoint
        {
            get { return _targetPoint; }

            set
            {
                _targetPoint = value;
                if (value == null)
                    _targetType = TargetType.Instant;
                else
                    _targetType = TargetType.Point;
            }
        }

        public dwObject2D targetObject
        {
            get { return _targetObject; }

            set
            {
                _targetObject = value;
                if (value == null)
                    _targetType = TargetType.Instant;
                else
                    _targetType = TargetType.Object;
            }
        }

        private TargetType _targetType;
        private dwVector2 _targetPoint;
        private dwObject2D _targetObject;

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
                    targetObject = dwObject2D.deserialize(inMsg);
                    break;
            }
        }
    }
}
