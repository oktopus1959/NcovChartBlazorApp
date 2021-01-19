using System;using System.Collections.Generic;
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
        private static ConsoleLog logger = ConsoleLog.GetLogger();

        // IJSRuntime はシステムが既定で用意している
        [Inject]
        protected IJSRuntime JSRuntime { get; set; }

        // [Inject] 属性を付加すると、Startup.cs の AddSingleton() で追加したサービスのシングルトンインスタンスがここに注入される。
        // 参照: https://docs.microsoft.com/ja-jp/aspnet/core/blazor/fundamentals/dependency-injection?view=aspnetcore-3.1
        [Inject]
        protected DailyData dailyData { get; set; }

        [Inject]
        protected ForecastData forecastData { get; set; }

        private UserForecastData _userData { get; set; } = new UserForecastData();

        private InfectData _infectData0 { get { return dailyData.InfectDataList[0]; } }

        private EffectiveParams _effectiveParams = new EffectiveParams(null, null);

        //private async Task insertStaticDescription()
        //{
        //    var html = Helper.GetFileContent("wwwroot/html/Description2.html", System.Text.Encoding.UTF8);
        //    if (html._notEmpty()) await JSRuntime.InvokeAsync<string>("insertStaticDescription2", html);
        //}

        private async Task selectStaticDescription()
        {
            await JSRuntime._selectDescription("forecast-page");
        }

        private async Task getSettings()
        {
            logger.Info($"CALLED");
            _effectiveParams = await EffectiveParams.CreateByGettingUserSettings(JSRuntime, dailyData);
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
                logger.Info($"CALLED");
                await getSettings();
                await RenderDeathAndSeriousChart();
                await selectStaticDescription();
                StateHasChanged();
            }
        }

        protected override void OnInitialized()
        {
            //forecastData.dailyData = dailyData;
            //forecastData.Initialize();
        }

        private bool _showOtherCharts = ConsoleLog.DEBUG_LEVEL > 0;

        public async Task ShowOtherCharts(ChangeEventArgs args)
        {
            _showOtherCharts = (bool)(args.Value);
            await RenderDeathAndSeriousChart(false);
            StateHasChanged();
        }

        private bool _extendDispDays = ConsoleLog.DEBUG_LEVEL > 0;

        public async Task ExtendDispDays(ChangeEventArgs args)
        {
            _extendDispDays = (bool)(args.Value);
            await RenderDeathAndSeriousChart(false);
            StateHasChanged();
        }

        /// <summary>
        /// 死亡者数グラフと重症者数グラフの描画メソッド。
        /// JSRuntime を介して JavaScript を呼び出している。
        /// </summary>
        /// <returns></returns>
        public async Task RenderDeathAndSeriousChart(bool bAnimation = true)
        {
            // システム設定によるパラメータ
            RtDecayParam rtParam = _effectiveParams.MakeRtDecayParam(true, 0);  // 0: 全国
            // ユーザ設定によるパラメータ
            RtDecayParam rtParamByUser = _effectiveParams.DetailSettings && !_effectiveParams.FourstepEnabled ? _effectiveParams.MakeRtDecayParam(false, 0) : null;

            // 予測に必要なデータの準備
            _userData = new UserForecastData(_extendDispDays).MakeData(forecastData, _infectData0, rtParam);
            var userDataByUser = rtParamByUser != null ? new UserForecastData(_extendDispDays).MakeData(forecastData, _infectData0, rtParamByUser, true) : null;

            bool onlyOnClick = _effectiveParams.OnlyOnClick;

            var jsonStr = forecastData.MakeDeathJsonData(_userData, userDataByUser, onlyOnClick, bAnimation);
            await JSRuntime._renderChart2("chart-wrapper-death", -2, newlyDaysRatio(), jsonStr);

            jsonStr = forecastData.MakeSeriousJsonData(_userData, userDataByUser, onlyOnClick, bAnimation);
            await JSRuntime._renderChart2("chart-wrapper-serious", -2, newlyDaysRatio(), jsonStr);

            if (_showOtherCharts) {
                jsonStr = forecastData.MakeDailyDeathJonData(_userData, userDataByUser, onlyOnClick);
                await JSRuntime._renderChart2("chart-wrapper-dailydeath", -2, newlyDaysRatio(), jsonStr);

                jsonStr = forecastData.MakeSeriousDiffJsonData(_userData, onlyOnClick);
                await JSRuntime._renderChart2("chart-wrapper-seriousdiff", -2, 100, jsonStr);

                jsonStr = forecastData.MakeDeathDiffJsonData(_userData, onlyOnClick);
                await JSRuntime._renderChart2("chart-wrapper-deathdiff", -2, 100, jsonStr);

                jsonStr = forecastData.MakeBothDiffJsonData(_userData, onlyOnClick);
                await JSRuntime._renderChart2("chart-wrapper-bothsum", -2, 100, jsonStr);
            }
        }

        private double newlyDaysRatio()
        {
            int totalDays = _userData.LabelDates._safeCount();
            if (!_userData.UseFourStep || totalDays < 1) return 100;
            return  (double)_userData.RealDeath._safeCount() / totalDays;
        }
    }


}
