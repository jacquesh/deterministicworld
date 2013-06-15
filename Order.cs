using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace DeterministicWorld
{
    public enum TargetType
    {
        Instant,
        Point,
        Object,
    }

    public class Order : System.Runtime.Serialization.ISerializable
    {
        internal dwObject2D owner;

        protected dwVector2 targetPoint;
        protected dwObject2D targetObject;

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

        //?????
        //See serialization tabs in firefox
        public void GetObjectData(SerializationInfo serialInfo, StreamingContext streamContext)
        {

        }
    }
}
