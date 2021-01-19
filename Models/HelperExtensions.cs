using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace ChartBlazorApp.Models
{
    public static class HelperExtensions
    {
        public static double?[] _toNullableArray(this IEnumerable<double> array, int roundDigit, double? defval = null)
        {
            return array.Select(x => x > 0 ? Math.Round(x, roundDigit) : defval).ToArray();
        }

        public static async Task _renderChart2(this IJSRuntime jsRuntime, string chartId, int barWidth, double scrollRatio, string jsonStr,
            [System.Runtime.CompilerServices.CallerMemberName] string method = "",
            [System.Runtime.CompilerServices.CallerFilePath] string path = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int linenum = 0)
        {
            await jsRuntime._invokeAsyncEx(callerLoc(method, path, linenum), "renderChart2", chartId, barWidth, scrollRatio, jsonStr);
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
