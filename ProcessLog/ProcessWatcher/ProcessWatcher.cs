using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ProcessWatcher
{
    public class ProcessWatcher
    {
        private HashSet<Tuple<string, int>> loggedProcesses = new HashSet<Tuple<string, int>>();

        private ProcessWatcherFactory Factory = ProcessWatcherFactory.Instance;

        private List<Tuple<Tuple<string, int>, double>> initialProcesses;

        private string Name { get; }

        private class KeyPercentTupleComparer : IEqualityComparer<Tuple<Tuple<string, int>, double>>
        {
            public bool Equals(Tuple<Tuple<string, int>, double> x, Tuple<Tuple<string, int>, double> y)
            {
                if (ReferenceEquals(x, y)) return true;

                return x.Item1.Item1 + x.Item1.Item2 == y.Item1.Item1 + y.Item1.Item2;
            }

            public int GetHashCode(Tuple<Tuple<string, int>, double> obj)
            {
                return obj.Item1.GetHashCode();
            }
        }

        private List<Tuple<Tuple<string, int>, double>> GetProcessesAsKeyPercentTuples(Process[] processes, DateTime stamp)
        {
            var tuplesList = new List<Tuple<Tuple<string, int>, double>>();

            foreach (var process in processes)
            {
                if (process.Id > 0 && (Name == "%" || Name != "%" && process.ProcessName.IndexOf(Name, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    tuplesList.Add(Tuple.Create(Tuple.Create(process.ProcessName, process.Id), process.TotalProcessorTime.TotalMilliseconds / (stamp - process.StartTime).TotalMilliseconds / Environment.ProcessorCount));
                }
            }

            return tuplesList;
        }

        /// <summary>
        /// Create a new ProcessWatcher with the given name.
        /// </summary>
        /// <param name="name"></param>
        internal ProcessWatcher(string name)
        {
            Name = name;
            initialProcesses = GetProcessesAsKeyPercentTuples(Process.GetProcesses(), DateTime.Now);
        }

        /// <summary>
        /// Update watched processes. Added processes fire LogStartEvent of the factory. Removed processes fire LogEndEvent of the factory.
        /// </summary>
        /// <param name="allProcesses"></param>
        internal void Update(Process[] allProcesses)
        {
            var currentProcesses = GetProcessesAsKeyPercentTuples(allProcesses, DateTime.Now);
            
            var addedProcesses = currentProcesses.Except(initialProcesses, new KeyPercentTupleComparer());
            var removedProcesses = initialProcesses.Except(currentProcesses, new KeyPercentTupleComparer());

            initialProcesses = currentProcesses;

            foreach (var process in addedProcesses)
            {
                if (!loggedProcesses.Contains(process.Item1))
                {
                    loggedProcesses.Add(process.Item1);
                    Factory.LogStartEvent(process);
                }
            }

            foreach (var process in removedProcesses)
            {
                if (loggedProcesses.Contains(process.Item1))
                {
                    loggedProcesses.Remove(process.Item1);
                    Factory.LogEndEvent(process);
                }
            }

        }

        /// <summary>
        /// Destory ProcessWatcher cleanup. May be unnecessary.
        /// </summary>
        internal void Destroy()
        {
            loggedProcesses.Clear();
        }
    }
}