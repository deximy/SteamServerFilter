using log4net;

namespace SteamServerFilter
{
    public static class LogService
    {
        private static readonly ILog log_manager_;

        static LogService()
        {
            log4net.Config.XmlConfigurator.Configure();
            log_manager_ = LogManager.GetLogger("log");
        }

        public static void Error(object message)
        {
            log_manager_.Error(message);
        }

        public static void Info(object message)
        {
            log_manager_.Info(message);
        }

        public static void Debug(object message)
        {
            log_manager_.Debug(message);
        }
    }
}
