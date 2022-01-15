using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryGunHand
{
    public static class TimeUtil
    {
        private static decimal StartTime;

        static TimeUtil()
        {
            StartTime = GetTimeInMs();
        }

        public static decimal GetTimeInMs()
        {
            return (decimal)DateTime.Now.Ticks / (decimal)TimeSpan.TicksPerMillisecond;
        }

        public static float GetTimeInMsF()
        {
            return (float)(GetTimeInMs() - StartTime);
        }
    }
}
