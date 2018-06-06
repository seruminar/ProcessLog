using Microsoft.Win32;
using ProcessWatcher;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace ProcessLog
{
    /// <summary>
    /// Interaction logic
    /// </summary>
    public partial class ProcessLogWindow : Window
    {
        private ProcessWatcherFactory factory = ProcessWatcherFactory.Instance;

        private enum State
        {
            Start = 0,

            Stop = 1
        }

        public ProcessLogWindow()
        {
            // Lower priority
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;

            // Required for designer
            InitializeComponent();

            // Event binding
            StartButton.Click += (s, e) => SetState(State.Start);
            StopButton.Click += (s, e) => SetState(State.Stop);
            Closed += (s, e) => SetState(State.Stop);
            SaveLogButton.Click += (s, e) => SaveLog();

            QueryBox.GotFocus += (s, e) => ProcessBoxActions(s, e, "enter");
            QueryBox.TextChanged += (s, e) => ProcessBoxActions(s, e);
            QueryBox.KeyDown += SetStateByKey;

            factory.EntryLogged += DispatchLogEvent;
        }

        private void SetStateByKey(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (sender as TextBox).Text != String.Empty)
            {
                SetState(State.Start);
            }
        }

        private void SetState(State state)
        {
            switch (state)
            {
                case State.Start:
                    string queryName = QueryBox.Text;

                    try
                    {
                        if (factory.New(queryName) != null)
                        {
                            StartButton.IsEnabled = false;
                            QueryBox.Text = String.Empty;

                            QueriesBox.IsEnabled = true;
                        }
                    }
                    catch (Exception e)
                    {
                        DisplayLogInternal(e.ToString());
                    }
                    finally
                    {
                        StopButton.IsEnabled = true;
                        OutputBox.Background = SystemColors.ControlLightBrush;
                    }
                    break;
                case State.Stop:
                    try
                    {
                        factory.SaveQueries();
                        factory.DestroyAllWatchers();

                        QueriesBox.IsEnabled = false;
                    }
                    catch (Exception e)
                    {
                        DisplayLogInternal(e.ToString());
                    }
                    finally
                    {
                        StopButton.IsEnabled = false;
                        OutputBox.Background = SystemColors.ControlBrush;
                    }
                    break;
            }

            QueriesBox.Text = String.Join(Environment.NewLine, factory.Queries);
        }

        private void DispatchLogEvent(object sender, ProcessWatcherEventArgs e)
        {
            // Handle cross-threading with Invoke and Action
            Dispatcher.Invoke(new Action(() => DisplayLog(e)));
        }

        private void DisplayLog(ProcessWatcherEventArgs e)
        {
            DisplayLogInternal(e.Output);

            if (e.Durations.Count > 0)
            {
                DurationsBox.Inlines.Clear();

                foreach (var duration in e.Durations)
                {
                    var run = new Run(duration.Item2);

                    if (duration.Item1)
                    {
                        run.FontWeight = FontWeights.Bold;
                    }

                    DurationsBox.Inlines.Add(run);
                    DurationsBox.Inlines.Add(new LineBreak());
                }

                DurationsBox.IsEnabled = true;
            }

            WatchingBox.Content = e.Watching;
            TotalTimeBox.Content = e.TotalTime;
        }

        private void DisplayLogInternal(string entry)
        {
            if (!String.IsNullOrEmpty(entry))
            {
                OutputBox.Text += entry;
                OutputBox.IsEnabled = true;

                // Required for scroll to work
                if (OutputBox.IsFocused)
                {
                    Keyboard.ClearFocus();
                }

                OutputBox.ScrollToEnd();

                SaveLogButton.IsEnabled = true;
            }
        }

        #region Button methods

        private void SaveLog()
        {
            var saveFileDialog = new SaveFileDialog
            {
                FileName = $"{DateTime.Now.ToString("s").Replace(":", "")}-Process-Log-for-{factory.SaveSuffix}",
                DefaultExt = "log",
                Filter = "Log files|*.log|All (*.*)|*"
            };

            // Process save file dialog box results
            if (saveFileDialog.ShowDialog() == true)
            {
                // Save document
                File.WriteAllText(saveFileDialog.FileName, OutputBox.Text);
            }
        }

        private void ProcessBoxActions(object sender, EventArgs e, string state = null)
        {
            if (sender is TextBox tb)
            {
                StartButton.IsEnabled = false;

                if (!factory.Queries.Contains(tb.Text) && tb.Text != String.Empty)
                {
                    StartButton.IsEnabled = true;
                }
            }
        }

        #endregion
    }
}
