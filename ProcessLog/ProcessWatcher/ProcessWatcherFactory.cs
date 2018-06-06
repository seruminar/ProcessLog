using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;

namespace ProcessWatcher
{
    public class ProcessWatcherFactory
    {
        private const string timeFormat = "hh':'mm':'ss";

        private string saveSuffix;

        private Timer update;

        private Stopwatch TotalTime = new Stopwatch();
        private HashSet<ProcessWatcher> Watchers = new HashSet<ProcessWatcher>();
        private Dictionary<string, SuperStopwatch> WatchedProcesses = new Dictionary<string, SuperStopwatch>();
        private Dictionary<string, TimeSpan> Durations = new Dictionary<string, TimeSpan>();

        public static ProcessWatcherFactory Instance { get; } = new ProcessWatcherFactory();

        public HashSet<string> Queries = new HashSet<string>();

        private HashSet<string> Log = new HashSet<string>();

        public event EventHandler<ProcessWatcherEventArgs> EntryLogged;

        private TimeSpan Interval {
            get
            {
                foreach (var arg in Environment.GetCommandLineArgs())
                {
                    if (arg.StartsWith("interval") && Double.TryParse(arg.Split('=')[1], out double interval))
                        return TimeSpan.FromSeconds(interval);
                }

                return TimeSpan.FromSeconds(1);
            }
        }

        public string SaveSuffix
        {
            get
            {
                if (Watchers.Any())
                {
                    return String.Join("_", Queries);
                }

                return saveSuffix;
            }
            private set
            {
                saveSuffix = value;
            }
        }


        public ProcessWatcher New(string name)
        {
            if (update == null)
            {
                update = new Timer(Interval.TotalMilliseconds);
                update.Elapsed += Update;
                update.Enabled = true;
            }

            if (Queries.Add(name))
            {
                ProcessWatcher pw = new ProcessWatcher(name);
                Watchers.Add(pw);
                return pw;
            }

            return null;
        }

        private void Update(object sender, ElapsedEventArgs e)
        {
            Watchers.ToList().ForEach(w => w.Update());

            var tempDurations = new Dictionary<string, TimeSpan>(Durations);

            // This is necessary to avoid two or more processes with the same name adding to duration twice
            var tempUpdatedTempDurations = new HashSet<string>();

            foreach (var proc in WatchedProcesses)
            {
                string name = proc.Key.Substring(0, proc.Key.IndexOf('|'));

                if (tempDurations.ContainsKey(name) && !tempUpdatedTempDurations.Contains(name))
                {
                    tempDurations[name] += proc.Value.Elapsed;
                    tempUpdatedTempDurations.Add(name);
                }
            }

            if (WatchedProcesses.Count > 0 && !TotalTime.IsRunning)
            {
                TotalTime.Start();
            }
            else if (WatchedProcesses.Count == 0 && TotalTime.IsRunning)
            {
                TotalTime.Stop();
            }

            var durationsList = new HashSet<Tuple<bool, string>>();

            foreach (var duration in tempDurations.OrderByDescending(d => d.Value))
            {
                durationsList.Add(new Tuple<bool, string>(WatchedProcesses.Any(p => p.Key.StartsWith(duration.Key)),
                                                 $"{duration.Value.ToString(timeFormat)}|{duration.Key}"));
            }

            var logEntry = new ProcessWatcherEventArgs()
            {
                Output = String.Join(String.Empty, Log),
                Durations = durationsList,
                Watching = WatchedProcesses.Count,
                TotalTime = TotalTime.Elapsed.ToString(timeFormat)
            };

            EntryLogged?.Invoke(this, logEntry);

            tempUpdatedTempDurations.Clear();
            Log.Clear();
        }

        internal void LogStartEvent(string key)
        {
            if (!WatchedProcesses.ContainsKey(key))
            {
                string name = key.Substring(0, key.IndexOf('|'));

                WatchedProcesses[key] = SuperStopwatch.StartNew();

                if (!Durations.ContainsKey(name))
                {
                    Durations.Add(name, TimeSpan.Zero);
                }
            }
        }

        internal void LogEndEvent(string key)
        {
            if (WatchedProcesses.TryGetValue(key, out SuperStopwatch timer))
            {
                 var tempUpdatedDurations = new HashSet<string>();

                timer.Stop();

                WatchedProcesses.Remove(key);

                string name = key.Substring(0, key.IndexOf('|'));

                if (!tempUpdatedDurations.Contains(name))
                {
                    Durations[name] += timer.Elapsed;
                    tempUpdatedDurations.Add(name);
                }

                Log.Add($"{timer.StartTime.ToString(timeFormat)}|{timer.StopTime.ToString(timeFormat)}|{timer.Elapsed.ToString(timeFormat)}|{name}{Environment.NewLine}");

                tempUpdatedDurations.Clear();
            }
        }

        internal void SaveQueries()
        {
            SaveSuffix = String.Join("_", Queries);
        }

        public void DestroyAllWatchers()
        {
            Watchers.ToList().ForEach(w => w.Destroy());
            Watchers.Clear();
            WatchedProcesses.Clear();
            Queries.Clear();
            TotalTime.Reset();

            Update(null, null);
        }

    }
}
