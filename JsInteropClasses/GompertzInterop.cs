using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using ChartBlazorApp.Models;

namespace ChartBlazorApp.JsInteropClasses
{
    /// <summary>
    /// JavaScript からサーバ側のインスタンスメソッドを呼び出す処理を行うクラス。
    /// 今回は使用していないが、サンプルとして残しておく。
    /// _Host.cshtml の renderChart0 関数も参照のこと。
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/ja-jp/aspnet/core/blazor/call-dotnet-from-javascript?view=aspnetcore-3.1"/>
    /// <seealso cref="https://docs.microsoft.com/ja-jp/aspnet/core/blazor/call-javascript-from-dotnet?view=aspnetcore-3.1"/>
    public class GompertzInterop : IDisposable
    {

        private readonly IJSRuntime jsRuntime;
        private DotNetObjectReference<DailyData> objRef;

        public GompertzInterop(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
        }

        public async Task CallHelperGetChartData(DailyData data,
            int dataIdx, int predDayPos, string realStopDate, string endDate, bool bManual, bool bAnimation)
        {
            objRef = DotNetObjectReference.Create(data);

            await jsRuntime.InvokeAsync<string>(
                "renderChart0", objRef, dataIdx, predDayPos, realStopDate, endDate, bManual, bAnimation);
        }

        public void Dispose()
        {
            objRef?.Dispose();
        }
    }
}
