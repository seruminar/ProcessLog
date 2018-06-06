using System;
using System.Collections.Generic;

namespace ProcessWatcher
{
    public class ProcessWatcherEventArgs : EventArgs
    {
        public string Output { get; internal set; }
        public HashSet<Tuple<bool, string>> Durations { get; internal set; }
        public int Watching { get; internal set; }
        public string TotalTime { get; internal set; }
    }
}
