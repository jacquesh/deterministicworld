using System;
using System.Collections.Generic;

using DeterministicWorld.Orders;

namespace DeterministicWorld
{
    public abstract partial class dwWorld2D
    {
        private dwFrameInput currentFrameInput;

        private Dictionary<uint, dwFrameInput> inputData;

        //Order handling
        public virtual void issueInputOrder(dwObject2D obj, dwOrder issuedOrder)
        {
            issuedOrder.owner = obj;

            currentFrameInput.addOrder(issuedOrder);
        }

        protected internal void issueOrder(dwObject2D obj, dwOrder issuedOrder)
        {
            obj.issueOrder(issuedOrder);
        }

        internal void addFrameInputData(dwFrameInput frameInput)
        {
            if (inputData.ContainsKey(frameInput.targetFrame))
            {
                inputData[frameInput.targetFrame].mergeFrom(frameInput);
            }
            else
                inputData[frameInput.targetFrame] = frameInput;
        }

        //Data accessors
        internal dwFrameInput getInputData()
        {
            return currentFrameInput;
        }

    }
}
