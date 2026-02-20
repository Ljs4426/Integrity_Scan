using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Hawkbat.Views
{
    public partial class ScriptTerminalWindow : Window
    {
        private readonly Process? _process;
        private readonly CancellationTokenSource _cts = new();
        private readonly StringBuilder _outputBuffer = new();

        public ScriptTerminalWindow(Process process)
        {
            InitializeComponent();
            _process = process;
            
            KeyDown += OnKeyDown;
            Loaded += OnLoaded;
            Closing += OnClosing;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_process == null) return;

            try
            {
                _process.OutputDataReceived += (s, args) =>
                {
                    if (args.Data != null)
                    {
                        AppendOutput(args.Data);
                    }
                };

                _process.ErrorDataReceived += (s, args) =>
                {
                    if (args.Data != null)
                    {
                        AppendOutput($"[ERROR] {args.Data}");
                    }
                };

                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();

                await Task.Run(() =>
                {
                    _process.WaitForExit();
                }, _cts.Token);

                Dispatcher.Invoke(() =>
                {
                    StatusText.Text = "Script completed. Press X to close.";
                    ProgressText.Text = $"Exit Code: {_process.ExitCode}";
                });
            }
            catch (OperationCanceledException)
            {
                // User closed window
            }
            catch (Exception ex)
            {
                AppendOutput($"[TERMINAL ERROR] {ex.Message}");
            }
        }

        private void AppendOutput(string line)
        {
            Dispatcher.Invoke(() =>
            {
                _outputBuffer.AppendLine(line);
                OutputText.Text = _outputBuffer.ToString();
                OutputScroller.ScrollToBottom();
            });
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Send blank line to process
                try
                {
                    _process?.StandardInput?.WriteLine();
                    AppendOutput(""); // Visual feedback
                }
                catch
                {
                    // Process may not accept input or already exited
                }
            }
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                _cts.Cancel();

                if (_process != null && !_process.HasExited)
                {
                    // Forcibly terminate the process
                    _process.Kill(true);
                }

                _process?.Dispose();
                _cts.Dispose();
            }
            catch
            {
                // Process already disposed or exited
            }
        }
    }
}
