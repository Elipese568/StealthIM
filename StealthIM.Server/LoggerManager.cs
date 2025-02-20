using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StealthIM.Server;

public static class LoggerManager
{
    private static Dictionary<Type, ILogger> _registedLoggers = new();
    private static ILoggerFactory _loggerFactory;

    static LoggerManager()
    {
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });
    }

    public static ILogger GetLogger<T>()
    {
        if (_registedLoggers.ContainsKey(typeof(T)))
            return _registedLoggers[typeof(T)];
        else
        {
            ILogger logger = _loggerFactory.CreateLogger<T>();
            _registedLoggers.Add(typeof(T), logger);
            return logger;
        }
    }
}
