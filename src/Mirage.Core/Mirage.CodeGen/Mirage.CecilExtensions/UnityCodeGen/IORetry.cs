using System;
using System.IO;
using System.Threading;

namespace Mirage.CodeGen
{
    public static class IORetry
    {
        public static void Retry(Action action, Action<IOException, int> handleError = null, int retryCount = 10, int waitMs = 1000)
        {
            while (retryCount > 0)
            {
                try
                {
                    action.Invoke();
                    return;
                }
                catch (IOException e)
                {
                    retryCount--;
                    if (retryCount == 0)
                        throw;

                    if (handleError != null)
                    {
                        handleError.Invoke(e, retryCount);
                    }
                    else
                    {
                        Console.WriteLine($"Caught IO Exception for {e}, trying {retryCount} more times");
                    }
                    Thread.Sleep(waitMs);
                }
            }

            throw new InvalidOperationException("Should never get here");
        }
        public static TResult Retry<TResult>(Func<TResult> func, Action<IOException, int> handleError = null, int retryCount = 10, int waitMs = 1000)
        {
            TResult result = default;
            Retry(new Action(() => result = func.Invoke()), handleError, retryCount, waitMs);
            return result;
        }
    }
}
