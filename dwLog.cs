using log4net;
using log4net.Config;
using log4net.Layout;
using log4net.Appender;

namespace DeterministicWorld
{
    internal abstract class dwLog
    {
        internal static readonly ILog logger;

        static dwLog()
        {
            logger = LogManager.GetLogger("DetWorld");

            string layoutPattern = "%date{HH:mm:ss} [%thread] %-5level %logger - %message%newline";
            PatternLayout layout = new PatternLayout(layoutPattern);

            ConsoleAppender appender = new ConsoleAppender();
            appender.Layout = layout;

            BasicConfigurator.Configure(appender);
        }
    }
}
