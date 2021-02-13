using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using StandardCommon;

namespace ChartBlazorApp.Models
{
    public static class HelperExtensions
    {
        private static ConsoleLog logger = ConsoleLog.GetLogger();

        public static double?[] _toNullableArray(this IEnumerable<double> array, int roundDigit, double? defval = null)
        {
            return array.Select(x => x > 0 ? Math.Round(x, roundDigit) : defval).ToArray();
        }

        public static string _toFullDateStr(this string dtStr, bool bAllowDayOnly = false)
        {
            var dtNow = DateTime.Now;
            if (bAllowDayOnly && dtStr._reMatch(@"^\d+$")) {
                dtStr = $"{dtNow:MM}/{dtStr}";
            }
            if (dtStr._reMatch(@"^\d+/\d+$")) {
                var result = $"{dtNow:yyyy}/{dtStr}";
                var resDt = result._parseDateTime();
                if (resDt > dtNow.AddMonths(9))
                    result = $"{dtNow.AddYears(-1):yyyy}/{dtStr}";
                else if (resDt < dtNow.AddMonths(-9))
                    result = $"{dtNow.AddYears(1):yyyy}/{dtStr}";
                return result;
            }
            return dtStr;
        }

        /// <summary> MM/DD 形式の日付文字列に変換して返す </summary>
        /// <param name="dtStr"></param>
        /// <returns></returns>
        public static string _toCanonicalMMDD(this string dtStr)
        {
            return dtStr._toFullDateStr()._parseDateTime().ToString("MM/dd");
        }

        public static async Task _renderChart2(this IJSRuntime jsRuntime, string chartId, int barWidth, double scrollRatio, string jsonStr,
            [System.Runtime.CompilerServices.CallerMemberName] string method = "",
            [System.Runtime.CompilerServices.CallerFilePath] string path = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int linenum = 0)
        {
            logger.Debug(() => $"renderChart2: json len={jsonStr.Length}");
            await jsRuntime._invokeAsyncEx(callerLoc(method, path, linenum), "renderChart2", chartId, barWidth, scrollRatio, jsonStr);
        }

        public static async Task _insertHtmlFile(this IJSRuntime jsRuntime, string htmlPath, string divId,
            [System.Runtime.CompilerServices.CallerMemberName] string method = "",
            [System.Runtime.CompilerServices.CallerFilePath] string path = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int linenum = 0)
        {
            var html = Helper.GetFileContent(htmlPath, System.Text.Encoding.UTF8);
            if (html._notEmpty()) await jsRuntime._invokeAsyncEx(callerLoc(method, path, linenum), "insertDescription", divId, html);
        }

        public static async Task _insertDescription(this IJSRuntime jsRuntime, string divId, string html,
            [System.Runtime.CompilerServices.CallerMemberName] string method = "",
            [System.Runtime.CompilerServices.CallerFilePath] string path = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int linenum = 0)
        {
            await jsRuntime._invokeAsyncEx(callerLoc(method, path, linenum), "insertDescription", divId, html);
        }

        public static async Task _initializeDescription(this IJSRuntime jsRuntime,
            [System.Runtime.CompilerServices.CallerMemberName] string method = "",
            [System.Runtime.CompilerServices.CallerFilePath] string path = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int linenum = 0)
        {
            await jsRuntime._invokeAsyncEx(callerLoc(method, path, linenum), "initializeDescription");
        }

        public static async Task _selectDescription(this IJSRuntime jsRuntime, string pageId,
            [System.Runtime.CompilerServices.CallerMemberName] string method = "",
            [System.Runtime.CompilerServices.CallerFilePath] string path = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int linenum = 0)
        {
            await jsRuntime._invokeAsyncEx(callerLoc(method, path, linenum), "selectDescription", pageId);
        }

        public static async ValueTask<string> _getSettings(this IJSRuntime jsRuntime,
            [System.Runtime.CompilerServices.CallerMemberName] string method = "",
            [System.Runtime.CompilerServices.CallerFilePath] string path = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int linenum = 0)
        {
            return await jsRuntime._invokeAsync(callerLoc(method, path, linenum), "getLocalStorage", Constants.SETTINGS_KEY);
        }

        public static async ValueTask<bool> _saveSettings(this IJSRuntime jsRuntime, string jsonStr,
            [System.Runtime.CompilerServices.CallerMemberName] string method = "",
            [System.Runtime.CompilerServices.CallerFilePath] string path = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int linenum = 0)
        {
            var result = await jsRuntime._invokeAsync(callerLoc(method, path, linenum), "setLocalStorage", Constants.SETTINGS_KEY, jsonStr);
            return result != null;
        }

        public static async ValueTask<string> _invokeAsync(this IJSRuntime jsRuntime, string caller, string funcname, params object[] args)
        {
            try {
                return await jsRuntime.InvokeAsync<string>(funcname, args);
            } catch (Exception e) {
                ConsoleLog.ERROR($"[JSRuntime.InvokeAsync({funcname})] {e}", caller);
                return null;
            }
        }

        public static async ValueTask<string> _invokeAsyncEx(this IJSRuntime jsRuntime, string caller, string funcname, params object[] args)
        {
            try {
                return await jsRuntime.InvokeAsync<string>(funcname, args);
            } catch (Exception e) {
                ConsoleLog.ERROR($"[JSRuntime.InvokeAsync({funcname})] {e}", caller);
                await Task.Delay(1000);
                ConsoleLog.INFO($"[JSRuntime.InvokeAsync({funcname}] RETRY");
                try {
                    return await jsRuntime.InvokeAsync<string>(funcname, args);
                } catch (Exception ex) {
                    ConsoleLog.ERROR($"[JSRuntime.InvokeAsync({funcname})] {ex}", caller);
                    ConsoleLog.ERROR($"RETRY ERROR. GIVE UP.", caller);
                    return null;
                }

            }
        }

        private static string callerLoc(string method, string path, int linenum)
        {
            var fname = System.Text.RegularExpressions.Regex.Replace(path, @"^.*[/\\]([A-Za-z0-9_]+)\.[a-z]+$", "$1");
            return $"{fname}({linenum}):{method}";
        }
    }
}
