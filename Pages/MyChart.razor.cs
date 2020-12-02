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
    /// <summary>
    /// MyChart.razor に付随する partial クラス。
    /// あえて完全に .razor から分離している。(.razor のほうには @code ブロックを含まない)
    /// </summary>
    public partial class MyChart : ComponentBase
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

        // [Parameter] 属性を付加すると、当 .razor を TagHelper として使用するときに attribute として値を渡すことができる。
        // 例: <MyChart DataIdx="0" />
        //[Parameter]
        //public int DataIdx { get; set; } = 0;

        /// <summary>
        /// ブラウザの LocalStorage から値を取得
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private async ValueTask<string> getLocalStorage(string key)
        {
            return await JSRuntime.InvokeAsync<string>("getLocalStorage", key);
        }

        /// <summary>
        /// ブラウザの LocalStorage に値を保存
        /// </summary>
        /// <param name="key"></param>
        private async Task setLocalStorage(string key, string value)
        {
            await JSRuntime.InvokeAsync<string>("setLocalStorage", key, value);
        }

        /// <summary>
        /// ブラウザの LocalStorage からint値を取得。無ければ null を返す。
        /// </summary>
        /// <param name="key"></param>
        private async ValueTask<int?> getLocalStorageInt(string key)
        {
            try {
                return int.Parse(await getLocalStorage(key));
            } catch {
                return null;
            }
        }

        /// <summary>
        /// ブラウザの LocalStorage からdouble値を取得。無ければ null を返す。
        /// </summary>
        /// <param name="key"></param>
        private async ValueTask<double?> getLocalStorageDouble(string key)
        {
            try {
                return double.Parse(await getLocalStorage(key));
            } catch {
                return null;
            }
        }

        // データの数
        public int _infectDataCount { get { return dailyData?.InfectDataList?.Count ?? 0; } }

        // タイトル（地域名）
        private string _getTitle(int n) { return (dailyData?.InfectDataList)._nth(n)?.Title; }

        // 減衰度
        public static int[] _decayFactors = new int[] {
            1000,   // 0
            250,    // 1
            100,    // 2
            70,     // 3
            50,     // 4
            30,     // 5
            20,     // 6
            10,     // 7
            5,      // 8
            2,      // 9
        };

        public class Settings
        {
            public int radioIdx { get; set; }
            public int prefIdx { get; set; }
            public int barWidth { get; set; }
            public int[] yAxisMax { get; set; }
            public bool drawGompertz { get; set; }
            public bool estimatedBar { get; set; }
            public string[] paramRtStartDate { get; set; }
            public int[] paramRtDaysToOne { get; set; }
            public double[] paramRtDecayFactor { get; set; }

            public int dataIdx { get { return radioIdx < _mainPrefNum ? radioIdx : prefIdx; } }

            public Settings initialize(int numData)
            {
                radioIdx = 0;
                prefIdx = _mainPrefNum;
                barWidth = 0;
                yAxisMax = new int[numData];
                drawGompertz = false;
                estimatedBar = false;
                paramRtStartDate = new string[numData];
                paramRtDaysToOne = new int[numData];
                paramRtDecayFactor = new double[numData];
                return this;
            }

            private T[] _extendArray<T>(T[] array, int num)
            {
                if (array._isEmpty()) {
                    array = new T[num];
                } else if (array.Length < num) {
                    Array.Resize(ref array, num);
                }
                return array;
            }

            public Settings fillEmptyArray(int numData)
            {
                yAxisMax = _extendArray(yAxisMax, numData);
                paramRtStartDate = _extendArray(paramRtStartDate, numData);
                paramRtDaysToOne = _extendArray(paramRtDaysToOne, numData);
                paramRtDecayFactor = _extendArray(paramRtDecayFactor, numData);
                return this;
            }

            public int myYAxisMax() { return yAxisMax._getNth(dataIdx); }
            public string myParamStartDate() { return paramRtStartDate._getNth(dataIdx); }
            public int myParamDaysToOne() { return paramRtDaysToOne._getNth(dataIdx); }
            public double myParamDecayFactor() { return paramRtDecayFactor._getNth(dataIdx); }

            public void changeBarWidth(int value)
            {
                barWidth = Math.Min(Math.Max(barWidth + value, -4), 4);
            }
            public void changeYAxisMax(int value) {
                if (yAxisMax.Length > dataIdx) yAxisMax[dataIdx] = value;
            }
            public void setDrawGompertz(bool value) {
                drawGompertz = value;
            }
            public void setEstimatedBar(bool value) {
                estimatedBar = value;
            }
            public void setParamStartDate(string value) {
                if (paramRtStartDate.Length > dataIdx) paramRtStartDate[dataIdx] = value;
            }
            public void setParamDaysToOne(int value) {
                if (paramRtDaysToOne.Length > dataIdx) paramRtDaysToOne[dataIdx] = value;
            }
            public void setParamDecayFactor(double value) {
                if (paramRtDecayFactor.Length > dataIdx) paramRtDecayFactor[dataIdx] = value;
            }
        }

        private const string SettingsKey = "net.oktopus59.ncov.settings.v4"; // v1008

        private Settings _currentSettings = (new Settings()).initialize(0);

        private ChartBlazorApp.Models.InfectData _infectData { get { return dailyData.InfectDataList._getNth(_currentSettings.dataIdx); } }

        private int _radioIdx { get { return _currentSettings.radioIdx; } }

        private int _prefIdx { get { return Math.Max(_currentSettings.prefIdx, _mainPrefNum); } }

        private int _dataIdx { get { return _currentSettings.dataIdx; } }

        private int _barWidth { get { return _currentSettings.barWidth; } }

        private double _yAxisMax { get { var res = _currentSettings.myYAxisMax(); return res > 0 ? res : _infectData.Y1_Max; } }

        private bool _drawGompertz { get { return _currentSettings.drawGompertz; } }

        private bool _estimatedBar { get { return _currentSettings.estimatedBar; } }

        private string _paramDate { get { return _currentSettings.myParamStartDate()._orElse(() => _infectData.InitialDecayParam.StartDate._toDateString()); } }

        private string _startDate { get { var d = _paramDate._isEmpty() ? dailyData.FindDecayStartDate(_dataIdx) : ""; return d._notEmpty() ? $"({d})" : ""; } }

        private string _defaultDaysToOne { get { var v = _currentSettings.myParamDaysToOne(); return v > 0 ? "" : $"({_infectData.InitialDecayParam.DaysToOne})"; } }

        private int _paramDaysToOne { get { return _currentSettings.myParamDaysToOne()._gtZeroOr(_infectData.InitialDecayParam.DaysToOne); } }

        private double _paramDecayFactor { get { return _currentSettings.myParamDecayFactor()._gtZeroOr(_infectData.InitialDecayParam.DecayFactor); } }

        private async Task saveSettings()
        {
            await setLocalStorage(
                SettingsKey,
                JsonConvert.SerializeObject(_currentSettings));
        }

        /// <summary>
        /// 保存しておいた設定を取得する
        /// </summary>
        /// <returns></returns>
        private async Task getSettings()
        {
            async ValueTask<Settings> _getSettings()
            {
                try {
                    return JsonConvert.DeserializeObject<Settings>(await getLocalStorage(SettingsKey)).fillEmptyArray(_infectDataCount);
                } catch {
                    return (new Settings()).initialize(_infectDataCount);
                }
            }
            _currentSettings = await _getSettings();
        }

        private async Task insertStaticDescription()
        {
            //var html = Helper.GetFileContent("wwwroot/html/Description.html", System.Text.Encoding.UTF8);
            //if (html._notEmpty()) await JSRuntime.InvokeAsync<string>("insertStaticDescription", html);
            await JSRuntime.InvokeAsync<string>("selectDescription", "home-page");
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
                StateHasChanged();
                await RenderChartMethod(true, true);
            }
        }

        /// <summary>
        /// 親コンポーネントから初期パラメーターを受け取った後で初期化されるときに呼び出される。
        /// <para>
        /// _Host.cshtml で render-mode="ServerPrerendered" になっている場合は、プリレンダリング中なので、ここから JavaScript を呼び出してはいけない。
        /// なお ServerPrerendered の場合は、 OnInitialized が2回呼び出される(つまりインスタンスが2回生成される)可能性があることに注意。
        /// render-mode="Server" ならここから JavaScript を呼び出すことが可能。
        /// </para><para>
        /// この例で dailyData はシングルトンであり、その Initialize() メソッドが短時間のうちに複数回呼び出された場合は、2回目以降をスキップする処理をいれてある。
        /// </para><para>
        /// 参照: https://docs.microsoft.com/ja-jp/aspnet/core/blazor/components/lifecycle?view=aspnetcore-3.1 </para>
        /// </summary>
        //protected override async Task OnInitializedAsync()
        protected override void OnInitialized()
        {
            //dailyData.Initialize();

            //infectData = dailyData.InfectDataList._getNth(_currentSettings.dataIdx);
            //_approxDayPos = (infectData?.Dates)._safeCount() - 1;
            //_lastDate = infectData?.Dates.Last() ?? DateTime.MinValue;
            //await getSettings();
            //await RenderChartMethod(true, true);
        }

        public async Task ChangeChart(ChangeEventArgs args)
        {
            _currentSettings.radioIdx = args.Value.ToString()._parseInt();
            await RenderChartMethod();
        }

        public async Task ChangePref(ChangeEventArgs args)
        {
            _currentSettings.prefIdx = args.Value.ToString()._parseInt();
            _currentSettings.radioIdx = _mainPrefNum;
            await RenderChartMethod();
        }

        public async Task ChangeFile(ChangeEventArgs args)
        {
            var file = args.Value.ToString();
            Console.WriteLine($"selected file={file}");
            var text = await JSRuntime.InvokeAsync<string>("uploadFile");
            Console.WriteLine(text);
        }

        public async Task ChangeYAxisMax(ChangeEventArgs args)
        {
            _currentSettings.changeYAxisMax(args.Value.ToString()._parseInt(0));
            await RenderChartMethod();
        }

        public async Task RenderGompertzCurveMethod(ChangeEventArgs args)
        {
            _currentSettings.setDrawGompertz((bool)args.Value);
            await RenderChartMethod();
        }

        public async Task ChangeGompertzParamDate(ChangeEventArgs args)
        {
            string dt = args.Value.ToString();
            if (dt._reMatch(@"^\d+/\d+$")) dt = $"{DateTime.Now._yyyy()}/{dt}";
            _currentSettings.setParamStartDate(dt);
            await RenderChartMethod();
        }

        public async Task ChangeRtParamDaysToOne(ChangeEventArgs args)
        {
            _currentSettings.setParamDaysToOne(Math.Min(args.Value.ToString()._parseInt(0), 100));
            await RenderChartMethod();
        }

        public async Task ChangeRtParamDecayFactor(ChangeEventArgs args)
        {
            _currentSettings.setParamDecayFactor(args.Value.ToString()._parseDouble(0));
            await RenderChartMethod();
        }

        public async Task InitializeParams()
        {
            _currentSettings.setParamStartDate("");
            _currentSettings.setParamDaysToOne(0);
            _currentSettings.setParamDecayFactor(0);
            await RenderChartMethod();
        }

        public async Task RenderEstimatedBarMethod(ChangeEventArgs args)
        {
            _currentSettings.setEstimatedBar((bool)args.Value);
            await RenderChartMethod(false, _barWidth < 0);
        }

        public async Task RenderThickChartBarMethod()
        {
            _currentSettings.changeBarWidth(1);
            await RenderChartMethod(false, true);
        }

        public async Task RenderThinChartBarMethod()
        {
            _currentSettings.changeBarWidth(-1);
            await RenderChartMethod(false, true);
        }

        private DateTime _prevReloadDt = DateTime.MinValue;

        public async Task RenderReloadMethod()
        {
            // 5秒経過後、10秒以内に再度呼び出されたら、データの再読み込みを行う
            var dtNow = DateTime.Now;
            if (dtNow > _prevReloadDt.AddSeconds(5) && dtNow < _prevReloadDt.AddSeconds(10)) {
                dailyData.Initialize();
                forecastData.Initialize();
            }
            _prevReloadDt = dtNow;

            await RenderChartMethod(true, true);
        }

        /// <summary>
        /// グラフ描画メソッド。
        /// JSRuntime を介して JavaScript を呼び出している。
        /// <para>参照: https://docs.microsoft.com/ja-jp/aspnet/core/blazor/call-javascript-from-dotnet?view=aspnetcore-3.1 </para>
        /// </summary>
        /// <param name="bFirst">グラフ描画時のアニメーションの要否</param>
        /// <returns></returns>
        public async Task RenderChartMethod(bool bFirst = false, bool bResetScrollBar = false)
        {
            RtDecayParam rtParam = null;
            if (_drawGompertz) {
                rtParam = new RtDecayParam {
                    StartDate = _paramDate._parseDateTime(),
                    DaysToOne = _paramDaysToOne,
                    DecayFactor = _paramDecayFactor,
                };
            }

            var json = dailyData.MakeJsonData(_currentSettings.dataIdx, _yAxisMax, _endDate._parseDateTime(), rtParam, _estimatedBar, bFirst);

            var jsonStr = (json?.chartData)._toString();
            int barWidth = _estimatedBar && _barWidth < 0 ? 0 : _barWidth;
            double scrlbarRatio = bResetScrollBar || barWidth != _barWidth ? newlyDaysRatio() : 0;
            await JSRuntime.InvokeAsync<string>("renderChart2", "chart-wrapper-home", barWidth, scrlbarRatio, jsonStr);
            if (!bFirst) await saveSettings();
        }

        private double newlyDaysRatio()
        {
            // return (double)_infectData.Dates._safeCount() / Math.Max((_endDate._parseDateTime() - _infectData.Dates._getFirst()).TotalDays, 1);
            return (double)_infectData.Dates._safeCount() / _infectData.DispDays;
        }
    }
}
