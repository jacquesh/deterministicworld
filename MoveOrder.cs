using System.Collections.Generic;

using Lidgren.Network;

namespace DeterministicWorld
{
    public class MoveOrder : Order
    {

        public MoveOrder()
        {
        }

        public MoveOrder(dwVector2 targetLoc)
        {
            targetPoint = targetLoc;
        }

        public override void OnStart()
        {
            //throw new NotImplementedException();
        }

        public override void OnUpdate()
        {
            dwVector2 offset = targetPoint - owner.position;
            dwVector2 moveOffset = offset.normalized() * 5;//(100 / 20f);

            if (offset.sqrMagnitude() < moveOffset.sqrMagnitude())
            {
                owner.position = targetPoint;
                complete();
            }
            else if (offset.sqrMagnitude() > 0)
            {
                owner.position += moveOffset;
            }
        }

        public override void OnComplete()
        {
            //throw new NotImplementedException();
        }
    }
}