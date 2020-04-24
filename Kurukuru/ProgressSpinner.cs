using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kurukuru
{
    public class ProgressSpinner<T> : BaseSpinner, IProgress<T>
    {
        private SemaphoreSlim ReportSemaphore = new SemaphoreSlim(0);

        public ProgressSpinner(string text, Pattern pattern = null, ConsoleColor? color = null, bool enabled = true, Pattern fallbackPattern = null)
            : base(text, pattern, color, enabled, fallbackPattern)
        {
        }

        public void Report(T value)
        {
            ReportSemaphore.Release();
        }

        protected override async Task WaitNextFrame(CancellationToken token)
        {
            try
            {
                await ReportSemaphore.WaitAsync(token);
            }
            catch (OperationCanceledException) { }
        }

        public static void Start(string text, Action action, Pattern pattern = null, Pattern fallbackPattern = null)
            => Start(text, _ => action(), pattern, fallbackPattern);

        public static void Start(string text, Action<ProgressSpinner<T>> action, Pattern pattern = null, Pattern fallbackPattern = null)
        {
            using (var spinner = new ProgressSpinner<T>(text, pattern, fallbackPattern: fallbackPattern))
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

        public static async Task StartAsync(string text, Func<ProgressSpinner<T>, Task> action, Pattern pattern = null, Pattern fallbackPattern = null)
        {
            using (var spinner = new ProgressSpinner<T>(text, pattern, fallbackPattern: fallbackPattern))
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
