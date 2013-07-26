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

    public class dwTimer
    {

        private static dwTimer _instance;

        public static dwTimer instance
        {
            get 
            {
                if (_instance == null)
                    _instance = new dwTimer();
                return _instance; 
            }
        }

        private List<TimerData> timerList;

        private dwTimer()
        {
            timerList = new List<TimerData>();
        }

        public void createTimer(uint delay, bool recurring, Action callback)
        {
            TimerData newTimerData = new TimerData();
            newTimerData.delay = delay;
            newTimerData.recurring = recurring;
            newTimerData.onCallback += callback;

            timerList.Add(newTimerData);
        }

        internal void update()
        {
            for(int i=0; i<timerList.Count; i++)
            {
                TimerData timer = timerList[i];
                if (timer.startFrame + timer.delay == dwWorld2D.instance.gameFrame)
                {
                    if (timer.recurring)
                    {
                        timer.startFrame = dwWorld2D.instance.gameFrame;
                    }
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
