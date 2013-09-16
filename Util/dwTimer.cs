using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeterministicWorld.Util
{
    internal class TimerData
    {
        public uint startFrame;
        public uint delay;
        public bool recurring;

        public event Action onCallback;

        public void callback()
        {
            if (onCallback != null)
            {
                onCallback();
            }
        }
    }

    /// <summary>
    /// The timer class allows for the running of actions with a specified delay
    /// and/or at some predefined interval. One instance of this class is created by the world
    /// and should generally not be instantiated otherwise. Rather use the wrappers 
    /// provided by the world
    /// </summary>
    internal class dwTimer
    {
        private List<TimerData> timerList;

        public dwTimer()
        {
            timerList = new List<TimerData>();
        }

        public void createTimer(uint startFrame, uint intervalDelay, bool recurring, Action callback)
        {
            TimerData newTimerData = new TimerData();
            newTimerData.startFrame = startFrame;
            newTimerData.delay = intervalDelay;
            newTimerData.recurring = recurring;
            newTimerData.onCallback += callback;

            timerList.Add(newTimerData);

            dwLog.debug("Created new timer, we now have " + timerList.Count);
        }

        internal void update()
        {
            for(int i=0; i<timerList.Count; i++)
            {
                TimerData timer = timerList[i];
                if (timer.startFrame + timer.delay == dwWorld2D.instance.gameFrame)
                {
                    timer.callback();

                    if (timer.recurring)
                    {
                        timer.startFrame = dwWorld2D.instance.gameFrame;
                    }
                    else
                    {
                        timerList.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

    }
}
