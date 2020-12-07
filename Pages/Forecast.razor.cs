using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChartBlazorApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using ChartBlazorApp.JsInteropClasses;
using StandardCommon;

namespace ChartBlazorApp.Pages
{
    public partial class Forecast : ComponentBase
    {
        // IJSRuntime はシステムが既定で用意している
        [Inject]
        protected IJSRuntime JSRuntime { get; set; }

        // [Inject] 属性を付加すると、Startup.cs の AddSingleton() で追加したサービスのシングルトンインスタンスがここに注入される。
        // 参照: https://docs.microsoft.com/ja-jp/aspnet/core/blazor/fundamentals/dependency-injection?view=aspnetcore-3.1
        [Inject]
        protected DailyData dailyData { get; set; }

        [Inject]
        protected ForecastData forecastData { get; set; }

        private InfectData _infectData { get { return dailyData.InfectDataList[0]; } }

        private UserSettings _userSettings;

        private async Task insertStaticDescription()
        {
            //var html = Helper.GetFileContent("wwwroot/html/Description2.html", System.Text.Encoding.UTF8);
            //if (html._notEmpty()) await JSRuntime.InvokeAsync<string>("insertStaticDescription2", html);
            await JSRuntime.InvokeAsync<string>("selectDescription", "forecast-page");
        }

        private async Task getSettings()
        {
            _userSettings = await UserSettings.GetSettings(JSRuntime, 0);
        }

        /// <summary>
        /// プリレンダリングが終わった後に呼び出されるメソッド。
        /// _Host.cshtml で render-mode="ServerPrerendered" になっている場合は、
        /// JavaScript の実行などは、プリレンダリング中に実行できないので、ここでやる。
        /// <para>
        /// 参照: https://docs.microsoft.com/ja-jp/aspnet/core/blazor/components/lifecycle?view=aspnetcore-3.1 </para>
        /// </summary>
        /// <param name="firstRender"></param>
        /// <returns></returns>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender) {
                await insertStaticDescription();
                await getSettings();
                _infectData.PredictValuesEx(null, forecastData.FullDays(_infectData.Dates._first()), forecastData.PredictStartDate);
                await RenderDeathAndSeriousChart();
                StateHasChanged();
            }
        }

        protected override void OnInitialized()
        {
            forecastData.dailyData = dailyData;
            //forecastData.Initialize();
        }

        /// <summary>
        /// 死亡者数グラフと重症者数グラフの描画メソッド。
        /// JSRuntime を介して JavaScript を呼び出している。
        /// </summary>
        /// <returns></returns>
        public async Task RenderDeathAndSeriousChart(bool bResetScrollBar = false)
        {
            forecastData.MakePreliminaryData(_infectData);

            var json = forecastData.MakeDeathJsonData(_userSettings?.onlyOnClick ?? false);
            var jsonStr = (json?.chartData)._toString();
            await JSRuntime.InvokeAsync<string>("renderChart2", "chart-wrapper-death", -2, 100, jsonStr);

            json = forecastData.MakeSeriousJsonData(_userSettings?.onlyOnClick ?? false);
            jsonStr = (json?.chartData)._toString();
            await JSRuntime.InvokeAsync<string>("renderChart2", "chart-wrapper-serious", -2, 100, jsonStr);
        }

    }


}
