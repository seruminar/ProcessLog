using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ProcessWatcher
{
    public class ProcessWatcher
    {
        private HashSet<string> logged = new HashSet<string>();

        internal ProcessWatcherFactory Factory = ProcessWatcherFactory.Instance;
        private IEnumerable<string> initial;

        public string Name { get; }

        public ProcessWatcher(string name)
        {
            initial = Process.GetProcesses().Select(c => $"{c.ProcessName}|{c.Id.ToString()}");
            Name = name;
        }

        public void Update()
        {
            var current = Process.GetProcesses().Select(c => $"{c.ProcessName}|{c.Id.ToString()}");

            var added = current.Except(initial);
            var removed = initial.Except(current);

            initial = current;

            if (Name != "%")
            {
                added = added.Where(a => a.IndexOf(Name, StringComparison.OrdinalIgnoreCase) >= 0);
                removed = removed.Where(r => r.IndexOf(Name, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            foreach (var proc in added)
            {
                if (!logged.Contains(proc))
                {
                    logged.Add(proc);
                    Factory.LogStartEvent(proc);
                }
            }

            foreach (var proc in removed)
            {
                if (logged.Contains(proc))
                {
                    logged.Remove(proc);
                    Factory.LogEndEvent(proc);
                }
            }

        }

        internal void Destroy()
        {
            logged.Clear();
        }
    }
}