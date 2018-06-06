using System;
using System.Diagnostics;

namespace ProcessWatcher
{
    internal class SuperStopwatch : Stopwatch
    {
        public DateTime StartTime;
        public DateTime StopTime;

        public new void Start()
        {
            StartTime = DateTime.Now;
            base.Start();
        }

        public static new SuperStopwatch StartNew()
        {
            SuperStopwatch s = new SuperStopwatch();
            s.Start();
            return s;
        }

        public new void Stop()
        {
            base.Stop();
            StopTime = StartTime + Elapsed;
        }
    }
}