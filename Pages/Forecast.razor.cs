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

        private EffectiveParams _effectiveParams = new EffectiveParams(null, null);

        //private InfectData _infectData0 { get { return dailyData.InfectDataList[0]; } }

        private async Task insertStaticDescription()
        {
            var html = Helper.GetFileContent("wwwroot/html/Forecast.html", System.Text.Encoding.UTF8);
            //if (html._notEmpty()) await JSRuntime.InvokeAsync<string>("insertDescription", "forecast-description", html);
            if (html._notEmpty()) await JSRuntime._insertDescription("forecast-description", html);
        }

        //private async Task selectStaticDescription()
        //{
        //    await JSRuntime._selectDescription("forecast-page");
        //}

        private async Task getSettings()
        {
            logger.Info($"CALLED");
            _effectiveParams = await EffectiveParams.CreateByGettingUserSettings(JSRuntime, dailyData);
        }

        private void initializeTimeMachineInfectData()
        {
            if (_effectiveParams.TimeMachineMode) _effectiveParams.MakeTimeMachineInfectData();
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
                await insertStaticDescription();
                await getSettings();
                initializeTimeMachineInfectData();
                if (_showOtherCharts) StateHasChanged();
                await RenderDeathAndSeriousChart();
                //await selectStaticDescription();
                StateHasChanged();
            }
        }

        protected override void OnInitialized()
        {
            //forecastData.dailyData = dailyData;
            //forecastData.Initialize();
        }

        private bool _showOtherCharts => _effectiveParams.OtherForecastCharts;

        public async Task ShowOtherCharts(ChangeEventArgs args)
        {
            _effectiveParams.CurrentSettings.setOtherForecastCharts((bool)args.Value);
            await _effectiveParams.CurrentSettings.SaveSettings();
            await RenderDeathAndSeriousChart(false);
            StateHasChanged();
        }

        private bool _extendDispDays => _effectiveParams.CurrentSettings.forecastExpandChartDates; // ConsoleLog.DEBUG_LEVEL > 0;

        public async Task ExtendDispDays(ChangeEventArgs args)
        {
            _effectiveParams.CurrentSettings.forecastExpandChartDates = (bool)(args.Value);
            await _effectiveParams.CurrentSettings.SaveSettings();
            await RenderDeathAndSeriousChart(false);
            StateHasChanged();
        }

        private bool _thinBar => _effectiveParams.ThinForecastBar;

        public async Task ThinBar(ChangeEventArgs args)
        {
            _effectiveParams.CurrentSettings.setThinForecastBar((bool)args.Value);
            await _effectiveParams.CurrentSettings.SaveSettings();
            await RenderDeathAndSeriousChart(false);
            StateHasChanged();
        }

        private bool _fullWidthChart => _effectiveParams.CurrentSettings.forecastFullWidthCharts;

        public async Task FullWidthChart(ChangeEventArgs args)
        {
            _effectiveParams.CurrentSettings.forecastFullWidthCharts = (bool)args.Value;
            await _effectiveParams.CurrentSettings.SaveSettings();
            await RenderDeathAndSeriousChart(false);
            StateHasChanged();
        }

        public async Task Reload()
        {
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
            RtDecayParam rtParam;               // システム設定によるパラメータ
            RtDecayParam rtParamByUser = null;  // ユーザ設定によるパラメータ
            InfectData infectData;

            bool timeMachineMode = _effectiveParams.TimeMachineMode && _effectiveParams.RadioIdx == 0;  // タイムマシンモードを使うか
            if (timeMachineMode) {
                rtParam = _effectiveParams.MakeRtDecayParam(false, _effectiveParams.MyDataIdx);
                infectData = _effectiveParams.MyInfectData;
            } else {
                rtParam = _effectiveParams.MakeRtDecayParam(true, 0);
                if (_effectiveParams.DetailSettings && !_effectiveParams.FourstepEnabled) {
                    rtParamByUser = _effectiveParams.MakeRtDecayParam(false, 0);
                }
                infectData = _effectiveParams.NthInfectData(0);
            }

            // 予測に必要なデータの準備
            var startDt = _effectiveParams.DispStartDate._parseDateTime();
            _userData = new UserForecastData(_extendDispDays).MakeData(forecastData, infectData, rtParam, startDt, timeMachineMode);
            var userDataByUser = rtParamByUser != null ? new UserForecastData(_extendDispDays).MakeData(forecastData, infectData, rtParamByUser, startDt, false, true) : null;

            bool onlyOnClick = _effectiveParams.OnlyOnClick;
            int barWidth = _thinBar ? -4 : -2;

            string jsonStr;

            jsonStr = forecastData.MakeSeriousJsonData(_userData, userDataByUser, onlyOnClick, bAnimation);
            await JSRuntime._renderChart2("chart-wrapper-serious", barWidth, newlyDaysRatio(), jsonStr);

            jsonStr = forecastData.MakeDeathJsonData(_userData, userDataByUser, onlyOnClick, bAnimation);
            await JSRuntime._renderChart2("chart-wrapper-death", barWidth, newlyDaysRatio(), jsonStr);

            if (_showOtherCharts) {
                jsonStr = forecastData.MakeDailyDeathJonData(_userData, userDataByUser, onlyOnClick);
                await JSRuntime._renderChart2("chart-wrapper-dailydeath", barWidth, newlyDaysRatio(), jsonStr);

                jsonStr = forecastData.MakeSeriousDiffJsonData(_userData, onlyOnClick);
                await JSRuntime._renderChart2("chart-wrapper-seriousdiff", barWidth, 100, jsonStr);

                jsonStr = forecastData.MakeDeathDiffJsonData(_userData, onlyOnClick);
                await JSRuntime._renderChart2("chart-wrapper-deathdiff", barWidth, 100, jsonStr);

                jsonStr = forecastData.MakeBothDiffJsonData(_userData, onlyOnClick);
                await JSRuntime._renderChart2("chart-wrapper-bothsum", barWidth, 100, jsonStr);
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
