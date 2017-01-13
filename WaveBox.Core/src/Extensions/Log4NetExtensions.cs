using System;
using System.Text;

namespace WaveBox.Core.Extensions {
    public static class Log4NetExtensions {
        /// <summary>
        /// Log only if logger.IsInfoEnabled
        /// </summary>
        public static void IfInfo(this log4net.ILog logger, string message) {
            if (logger.IsInfoEnabled) { logger.Info(message); }

            return;
        }
    }
}
