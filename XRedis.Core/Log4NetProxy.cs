using log4net;
using log4net.Appender;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net.Config;
using log4net.Core;
using log4net.Repository;

namespace XRedis.Core
{
    public class Log4NetProxy<T> : ILogger
    {
        public static void SetLevel(Type type, string levelName)
        {
            ILog log = LogManager.GetLogger(type);
            Logger l = (Logger)log.Logger;

            l.Level = l.Hierarchy.LevelMap[levelName];
        }

        // Add an appender to a logger
        public static void AddAppender(Type type, IAppender appender)
        {
            ILog log = LogManager.GetLogger(type);
            Logger l = (Logger)log.Logger;

            l.AddAppender(appender);
        }
        // Add an appender to a logger
        public static void AddAppender2(ILog log, IAppender appender)
        {
            // ILog log = LogManager.GetLogger(loggerName);
            Logger l = (Logger)log.Logger;

            l.AddAppender(appender);
        }

        // Create a new file appender
        public static IAppender CreateFileAppender(string name, string fileName)
        {
            FileAppender appender = new FileAppender();
            appender.Name = name;
            appender.File = fileName;
            appender.AppendToFile = true;

            PatternLayout layout = new PatternLayout();
            layout.ConversionPattern = "%d [%t] %-5p %c [%logger] - %m%n";
            layout.ActivateOptions();

            appender.Layout = layout;
            appender.ActivateOptions();

            return appender;
        }

        private static ILog GetMyLogger()
        {
            var logger = LogManager.GetLogger(typeof(T));
            //BasicConfigurator.Configure();
            SetLevel(typeof(T), "ALL");
            AddAppender2(logger, CreateFileAppender("myappender", "rflog.log"));
            return logger;
        }

        private static ILog _logger;

        private static ILog Logger
        {
            get
            {
                if (_logger == null)
                {
                    _logger = GetMyLogger();
                }
                return _logger;
            }
            
        }


        public void Log(string message)
        {
           Logger.Debug(message);
        }
       
    }

    public interface ILogger
    {
        void Log(string message);
    }
}
