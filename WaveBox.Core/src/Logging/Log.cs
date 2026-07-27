using System;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace WaveBox.Core.Logging {
    // log4net-shaped logging facade over Microsoft.Extensions.Logging.
    // log4net 3.x instantiates appenders from XML config via reflection, which is not NativeAOT-safe;
    // this shim keeps the hundreds of existing logger.IfInfo/Warn/Error call sites unchanged.
    public interface ILog {
        bool IsDebugEnabled { get; }
        bool IsInfoEnabled { get; }
        bool IsWarnEnabled { get; }
        bool IsErrorEnabled { get; }
        void Debug(object message);
        void Debug(object message, Exception exception);
        void Info(object message);
        void Info(object message, Exception exception);
        void Warn(object message);
        void Warn(object message, Exception exception);
        void Error(object message);
        void Error(object message, Exception exception);
    }

    public static class LogManager {
        private static ILoggerFactory factory;

        // Called once at startup with the host's logger factory; loggers created before this
        // fall back to plain console output.
        public static void SetFactory(ILoggerFactory loggerFactory) {
            factory = loggerFactory;
        }

        public static ILog GetLogger(Type type) {
            return new MelLog(type.Name);
        }

        // Category defaults to the calling file's name, avoiding MethodBase.GetCurrentMethod()
        // which is unreliable under NativeAOT
        public static ILog GetLogger([CallerFilePath] string callerFilePath = "") {
            return new MelLog(Path.GetFileNameWithoutExtension(callerFilePath));
        }

        internal static Microsoft.Extensions.Logging.ILogger CreateLogger(string category) {
            ILoggerFactory current = factory;
            return current != null ? current.CreateLogger(category) : null;
        }
    }

    internal class MelLog : ILog {
        private readonly string category;
        private Microsoft.Extensions.Logging.ILogger logger;

        public MelLog(string category) {
            this.category = category;
        }

        private Microsoft.Extensions.Logging.ILogger Logger {
            get {
                if (logger == null) {
                    logger = LogManager.CreateLogger(category);
                }
                return logger;
            }
        }

        public bool IsDebugEnabled { get { return Logger == null || Logger.IsEnabled(LogLevel.Debug); } }
        public bool IsInfoEnabled { get { return Logger == null || Logger.IsEnabled(LogLevel.Information); } }
        public bool IsWarnEnabled { get { return Logger == null || Logger.IsEnabled(LogLevel.Warning); } }
        public bool IsErrorEnabled { get { return Logger == null || Logger.IsEnabled(LogLevel.Error); } }

        private void Write(LogLevel level, object message, Exception exception) {
            Microsoft.Extensions.Logging.ILogger current = Logger;
            if (current != null) {
                if (current.IsEnabled(level)) {
                    current.Log(level, exception, "{Message}", message);
                }
            } else {
                // Host not built yet; write straight to the console so early startup isn't silent
                Console.WriteLine(DateTime.Now.ToString("HH:mm:ss,fff") + " " + level + " " + category + " - " + message + (exception != null ? Environment.NewLine + exception : ""));
            }
        }

        public void Debug(object message) { Write(LogLevel.Debug, message, null); }
        public void Debug(object message, Exception exception) { Write(LogLevel.Debug, message, exception); }
        public void Info(object message) { Write(LogLevel.Information, message, null); }
        public void Info(object message, Exception exception) { Write(LogLevel.Information, message, exception); }
        public void Warn(object message) { Write(LogLevel.Warning, message, null); }
        public void Warn(object message, Exception exception) { Write(LogLevel.Warning, message, exception); }
        public void Error(object message) { Write(LogLevel.Error, message, null); }
        public void Error(object message, Exception exception) { Write(LogLevel.Error, message, exception); }
    }
}
