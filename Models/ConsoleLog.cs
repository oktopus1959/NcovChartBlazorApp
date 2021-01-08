using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using StandardCommon;

namespace ChartBlazorApp.Models
{
    public class ConsoleLog
    {
        public static ConsoleLog GetLogger()
        {
            return new ConsoleLog() {
                ClassName = new StackFrame(1).GetMethod().DeclaringType.FullName._split('.')[^1],
                DebugLevel = DEBUG_LEVEL
            };
        }

        public string ClassName { get; set; }

        public int DebugLevel { get; set; } = 0;

        public static void INFO(string msg, string caller = null,
            [System.Runtime.CompilerServices.CallerMemberName] string method = "")
        {
            consoleWrite($"INFO ", caller ?? method, msg);
        }

        public static void INFO(Func<string> func, string caller = null,
            [System.Runtime.CompilerServices.CallerMemberName] string method = "")
        {
            if (func != null) INFO(func(), caller ?? method);
        }

        public void InfoNL(int nl = 1)
        {
            Console.WriteLine(new string[(nl - 1)._lowLimit(0)]._fill("\n")._join(""));
        }

        public void Info(string msg, string caller = null,
            [System.Runtime.CompilerServices.CallerMemberName] string method = "")
        {
            INFO(msg, caller ?? $"{ClassName}.{method}");
        }

        public void Info(Func<string> func, string caller = null,
            [System.Runtime.CompilerServices.CallerMemberName] string method = "")
        {
            INFO(func, caller ?? $"{ClassName}.{method}");
        }

#if DEBUG
        public static int DEBUG_LEVEL { get; set; } = 1;

        public static void DEBUG(string msg, string caller = null,
            [System.Runtime.CompilerServices.CallerMemberName] string method = "")
        {
            if (DEBUG_LEVEL >= 1) consoleWrite($"DEBUG", caller ?? method, msg);
        }

        public static void DEBUG(Func<string> func, string caller = null,
            [System.Runtime.CompilerServices.CallerMemberName] string method = "")
        {
            if (func != null) DEBUG(func(), caller ?? method);
        }

        public void DebugNL(int nl = 1)
        {
            if (DebugLevel >= 1) Console.WriteLine(new string[(nl - 1)._lowLimit(0)]._fill("\n")._join(""));
        }

        public void Debug(string msg, string caller = null,
            [System.Runtime.CompilerServices.CallerMemberName] string method = "")
        {
            if (DebugLevel >= 1) DEBUG(msg, caller ?? $"{ClassName}.{method}");
        }

        public void Debug(Func<string> func, string caller = null,
            [System.Runtime.CompilerServices.CallerMemberName] string method = "")
        {
            if (DebugLevel >= 1) DEBUG(func, caller ?? $"{ClassName}.{method}");
        }

        public static void _TRACE(int level, string msg, string caller)
        {
            consoleWrite($"TRACE{(level > 2 ? (level - 1).ToString() : "")}", caller, msg);
        }

        public static void _TRACE(int level, Func<string> func, string caller)
        {
            if (func != null) _TRACE(level, func(), caller);
        }

        public static void TRACE(string msg, string caller = null,
            [System.Runtime.CompilerServices.CallerMemberName] string method = "")
        {
            if (DEBUG_LEVEL >= 2) _TRACE(1, msg, caller ?? method);
        }

        public static void TRACE(Func<string> func, string caller = null,
            [System.Runtime.CompilerServices.CallerMemberName] string method = "")
        {
            if (DEBUG_LEVEL >= 2) _TRACE(1, func(), caller ?? method);
        }

        public void Trace(string msg, string caller = null,
            [System.Runtime.CompilerServices.CallerMemberName] string method = "")
        {
            if (DebugLevel >= 2) _TRACE(3, msg, caller ?? $"{ClassName}.{method}");
        }

        public void Trace(Func<string> func, string caller = null,
            [System.Runtime.CompilerServices.CallerMemberName] string method = "")
        {
            if (DebugLevel >= 2) _TRACE(3, func, caller ?? $"{ClassName}.{method}");
        }

        public void Trace2(string msg, string caller = null,
            [System.Runtime.CompilerServices.CallerMemberName] string method = "")
        {
            if (DebugLevel == 3 || DebugLevel >= 5) _TRACE(3, msg, caller ?? $"{ClassName}.{method}");
        }

        public void Trace2(Func<string> func, string caller = null,
            [System.Runtime.CompilerServices.CallerMemberName] string method = "")
        {
            if (DebugLevel == 3 || DebugLevel >= 5) _TRACE(3, func, caller ?? $"{ClassName}.{method}");
        }

        public void Trace3(string msg, string caller = null,
            [System.Runtime.CompilerServices.CallerMemberName] string method = "")
        {
            if (DebugLevel >= 4) _TRACE(4, msg, caller ?? $"{ClassName}.{method}");
        }

        public void Trace3(Func<string> func, string caller = null,
            [System.Runtime.CompilerServices.CallerMemberName] string method = "")
        {
            if (DebugLevel >= 4) _TRACE(4, func, caller ?? $"{ClassName}.{method}");
        }

        public void Trace4(string msg, string caller = null,
            [System.Runtime.CompilerServices.CallerMemberName] string method = "")
        {
            if (DebugLevel >= 5) _TRACE(5, msg, caller ?? $"{ClassName}.{method}");
        }

        public void Trace4(Func<string> func, string caller = null,
            [System.Runtime.CompilerServices.CallerMemberName] string method = "")
        {
            if (DebugLevel >= 5) _TRACE(5, func, caller ?? $"{ClassName}.{method}");
        }

        public void TraceA(string msg, string caller = null,
            [System.Runtime.CompilerServices.CallerMemberName] string method = "")
        {
            if (DebugLevel >= 3 ) _TRACE(10, msg, caller ?? $"{ClassName}.{method}");
        }

        public void TraceA(Func<string> func, string caller = null,
            [System.Runtime.CompilerServices.CallerMemberName] string method = "")
        {
            if (DebugLevel >= 3) _TRACE(10, func, caller ?? $"{ClassName}.{method}");
        }
#else
        public static int DEBUG_LEVEL { get; set; } = 0;
        public static void DEBUG(string msg, string caller = null) {}
        public static void DEBUG(Func<string> func, string caller = null) {}
        public void DebugNL(int nl = 1) {}
        public void Debug(string msg, string caller = null) {}
        public void Debug(Func<string> func, string caller = null) {}
        public static void TRACE(string msg, string caller = null) {}
        public static void TRACE(Func<string> func, string caller = null) {}
        public void Trace(string msg, string caller = null) {}
        public void Trace(Func<string> func, string caller = null) {}
        public void Trace2(string msg, string caller = null) {}
        public void Trace2(Func<string> func, string caller = null) {}
        public void Trace3(string msg, string caller = null) {}
        public void Trace3(Func<string> func, string caller = null) {}
        public void Trace4(string msg, string caller = null) {}
        public void Trace4(Func<string> func, string caller = null) {}
        public void Trace5(string msg, string caller = null) {}
        public void Trace5(Func<string> func, string caller = null) {}
#endif

        public static void WARN(string msg, string caller = null,
            [System.Runtime.CompilerServices.CallerMemberName] string method = "")
        {
            consoleWrite($"\x1b[33;1mWARN\x1b[0m ", caller ?? method, msg);
        }

        public static void WARN(Func<string> func, string caller = null,
            [System.Runtime.CompilerServices.CallerMemberName] string method = "")
        {
            if (func != null) WARN(func(), caller ?? method);
        }

        public void Warn(string msg, string caller = null,
            [System.Runtime.CompilerServices.CallerMemberName] string method = "")
        {
            WARN(msg, caller ?? $"{ClassName}.{method}");
        }

        public void Warn(Func<string> func, string caller = null,
            [System.Runtime.CompilerServices.CallerMemberName] string method = "")
        {
            WARN(func, caller ?? $"{ClassName}.{method}");
        }

        public static void ERROR(string msg, string caller = null,
            [System.Runtime.CompilerServices.CallerMemberName] string method = "")
        {
            consoleWrite($"\x1b[31;1mERROR\x1b[0m", caller ?? method, msg);
        }

        public static void ERROR(Func<string> func, string caller = null,
            [System.Runtime.CompilerServices.CallerMemberName] string method = "")
        {
            if (func != null) ERROR(func(), caller ?? method);
        }

        public void Error(string msg, string caller = null,
            [System.Runtime.CompilerServices.CallerMemberName] string method = "")
        {
            ERROR(msg, caller ?? $"{ClassName}.{method}");
        }

        public void Error(Func<string> func, string caller = null,
            [System.Runtime.CompilerServices.CallerMemberName] string method = "")
        {
            ERROR(func, caller ?? $"{ClassName}.{method}");
        }

        private static void consoleWrite(string level, string caller, string msg)
        {
            Console.WriteLine($"{DateTime.Now._toString()} {level} [{caller}] {msg}");
        }

        private static string callerLoc(string method, string path, int linenum)
        {
            var fname = path._reReplaceIcase(@"^.*[/\\]([A-Za-z0-9_]+)\.[a-z]+$", "$1");
            return $"{fname}({linenum}):{method}";
        }
    }
}
