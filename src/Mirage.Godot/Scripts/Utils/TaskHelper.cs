using System;
using System.Threading.Tasks;
using Mirage.Logging;

namespace Mirage
{
    public static class TaskHelper
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(TaskHelper));
        public static async void Forget(this Task task)
        {
            try
            {
                await task;
            }
            catch (Exception e)
            {
                logger.LogException(e);
            }
        }
    }
}

