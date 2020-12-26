using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StandardCommon;

namespace ChartBlazorApp.Models
{
    public static class ConsoleLog
    {
#if DEBUG
        public static bool DebugFlag { get; set; } = true;
#else
        public static bool DebugFlag { get; set; }
#endif

        public static void Info(string msg)
        {
            Console.WriteLine($"{DateTime.Now} [INFO]  {msg}");
        }

        public static void Info(Func<string> func)
        {
            if (func != null) Info(func());
        }

        public static void Debug(string msg)
        {
            if (DebugFlag) {
                Console.WriteLine($"{DateTime.Now} [DEBUG] {msg}");
            }
        }

        public static void Debug(Func<string> func)
        {
            if (DebugFlag) {
                if (func != null) Debug(func());
            }
        }

        public static void Warn(string msg)
        {
            Console.WriteLine($"{DateTime.Now} [\x1b[33;1mWARN\x1b[0m]  {msg}");
        }

        public static void Warn(Func<string> func)
        {
            if (func != null) Warn(func());
        }

        public static void Error(string msg)
        {
            Console.WriteLine($"{DateTime.Now} [\x1b[31;1mERROR\x1b[0m] {msg}");
        }

        public static void Error(Func<string> func)
        {
            if (func != null) Error(func());
        }
    }
}
