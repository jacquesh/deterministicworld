using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeterministicWorld
{
    class ReplayController
    {
        private dwWorld2D replayWorld;
        private Dictionary<uint, FrameInput> replayInput;

        public ReplayController(dwWorld2D world, Dictionary<uint,FrameInput> input)
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
                FrameInput input = replayInput[replayWorld.gameFrame];

                //Execute all orders/actions listed in input
                foreach (Order o in input.orderList)
                {
                    replayWorld.issueOrder(o.owner, o);
                }
            }
        }
    }
}
