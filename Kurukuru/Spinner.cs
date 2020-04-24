using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kurukuru
{
    public class Spinner : BaseSpinner
    {
        public Spinner(string text, Pattern pattern = null, ConsoleColor? color = null, bool enabled = true, Pattern fallbackPattern = null)
            : base(text, pattern, color, enabled, fallbackPattern)
        {
        }

        protected override async Task WaitNextFrame(CancellationToken token)
        {
            try
            {
                await Task.Delay(CurrentPattern.Interval, token);
            }
            catch (OperationCanceledException) { }
        }

        public static void Start(string text, Action action, Pattern pattern = null, Pattern fallbackPattern = null)
            => Start(text, _ => action(), pattern, fallbackPattern);

        public static void Start(string text, Action<Spinner> action, Pattern pattern = null, Pattern fallbackPattern = null)
        {
            using (var spinner = new Spinner(text, pattern, fallbackPattern: fallbackPattern))
            {
                spinner.Start();

                try
                {
                    action(spinner);

                    if (!spinner.Stopped)
                    {
                        spinner.Succeed();
                    }
                }
                catch
                {
                    if (!spinner.Stopped)
                    {
                        spinner.Fail();
                    }
                    throw;
                }
            }
        }

        public static Task StartAsync(string text, Func<Task> action, Pattern pattern = null, Pattern fallbackPattern = null)
            => StartAsync(text, _ => action(), pattern, fallbackPattern);

        public static async Task StartAsync(string text, Func<Spinner, Task> action, Pattern pattern = null, Pattern fallbackPattern = null)
        {
            using (var spinner = new Spinner(text, pattern, fallbackPattern: fallbackPattern))
            {
                spinner.Start();

                try
                {
                    await action(spinner);
                    if (!spinner.Stopped)
                    {
                        spinner.Succeed();
                    }
                }
                catch
                {
                    if (!spinner.Stopped)
                    {
                        spinner.Fail();
                    }
                    throw;
                }
            }
        }
    }
}
