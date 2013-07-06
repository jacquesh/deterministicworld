using System.Collections.Generic;

using DeterministicWorld.Orders;

namespace DeterministicWorld.Util
{
    class dwReplayController
    {
        private dwWorld2D replayWorld;
        private Dictionary<uint, dwFrameInput> replayInput;

        public dwReplayController(dwWorld2D world, Dictionary<uint,dwFrameInput> input)
        {
            replayWorld = world;
            replayInput = input;
        }

        public void start()
        {
            replayWorld.startSimulation();
        }

        //Get this to run somehow
        public void update()
        {
            if (replayInput.ContainsKey(replayWorld.gameFrame))
            {
                dwFrameInput input = replayInput[replayWorld.gameFrame];

                //Execute all orders/actions listed in input
                foreach (dwOrder o in input.orderList)
                {
                }
            }
        }
    }
}
