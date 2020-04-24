using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kurukuru
{
    public class BaseSpinner : IDisposable
    {
        private CancellationTokenSource _cancellationTokenSource;
        private Task _task;
        private Pattern _pattern;
        private Pattern _fallbackPattern;
        private int _frameIndex;
        private bool _enabled;

        public bool Stopped { get; private set; }
        public SymbolDefinition SymbolSucceed { get; set; } = new SymbolDefinition("✔", "O");
        public SymbolDefinition SymbolFailed { get; set; } = new SymbolDefinition("✖", "X");
        public SymbolDefinition SymbolWarn { get; set; } = new SymbolDefinition("⚠", "[!]");
        public SymbolDefinition SymbolInfo { get; set; } = new SymbolDefinition("ℹ", "[i]");

        public ConsoleColor? Color { get; set; }
        public string Text { get; set; }

        private static Pattern DefaultPattern
        {
            get
            {
                return ConsoleHelper.ShouldFallback
                    ? Patterns.Line
                    : Patterns.Dots;
            }
        }

        private Pattern CurrentPattern
        {
            get
            {
                return ConsoleHelper.ShouldFallback
                    ? _fallbackPattern
                    : _pattern;
            }
        }

        public BaseSpinner(string text, Pattern pattern = null, ConsoleColor? color = null, bool enabled = true, Pattern fallbackPattern = null)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _pattern = pattern ?? DefaultPattern;
            _fallbackPattern = fallbackPattern ?? DefaultPattern;
            _enabled = enabled && String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CI")) && !Console.IsOutputRedirected /* isatty */;

            Text = text;
            Color = color;
        }

        public void Start()
        {
            if (!_enabled) return;
            if (_task != null) throw new InvalidOperationException("Spinner is already running");

            ConsoleHelper.SetCursorVisibility(false);

            Stopped = false;

            _task = Task.Run(async () =>
            {
                _frameIndex = 0;

                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    Render();
                    await WaitNextFrame(_cancellationTokenSource.Token);
                }
            });
        }

        protected virtual async Task WaitNextFrame(CancellationToken token)
        {
            await Task.Delay(CurrentPattern.Interval, token);
        }

        private void Render()
        {
            if (Console.IsOutputRedirected)
                return;

            int currentLeft = Console.CursorLeft;
            Console.SetCursorPosition(0, Console.CursorTop);

            var pattern = CurrentPattern;
            var frame = pattern.Frames[_frameIndex++ % pattern.Frames.Length];
            ConsoleHelper.WriteWithColor(frame, Color ?? Console.ForegroundColor);
            Console.Write(" ");
            Console.Write(Text);

            if (Console.CursorLeft < currentLeft)
            {
                int newLeft = Console.CursorLeft;
                Console.Write(new string(' ', currentLeft - newLeft));
                Console.SetCursorPosition(newLeft, Console.CursorTop);
            }

            Console.Out.Flush();
        }

        public void Dispose()
        {
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                Stop(symbol: null, color: null);
            }
        }

        public void Stop(string text = null, string symbol = null, ConsoleColor? color = null)
        {
            Stop(text, symbol, color, Environment.NewLine);
        }

        public void Stop(string text, string symbol, ConsoleColor? color, string terminator)
        {
            if (_cancellationTokenSource.IsCancellationRequested) return;

            _cancellationTokenSource.Cancel();
            _task?.Wait();

            Color = color ?? Color;
            Text = text ?? Text;
            Stopped = true;

            _pattern = _fallbackPattern = new Pattern(new[] { symbol ?? " " }, 1000);
            Render();

            Console.Write(terminator);

            ConsoleHelper.SetCursorVisibility(true);
        }

        public void Succeed(string text = null)
        {
            Stop(text, ConsoleHelper.ShouldFallback ? SymbolSucceed.Fallback : SymbolSucceed.Default, ConsoleColor.Green);
        }

        public void Fail(string text = null)
        {
            Stop(text, ConsoleHelper.ShouldFallback ? SymbolFailed.Fallback : SymbolFailed.Default, ConsoleColor.Red);
        }

        public void Warn(string text = null)
        {
            Stop(text, ConsoleHelper.ShouldFallback ? SymbolWarn.Fallback : SymbolWarn.Default, ConsoleColor.Yellow);
        }

        public void Info(string text = null)
        {
            Stop(text, ConsoleHelper.ShouldFallback ? SymbolInfo.Fallback : SymbolInfo.Default, ConsoleColor.Blue);
        }
    }
}
