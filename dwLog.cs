using System;
using System.IO;

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

            ConsoleAppender cappender = new ConsoleAppender();
            cappender.Layout = layout;

            RollingFileAppender appender = new RollingFileAppender();
            appender.File = Environment.CurrentDirectory+"\\warlogs.txt";
            appender.ImmediateFlush = true;
            appender.RollingStyle = RollingFileAppender.RollingMode.Once;
            appender.Layout = layout;

            layout.ActivateOptions();
            appender.ActivateOptions();
            cappender.ActivateOptions();

            BasicConfigurator.Configure(appender);
            BasicConfigurator.Configure(cappender);
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
