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

        public static double GetDecayFactor(double indexOrFactor)
        {
            return (indexOrFactor < _decayFactors.Length) ? _decayFactors[Math.Max((int)indexOrFactor, 0)] : indexOrFactor;
        }

        private UserSettings _currentSettings = UserSettings.CreateInitialSettings();

        private bool _cancellable = false;

        private ChartBlazorApp.Models.InfectData _infectData { get { return dailyData.InfectDataList._getNth(_currentSettings.dataIdx); } }

        private int _radioIdx { get { return _currentSettings.radioIdx; } }

        private int _prefIdx { get { return Math.Max(_currentSettings.prefIdx, _mainPrefNum); } }

        private int _dataIdx { get { return _currentSettings.dataIdx; } }

        private int _barWidth { get { return _currentSettings.barWidth; } }

        private double _yAxisMax { get { var res = _currentSettings.myYAxisMax(); return res > 0 ? res : _infectData.Y1_Max; } }

        private bool _drawExpectation { get { return _currentSettings.drawExpectation; } }

        private bool _estimatedBar { get { return _currentSettings.estimatedBar; } }

        private bool _detailSettings { get { return _currentSettings.detailSettings; } }

        private bool _fourstepSettings { get { return _currentSettings.fourstepSettings; } }

        private bool _onlyOnClick { get { return _currentSettings.onlyOnClick; } }

        private int _extensionDays { get { return _currentSettings.extensionDays._gtZeroOr(UserSettings.ExtensionDays); } }

        private bool _useOnForecast { get { return _currentSettings.useOnForecast; } }

        private string _paramDate { get { return _currentSettings.myParamStartDate()._orElse(() => _infectData.InitialDecayParam.StartDate._toDateString()); } }

        private string _paramDateFourstep { get { return _currentSettings.myParamStartDateFourstep()._orElse(() => _infectData.InitialDecayParam.StartDateFourstep._toDateString()); } }

        private string _defaultDaysToOne { get { var v = _currentSettings.myParamDaysToOne(); return v > 0 ? "" : $"({_infectData.InitialDecayParam.DaysToOne})"; } }

        private int _paramDaysToOne { get { return _currentSettings.myParamDaysToOne()._gtZeroOr(_infectData.InitialDecayParam.DaysToOne); } }

        private double _paramDecayFactor { get { return _currentSettings.myParamDecayFactor()._gtZeroOr(_infectData.InitialDecayParam.DecayFactor); } }

        private int _paramDaysToNext { get { return _currentSettings.myParamDaysToNext()._gtZeroOr(_infectData.InitialDecayParam.DaysToNext); } }

        private double _paramDecayFactorNext { get { return _currentSettings.myParamDecayFactorNext()._gtZeroOr(_infectData.InitialDecayParam.DecayFactorNext); } }

        private double _paramRtMax { get { return _currentSettings.myParamMaxRt()._gtZeroOr(_infectData.InitialDecayParam.RtMax); } }

        private double _paramRtMin { get { return _currentSettings.myParamMinRt()._gtZeroOr(_infectData.InitialDecayParam.RtMin); } }

        private int _paramDaysToRt1 { get { return _currentSettings.myParamDaysToRt1()._gtZeroOr(_infectData.InitialDecayParam.DaysToRt1); } }

        private double _paramRt1 { get { return _currentSettings.myParamRt1()._gtZeroOr(_infectData.InitialDecayParam.Rt1); } }

        private int _paramDaysToRt2 { get { return _currentSettings.myParamDaysToRt2()._gtZeroOr(_infectData.InitialDecayParam.DaysToRt2); } }

        private double _paramRt2 { get { return _currentSettings.myParamRt2()._gtZeroOr(_infectData.InitialDecayParam.Rt2); } }

        private int _paramDaysToRt3 { get { return _currentSettings.myParamDaysToRt3()._gtZeroOr(_infectData.InitialDecayParam.DaysToRt3); } }

        private double _paramRt3 { get { return _currentSettings.myParamRt3()._gtZeroOr(_infectData.InitialDecayParam.Rt3); } }

        private int _paramDaysToRt4 { get { return _currentSettings.myParamDaysToRt4()._gtZeroOr(_infectData.InitialDecayParam.DaysToRt4); } }

        private double _paramRt4 { get { return _currentSettings.myParamRt4()._gtZeroOr(_infectData.InitialDecayParam.Rt4); } }

        private int _favorPrefNum { get { return _currentSettings.favorPrefNum; } }

        private int _selectorPos { get { return _currentSettings.selectorRadioPos; } }

        /// <summary>idx は ラジオボタンのインデックス</summary>
        private string _radioPrefName(int idx) { return _getTitle(_currentSettings.prefIdxByRadio(idx)); }

        //private int[] _prefArray { get; set; }

        //private int _getPrefIdx(int idx)
        //{
        //    if (_prefArray._isEmpty()) setPrefArray();
        //    return _prefArray._nth(idx, idx);
        //}

        //private void setPrefArray()
        //{
        //    int npref = _infectDataCount;
        //    if (npref > 0) {
        //        _prefArray = new int[npref];
        //        bool[] flags = new bool[npref];
        //        int n = 0;
        //        for (; n < _mainPrefNum; ++n) {
        //            _prefArray[n] = n;
        //        }
        //        if (_currentSettings != null) {
        //            var topPrefs = _currentSettings.addedPrefIdxes;
        //            if (topPrefs._notEmpty()) {
        //                for (int i = 0; i < topPrefs.Length; ++i) {
        //                    int j = topPrefs[i];
        //                    if (j >= _mainPrefNum && j < flags.Length && !flags[j]) {
        //                        _prefArray[n++] = j;
        //                        flags[j] = true;
        //                    }
        //                }
        //            }
        //        }
        //        for (int j = _mainPrefNum; j < flags.Length; ++j) {
        //            if (!flags[j]) {
        //                if (n >= _prefArray.Length) {
        //                    Console.WriteLine("ERROR");
        //                } else {
        //                    _prefArray[n++] = j;
        //                }
        //            }
        //        }
        //    }

        //}

        private async Task getSettings()
        {
            _currentSettings = (await UserSettings.GetSettings(JSRuntime, _infectDataCount)).Cleanup();
        }

        private async Task insertStaticDescription()
        {
            var html = Helper.GetFileContent("wwwroot/html/Description.html", System.Text.Encoding.UTF8);
            if (html._notEmpty()) await JSRuntime.InvokeAsync<string>("insertStaticDescription", html);
        }

        private async Task selectStaticDescription()
        {
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
                await getSettings();
                await RenderChartMethod(true, true);
                await selectStaticDescription();
                StateHasChanged();
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
            _currentSettings.radioIdx = _selectorPos;
            await RenderChartMethod();
        }

        public async Task MovePrefLeft()
        {
            if (_radioIdx >= _mainPrefNum) {
                await movePref(-1);
                // そのままだとラジオボタンの選択が反映されないので、いったん別のやつを選択し直す
                int currentRadioIdx = _radioIdx;
                _currentSettings.radioIdx = 0;
                StateHasChanged();
                _currentSettings.radioIdx = currentRadioIdx; ;
                StateHasChanged();
            }
        }

        public async Task MovePrefRight()
        {
           await movePref(1);
        }

        private async Task movePref(int delta)
        {
            _currentSettings.moveRadioPref(delta);
            _cancellable = false;
            await _currentSettings.SaveSettings();
#if DEBUG
            Console.WriteLine($"movePref: radioIdx={_radioIdx}");
#endif
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

        public async Task RenderExpectationCurveMethod(ChangeEventArgs args)
        {
            _currentSettings.setDrawExpectation((bool)args.Value);
            await RenderChartMethod();
        }

        public async Task ChangeExpectationParamDate(ChangeEventArgs args)
        {
            string dt = args.Value.ToString();
            if (dt._reMatch(@"^\d+/\d+$")) dt = $"{DateTime.Now._yyyy()}/{dt}";
            _currentSettings.setParamStartDate(dt);
            await RenderChartMethod();
        }

        public async Task ChangeExpectationParamDateDetail(ChangeEventArgs args)
        {
            string dt = args.Value.ToString();
            if (dt._reMatch(@"^\d+/\d+$")) dt = $"{DateTime.Now._yyyy()}/{dt}";
            _currentSettings.setParamStartDateFourstep(dt);
            await RenderChartMethod();
        }

        public async Task ChangeOnlyOnClick(ChangeEventArgs args)
        {
            _currentSettings.setOnlyOnClick((bool)args.Value);
            await RenderChartMethod();
        }

        public async Task ChangeExtensionDays(ChangeEventArgs args)
        {
            _currentSettings.setExtensionDays(args.Value.ToString()._parseInt(0));
            await RenderChartMethod();
        }

        public async Task ChangeUseOnForecast(ChangeEventArgs args)
        {
            _currentSettings.setUseOnForecast((bool)args.Value);
            _cancellable = false;
            await _currentSettings.SaveSettings();
        }

        public async Task ChangeRtParamDaysToOne(ChangeEventArgs args)
        {
            _currentSettings.setParamDaysToOne(Math.Min(args.Value.ToString()._parseInt(0), 100));
            await RenderChartMethod();
        }

        public async Task ChangeRtParamDecayFactor(ChangeEventArgs args)
        {
            _currentSettings.setParamDecayFactor(args.Value.ToString()._parseDouble(1000));
            await RenderChartMethod();
        }

        public async Task ChangeRtParamDaysToNext(ChangeEventArgs args)
        {
            _currentSettings.setParamDaysToNext(Math.Min(args.Value.ToString()._parseInt(RtDecayParam.DefaultParam.DaysToNext), 15));
            await RenderChartMethod();
        }

        public async Task ChangeRtParamMaxRt(ChangeEventArgs args)
        {
            _currentSettings.setParamMaxRt(args.Value.ToString()._parseDouble(RtDecayParam.DefaultParam.RtMax));
            await RenderChartMethod();
        }

        public async Task ChangeRtParamMinRt(ChangeEventArgs args)
        {
            _currentSettings.setParamMinRt(args.Value.ToString()._parseDouble(RtDecayParam.DefaultParam.RtMin));
            await RenderChartMethod();
        }

        public async Task ChangeRtParamDecayFactorNext(ChangeEventArgs args)
        {
            _currentSettings.setParamDecayFactorNext(args.Value.ToString()._parseDouble(50));
            await RenderChartMethod();
        }

        public async Task ChangeRtParamDaysToRt1(ChangeEventArgs args)
        {
            _currentSettings.setParamDaysToRt1(Math.Min(args.Value.ToString()._parseInt(RtDecayParam.DefaultParam.DaysToRt1), 100));
            await RenderChartMethod();
        }

        public async Task ChangeRtParamRt1(ChangeEventArgs args)
        {
            _currentSettings.setParamRt1(args.Value.ToString()._parseDouble(RtDecayParam.DefaultParam.Rt1));
            await RenderChartMethod();
        }

        public async Task ChangeRtParamDaysToRt2(ChangeEventArgs args)
        {
            _currentSettings.setParamDaysToRt2(Math.Min(args.Value.ToString()._parseInt(RtDecayParam.DefaultParam.DaysToRt2), 100));
            await RenderChartMethod();
        }

        public async Task ChangeRtParamRt2(ChangeEventArgs args)
        {
            _currentSettings.setParamRt2(args.Value.ToString()._parseDouble(RtDecayParam.DefaultParam.Rt2));
            await RenderChartMethod();
        }

        public async Task ChangeRtParamDaysToRt3(ChangeEventArgs args)
        {
            _currentSettings.setParamDaysToRt3(Math.Min(args.Value.ToString()._parseInt(RtDecayParam.DefaultParam.DaysToRt3), 100));
            await RenderChartMethod();
        }

        public async Task ChangeRtParamRt3(ChangeEventArgs args)
        {
            _currentSettings.setParamRt3(args.Value.ToString()._parseDouble(RtDecayParam.DefaultParam.Rt3));
            await RenderChartMethod();
        }

        public async Task ChangeRtParamDaysToRt4(ChangeEventArgs args)
        {
            _currentSettings.setParamDaysToRt4(Math.Min(args.Value.ToString()._parseInt(RtDecayParam.DefaultParam.DaysToRt4), 100));
            await RenderChartMethod();
        }

        public async Task ChangeRtParamRt4(ChangeEventArgs args)
        {
            _currentSettings.setParamRt4(args.Value.ToString()._parseDouble(RtDecayParam.DefaultParam.Rt4));
            await RenderChartMethod();
        }

        public async Task RenderEstimatedBarMethod(ChangeEventArgs args)
        {
            _currentSettings.setEstimatedBar((bool)args.Value);
            await RenderChartMethod(false, _barWidth < 0);
        }

        public async Task ShowDetailSettings(ChangeEventArgs args)
        {
            _currentSettings.setDetailSettings((bool)args.Value);
            _cancellable = false;
            await _currentSettings.SaveSettings();
        }

        public async Task ChangeEasyOrFourstepSettings(ChangeEventArgs args)
        {
            _currentSettings.setFourstepSettings(args.Value.ToString() == "fourstep");
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

        private void reloadData()
        {
            // 5秒経過後、10秒以内に再度呼び出されたら、データの再読み込みを行う
            var dtNow = DateTime.Now;
            if (dtNow > _prevReloadDt.AddSeconds(5) && dtNow < _prevReloadDt.AddSeconds(10)) {
                dailyData.Initialize();
                forecastData.Initialize();
            }
            _prevReloadDt = dtNow;
        }

        public async Task InitializeParams()
        {
            if (_cancellable) {
                await getSettings();
            } else {
                reloadData();
                _currentSettings.setParamStartDate("");
                _currentSettings.setParamDaysToOne(0);
                _currentSettings.setParamDecayFactor(0);
                _currentSettings.setParamDaysToNext(0);
                _currentSettings.setParamMaxRt(0);
                _currentSettings.setParamMinRt(0);
                _currentSettings.setParamDecayFactorNext(0);
            }
            await RenderChartMethod(false, false, !_cancellable);
        }

        public async Task InitializeFourstepParams()
        {
            if (_cancellable) {
                await getSettings();
            } else {
                _currentSettings.setParamStartDateFourstep("");
                _currentSettings.setParamDaysToRt1(0);
                _currentSettings.setParamRt1(0);
                _currentSettings.setParamDaysToRt2(0);
                _currentSettings.setParamRt2(0);
                _currentSettings.setParamDaysToRt3(0);
                _currentSettings.setParamRt3(0);
                _currentSettings.setParamDaysToRt4(0);
                _currentSettings.setParamRt4(0);
            }
            await RenderChartMethod(false, false, !_cancellable);
        }

        public async Task RenderReloadMethod()
        {
            reloadData();
            await RenderChartMethod(true, true);
        }

        /// <summary>
        /// グラフ描画メソッド。
        /// JSRuntime を介して JavaScript を呼び出している。
        /// <para>参照: https://docs.microsoft.com/ja-jp/aspnet/core/blazor/call-javascript-from-dotnet?view=aspnetcore-3.1 </para>
        /// </summary>
        /// <param name="bFirst">グラフ描画時のアニメーションの要否</param>
        /// <returns></returns>
        public async Task RenderChartMethod(bool bFirst = false, bool bResetScrollBar = false, bool bCancellable = false)
        {
            RtDecayParam rtParam = null;
            if (_drawExpectation) {
                rtParam = _currentSettings.MakeRtDecayParam(_infectData.InitialDecayParam);
                //rtParam = new RtDecayParam {
                //    UseDetail = _detailSettings,
                //    StartDate = _paramDate._parseDateTime(),
                //    StartDateDetail = _paramDateDetail._parseDateTime(),
                //    DaysToOne = _paramDaysToOne,
                //    DaysToRt1 = _paramDaysToRt1,
                //    Rt1 = _paramRt1,
                //    DaysToRt2 = _paramDaysToRt2,
                //    Rt2 = _paramRt2,
                //    DaysToRt3 = _paramDaysToRt3,
                //    Rt3 = _paramRt3,
                //    DaysToRt4 = _paramDaysToRt4,
                //    Rt4 = _paramRt4,
                //    DecayFactor = _paramDecayFactor,
                //};
            }

            (var json, int dispDays) = dailyData.MakeJsonData(_infectData, _yAxisMax, _endDate._parseDateTime(), rtParam, _extensionDays, _estimatedBar, _onlyOnClick, bFirst);

            var jsonStr = (json?.chartData)._toString();
            int barWidth = _drawExpectation && _estimatedBar && _barWidth < 0 ? 0 : _barWidth;
            double scrlbarRatio = bResetScrollBar || barWidth != _barWidth ? newlyDaysRatio(dispDays) : 0;
            _cancellable = bCancellable;
            await JSRuntime.InvokeAsync<string>("renderChart2", "chart-wrapper-home", barWidth, scrlbarRatio, jsonStr);
            if (!bFirst && !_cancellable) await _currentSettings.SaveSettings();
        }

        private double newlyDaysRatio(int dispDays)
        {
            // return (double)_infectData.Dates._safeCount() / Math.Max((_endDate._parseDateTime() - _infectData.Dates._getFirst()).TotalDays, 1);
            return dispDays > 0 ? (double)_infectData.Dates._safeCount() / dispDays : 100;
        }
    }
}
