using System;

using log4net;
using log4net.Config;
using log4net.Layout;
using log4net.Appender;

namespace DeterministicWorld
{
    internal abstract class dwLog
    {
        private static readonly ILog logger;

        static dwLog()
        {
            logger = LogManager.GetLogger("DetWorld");

            //string layoutPattern = "%date{HH:mm:ss} [%thread] %-5level %logger - %message%newline";
            string layoutPattern = "%message%newline";
            PatternLayout layout = new PatternLayout(layoutPattern);

            ConsoleAppender appender = new ConsoleAppender();
            appender.Layout = layout;

            BasicConfigurator.Configure(appender);
        }

        public static void info(string msg)
        {
            logger.Info(msg);
        }

        public static void debug(string msg)
        {
            logger.Debug(msg);
        }

        public static void warn(string msg)
        {
            logger.Warn(msg);
        }

        public static void fatal(string msg)
        {
            logger.Fatal(msg);
        }
    }
}
