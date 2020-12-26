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

        //private UserSettings _currentSettings = UserSettings.CreateInitialSettings();
        private EffectiveParams _effectiveParams = new EffectiveParams(null, null);

        // データの数
        public int _infectDataCount { get { return _effectiveParams.InfectDataCount; } }

        // タイトル（地域名）
        private string _getTitle(int n) { return _effectiveParams.GetTitle(n); }

        // 変化度1
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

        // 変化度2
        public static int[] _decayFactors2 = new int[] {
            -4,     // -4
            -3,     // -3
            -2,     // -2
            -1,     // -1
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

        public static int _decayFactors2Start = -4;

        public static double GetDecayFactor(double indexOrFactor, int defaultFactor = 0)
        {
            return getDecayFactor(_decayFactors, indexOrFactor, defaultFactor);
        }

        public static double GetDecayFactor2(double indexOrFactor, int defaultFactor = 0)
        {
            return getDecayFactor(_decayFactors2, indexOrFactor - _decayFactors2Start, defaultFactor);
        }

        private static double getDecayFactor(int[] decayFactors, double indexOrFactor, int defaultFactor)
        {
            if (indexOrFactor < 0) return defaultFactor;
            if (indexOrFactor >= 0 && indexOrFactor < decayFactors.Length) return decayFactors[(int)indexOrFactor];
            return indexOrFactor;
        }

        private bool _drawExpectationChecked = false;

        private bool _cancellable = false;

        private static string _predictBackDt { get; set; }

#if DEBUG
        private int _debugLevel {
            get { return _effectiveParams?.DebugLevel ?? Constants.DEBUG_LEVEL; }
            set { if (_effectiveParams != null) _effectiveParams.DebugLevel = value; }
        }
#else
        private int _debugLevel = 0;
#endif

        private InfectData _infectData { get { return _effectiveParams.MyInfectData; } }

        private int _radioIdx { get { return _effectiveParams.RadioIdx; } }

        private int _prefIdx { get { return Math.Max(_effectiveParams.PrefIdx, _mainPrefNum); } }

        private int _dataIdx { get { return _effectiveParams.MyDataIdx; } }

        private int _barWidth { get { return _effectiveParams.BarWidth; } }

        private double _yAxisMax { get { return _effectiveParams.YAxisMax; } }

        private bool _drawExpectation { get { bool flag = _effectiveParams.DrawExpectation; if (flag) _drawExpectationChecked = true; return flag; } }

        private bool _estimatedBar { get { return _effectiveParams.EstimatedBar; } }

        private int _estimatedBarMinWidth { get { return _effectiveParams.EstimatedBarMinWidth; } }

        private bool _detailSettings { get { return _effectiveParams.DetailSettings; } }

        private bool _fourstepSettings { get { return _effectiveParams.FourstepSettings; } }

        private bool _onlyOnClick { get { return _effectiveParams.OnlyOnClick; } }

        private bool _expectOverReal { get { return _effectiveParams.ExpectOverReal; } }

        private int _extensionDays { get { return _effectiveParams.ExtensionDays; } }

        //private bool _useOnForecast { get { return _effectiveParams.UseOnForecast; } }

        private int _localMaxRtDuration { get { return _effectiveParams.LocalMaxRtDuration; } }

        //private string _paramDate { get { return _effectiveParams.MyParamStartDate()._orElse(() => _infectData.FindRecentMaxRtDateStr(_localMaxRtDuration))._orElse(() => _infectData.InitialDecayParam.StartDate._toDateString()); } }
        private string _paramDate { get { return _effectiveParams.ParamStartDate; } }

        private string _paramDateFourstep { get { return _effectiveParams.ParamStartDateFourstepStr; } }

        //private string _defaultDaysToOne { get { return _effectiveParams.DefaultDaysToOne; } }

        private int _paramDaysToOne { get { return _effectiveParams.ParamDaysToOne; } }

        private double _paramDecayFactor { get { return _effectiveParams.ParamDecayFactor; } }

        private int _paramDaysToNext { get { return _effectiveParams.ParamDaysToNext; } }

        private double _paramDecayFactorNext { get { return _effectiveParams.ParamDecayFactorNext; } }

        private double _paramRtMax { get { return _effectiveParams.ParamMaxRt; } }

        private double _paramRtMin { get { return _effectiveParams.ParamMinRt; } }

        private double _paramEasyRt1 { get { return _effectiveParams.ParamEasyRt1; } }

        private double _paramEasyRt2 { get { return _effectiveParams.ParamEasyRt2; } }

        private int _paramDaysToRt1 { get { return _effectiveParams.ParamDaysToRt1; } }

        private double _paramRt1 { get { return _effectiveParams.ParamRt1; } }

        private int _paramDaysToRt2 { get { return _effectiveParams.ParamDaysToRt2; } }

        private double _paramRt2 { get { return _effectiveParams.ParamRt2; } }

        private int _paramDaysToRt3 { get { return _effectiveParams.ParamDaysToRt3; } }

        private double _paramRt3 { get { return _effectiveParams.ParamRt3; } }

        private int _paramDaysToRt4 { get { return _effectiveParams.ParamDaysToRt4; } }

        private double _paramRt4 { get { return _effectiveParams.ParamRt4; } }

        private int _favorPrefNum { get { return _effectiveParams.FavorPrefNum; } }

        private int _selectorPos { get { return _effectiveParams.SelectorPos; } }

        /// <summary>idx は ラジオボタンのインデックス</summary>
        private string _radioPrefName(int idx) { return _effectiveParams.RadioPrefName(idx); }


        private async Task getSettings()
        {
            ConsoleLog.Info($"[MyChart.getSettings] CALLED");
            _effectiveParams = await EffectiveParams.CreateByGettingUserSettings(JSRuntime, dailyData, _debugLevel);
        }

        //private async Task insertStaticDescription()
        //{
        //    var html = Helper.GetFileContent("wwwroot/html/Description.html", System.Text.Encoding.UTF8);
        //    if (html._notEmpty()) await JSRuntime.InvokeAsync<string>("insertStaticDescription", html);
        //}

        private async Task selectStaticDescription()
        {
            await JSRuntime._selectDescription("home-page");
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
                ConsoleLog.Info($"[MyChart.OnAfterRenderAsync] CALLED");
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

            //infectData = dailyData.InfectDataList._getNth(_effectiveParams.dataIdx);
            //_approxDayPos = (infectData?.Dates)._safeCount() - 1;
            //_lastDate = infectData?.Dates.Last() ?? DateTime.MinValue;
            //await getSettings();
            //await RenderChartMethod(true, true);
        }

        public async Task ChangeDebugLevel(ChangeEventArgs args)
        {
            _debugLevel = args.Value.ToString()._parseInt();
            await RenderChartMethod();
        }

        public async Task ChangeChart(ChangeEventArgs args)
        {
            _effectiveParams.RadioIdx = args.Value.ToString()._parseInt();
            await RenderChartMethod();
        }

        public async Task ChangePref(ChangeEventArgs args)
        {
            _effectiveParams.PrefIdx = args.Value.ToString()._parseInt();
            _effectiveParams.RadioIdx = _selectorPos;
            await RenderChartMethod();
        }

        public async Task MovePrefLeft()
        {
            if (_radioIdx > _mainPrefNum || _radioIdx == _selectorPos) {
                movePref(-1);
                await _effectiveParams.CurrentSettings.SaveSettings();
                // そのままだとラジオボタンの選択が反映されないので、いったん別のやつを選択し直す
                int currentRadioIdx = _radioIdx;
                _effectiveParams.RadioIdx = 0;
                StateHasChanged();
                _effectiveParams.RadioIdx = currentRadioIdx; ;
                StateHasChanged();
            }
        }

        public async Task MovePrefRight()
        {
            if (_radioIdx >= _mainPrefNum && _radioIdx < _selectorPos) {
                movePref(1);
                await _effectiveParams.CurrentSettings.SaveSettings();
            }
        }

        private void movePref(int delta)
        {
            _effectiveParams.CurrentSettings.moveRadioPref(delta);
            _cancellable = false;
            ConsoleLog.Debug($"[MyChart.movePref] radioIdx={_radioIdx}");
        }

        public async Task ChangeYAxisMax(ChangeEventArgs args)
        {
            _effectiveParams.CurrentSettings.changeYAxisMax(args.Value.ToString()._parseInt(0));
            await RenderChartMethod();
        }

        public async Task RenderExpectationCurveMethod(ChangeEventArgs args)
        {
            _effectiveParams.CurrentSettings.setDrawExpectation((bool)args.Value);
            await RenderChartMethod(false, true);
        }

        /// <summary> 基準日<summary>
        public async Task ChangeExpectationParamDate(ChangeEventArgs args)
        {
            string dt = args.Value.ToString().Trim();
            if (dt._reMatch(@"^\d+/\d+$")) dt = $"{DateTime.Now._yyyy()}/{dt}";
            dt = getValidStartDt(dt);
            await changeRtParamCustom(dt, "2020/1/1",
                _effectiveParams.CurrentSettings.setParamStartDate,
                () => _effectiveParams.RenewDecaySubParams(),
                () => _effectiveParams.CurrentSettings.myParamStartDate(),
                _effectiveParams.CurrentSettings.setParamStartDate);
        }

        /// <summary>4段階の基準日<summary>
        public async Task ChangeExpectationParamDateForestep(ChangeEventArgs args)
        {
            string dt = args.Value.ToString().Trim();
            if (dt._reMatch(@"^\d+/\d+$")) dt = $"{DateTime.Now._yyyy()}/{dt}";
            dt = getValidStartDt(dt);
            await changeRtParamCustom(dt, "2020/1/1",
                _effectiveParams.CurrentSettings.setParamStartDateFourstep,
                null,
                () => _effectiveParams.CurrentSettings.myParamStartDateFourstep(),
                _effectiveParams.CurrentSettings.setParamStartDateFourstep);
        }

        private string getValidStartDt(string dts)
        {
            if (dts._isEmpty()) return dts;
            DateTime dt = dts._parseDateTime();
            var firstDt = _infectData.Dates._first();
            var lastDt = _infectData.Dates._last();
            if (dt._isValid() && dt >= firstDt && dt <= lastDt) return dts;
            ConsoleLog.Warn($"[MyChart.getValidStartDt] StartDate({dts}) is invalid. Use empty value instead.");
            return "";
        }

        public async Task ChangeOnlyOnClick(ChangeEventArgs args)
        {
            _effectiveParams.CurrentSettings.setOnlyOnClick((bool)args.Value);
            await RenderChartMethod();
        }

        public async Task ChangeExpOverReal(ChangeEventArgs args)
        {
            _effectiveParams.CurrentSettings.setExpectOverReal((bool)args.Value);
            await RenderChartMethod();
        }

        /// <summary>予想曲線表示日数<summary>
        public async Task ChangeExtensionDays(ChangeEventArgs args)
        {
            await changeRtParam(args, 9999,
                _effectiveParams.SetExtensionDays,
                () => _effectiveParams.CurrentSettings.myExtensionDays(),
                _effectiveParams.CurrentSettings.setExtensionDays);
        }

        public async Task ChangePredBackDays(ChangeEventArgs args)
        {
#if DEBUG
            string dts = args.Value.ToString().Trim();
            if (dts._reMatch(@"^\d+/\d+$"))
                dts = $"{DateTime.Now._yyyy()}/{dts}";
            else if (dts._reMatch(@"^\d+$"))
                dts = $"{DateTime.Now.ToString("yyyy/MM")}/{dts}";
            //dts = getValidStartDt(dts);
            _predictBackDt = dts;
            var dt = dts._parseDateTime();
            if (dt._isValid()) {
                dailyData.Initialize(true, dt);
                forecastData.Initialize();
                await RenderChartMethod();
            }
#endif
        }

        //public async Task ChangeUseOnForecast(ChangeEventArgs args)
        //{
        //    _effectiveParams.CurrentSettings.setUseOnForecast((bool)args.Value);
        //    _cancellable = false;
        //    await _effectiveParams.CurrentSettings.SaveSettings();
        //}

        /// <summary> 基準日検出遡及日数 </summary>
        public async Task ChangeLocalMaxRtDuration(ChangeEventArgs args)
        {
            await changeRtParam(args, 9999,
                _effectiveParams.SetLocalMaxRtDuration,
                () => _effectiveParams.CurrentSettings.myLocalMaxRtDuration(),
                _effectiveParams.CurrentSettings.setLocalMaxRtDuration);
        }

        /// <summary> 目標Rtになるまでの日数</summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public async Task ChangeRtParamDaysToOne(ChangeEventArgs args)
        {
            await changeRtParam(args, 9999,
                _effectiveParams.SetParamDaysToOne,
                () => _effectiveParams.CurrentSettings.myParamDaysToOne(),
                _effectiveParams.CurrentSettings.setParamDaysToOne);
        }

        public async Task ChangeRtParamDecayFactor(ChangeEventArgs args)
        {
            _effectiveParams.SetParamDecayFactor(args.Value.ToString());
            await RenderChartMethod();
        }

        public async Task ChangeRtParamDaysToNext(ChangeEventArgs args)
        {
            await changeRtParam(args, 9999,
                _effectiveParams.SetParamDaysToNext,
                () => _effectiveParams.CurrentSettings.myParamDaysToNext(),
                _effectiveParams.CurrentSettings.setParamDaysToNext);
        }

        public async Task ChangeRtParamMaxRt(ChangeEventArgs args)
        {
            await changeRtParam(args, 999,
                _effectiveParams.SetParamMaxRt,
                () => _effectiveParams.CurrentSettings.myParamMaxRt(),
                _effectiveParams.CurrentSettings.setParamMaxRt);
        }

        public async Task ChangeRtParamMinRt(ChangeEventArgs args)
        {
            await changeRtParam(args, 999,
                _effectiveParams.SetParamMinRt,
                () => _effectiveParams.CurrentSettings.myParamMinRt(),
                _effectiveParams.CurrentSettings.setParamMinRt);
        }

        /// <summary> 最初の目標Rt</summary>
        public async Task ChangeRtParamEasyRt1(ChangeEventArgs args)
        {
            await changeRtParam(args, 999,
                _effectiveParams.SetParamEasyRt1,
                () => _effectiveParams.CurrentSettings.myParamEasyRt1(),
                _effectiveParams.CurrentSettings.setParamEasyRt1);
        }

        /// <summary> 次の目標Rt</summary>
        public async Task ChangeRtParamEasyRt2(ChangeEventArgs args)
        {
            await changeRtParam(args, 999,
                _effectiveParams.SetParamEasyRt2,
                () => _effectiveParams.CurrentSettings.myParamEasyRt2(),
                _effectiveParams.CurrentSettings.setParamEasyRt2);
        }

        public async Task ChangeRtParamDecayFactorNext(ChangeEventArgs args)
        {
            _effectiveParams.SetParamDecayFactorNext(args.Value.ToString());
            await RenderChartMethod();
        }

        /// <summary>4段階：Rt1への日数<summary>
        public async Task ChangeRtParamDaysToRt1(ChangeEventArgs args)
        {
            await changeRtParam(args, 9999,
                _effectiveParams.SetParamDaysToRt1,
                () => _effectiveParams.CurrentSettings.myParamDaysToRt1(),
                _effectiveParams.CurrentSettings.setParamDaysToRt1);
        }

        public async Task ChangeRtParamRt1(ChangeEventArgs args)
        {
            await changeRtParam(args, 999,
                _effectiveParams.SetParamRt1,
                () => _effectiveParams.CurrentSettings.myParamRt1(),
                _effectiveParams.CurrentSettings.setParamRt1);
        }

        /// <summary>4段階：Rt2への日数<summary>
        public async Task ChangeRtParamDaysToRt2(ChangeEventArgs args)
        {
            await changeRtParam(args, 9999,
                _effectiveParams.SetParamDaysToRt2,
                () => _effectiveParams.CurrentSettings.myParamDaysToRt2(),
                _effectiveParams.CurrentSettings.setParamDaysToRt2);
        }

        public async Task ChangeRtParamRt2(ChangeEventArgs args)
        {
            await changeRtParam(args, 999,
                _effectiveParams.SetParamRt2,
                () => _effectiveParams.CurrentSettings.myParamRt2(),
                _effectiveParams.CurrentSettings.setParamRt2);
        }

        /// <summary>4段階：Rt3への日数<summary>
        public async Task ChangeRtParamDaysToRt3(ChangeEventArgs args)
        {
            await changeRtParam(args, 9999,
                _effectiveParams.SetParamDaysToRt3,
                () => _effectiveParams.CurrentSettings.myParamDaysToRt3(),
                _effectiveParams.CurrentSettings.setParamDaysToRt3);
        }

        public async Task ChangeRtParamRt3(ChangeEventArgs args)
        {
            await changeRtParam(args, 999,
                _effectiveParams.SetParamRt3,
                () => _effectiveParams.CurrentSettings.myParamRt3(),
                _effectiveParams.CurrentSettings.setParamRt3);
        }

        /// <summary>4段階：Rt4への日数<summary>
        public async Task ChangeRtParamDaysToRt4(ChangeEventArgs args)
        {
            await changeRtParam(args, 9999,
                _effectiveParams.SetParamDaysToRt4,
                () => _effectiveParams.CurrentSettings.myParamDaysToRt4(),
                _effectiveParams.CurrentSettings.setParamDaysToRt4);
        }

        public async Task ChangeRtParamRt4(ChangeEventArgs args)
        {
            await changeRtParam(args, 999,
                _effectiveParams.SetParamRt4,
                () => _effectiveParams.CurrentSettings.myParamRt4(),
                _effectiveParams.CurrentSettings.setParamRt4);
        }

        private async Task changeRtParam<T>(ChangeEventArgs args, T outVal, Action<string> mySetter, Func<T> getter, Action<T> setter)
        {
            var val = args.Value.ToString();
            mySetter(val);
            await RenderChartMethod();
            var newval = getter();
            if (val._isEmpty()) {
                setter(outVal);
                StateHasChanged();
                setter(newval);
                StateHasChanged();
            }
        }

        private async Task changeRtParamCustom<T>(T value, T outVal, Action<T> mySetter, Action custom, Func<T> getter, Action<T> setter) where T: IComparable
        {
            var oldval = getter();
            mySetter(value);
            custom?.Invoke();
            await RenderChartMethod();
            var newval = getter();
            if (newval.CompareTo(oldval) == 0) {
                setter(outVal);
                StateHasChanged();
                setter(newval);
                StateHasChanged();
            }
        }

        private async Task changeRtParamCustom2(ChangeEventArgs args, int outVal, Action<string> mySetter, Action custom, Func<int> getter, Action<int> setter)
        {
            var oldval = getter();
            mySetter(args.Value.ToString());
            custom?.Invoke();
            await RenderChartMethod();
            var newval = getter();
            if (newval.CompareTo(oldval) == 0) {
                setter(outVal);
                StateHasChanged();
                setter(newval);
                StateHasChanged();
            }
        }

        public async Task RenderEstimatedBarMethod(ChangeEventArgs args)
        {
            _effectiveParams.CurrentSettings.setEstimatedBar((bool)args.Value);
            await RenderChartMethod(false, _barWidth < _estimatedBarMinWidth);
        }

        public async Task RenderEstimatedBarMinWidthMethod(ChangeEventArgs args)
        {
            _effectiveParams.CurrentSettings.setEstimatedBarMinWidth(args.Value.ToString()._parseInt());
            await RenderChartMethod(false, _barWidth < _estimatedBarMinWidth);
        }

        public async Task ShowDetailSettings(ChangeEventArgs args)
        {
            _effectiveParams.CurrentSettings.setDetailSettings((bool)args.Value);
            _cancellable = false;
            await RenderChartMethod();
        }

        public async Task ChangeEasyOrFourstepSettings(ChangeEventArgs args)
        {
            _effectiveParams.SetFourstepSettings(args.Value.ToString());
            await RenderChartMethod();
        }

        public async Task RenderThickChartBarMethod()
        {
            _effectiveParams.CurrentSettings.changeBarWidth(1);
            await RenderChartMethod(false, true);
        }

        public async Task RenderThinChartBarMethod()
        {
            _effectiveParams.CurrentSettings.changeBarWidth(-1);
            await RenderChartMethod(false, true);
        }

        private DateTime _prevReloadDt = DateTime.MinValue;

        private void reloadData(bool bForce = false)
        {
            ConsoleLog.Info($"[MyChart.reloadData] CALLED");
            // 5秒経過後、10秒以内に再度呼び出されたら、データの再読み込みを行う
            var dtNow = DateTime.Now;
            if (bForce || (dtNow > _prevReloadDt.AddSeconds(5) && dtNow < _prevReloadDt.AddSeconds(10))) {
                ConsoleLog.Info($"[MyChart.reloadData] CALL dailyData.Initialize(true)");
                dailyData.Initialize(true);
                forecastData.Initialize();
            }
            _prevReloadDt = dtNow;
        }

        public async Task InitializeParams()
        {
            ConsoleLog.Info($"[MyChart.InitializeParams] CALLED");
            if (_cancellable) {
                await getSettings();
            } else {
#if DEBUG
                reloadData(true);
#else
                reloadData();
#endif
                _effectiveParams.CurrentSettings.setParamStartDate("");
                _effectiveParams.CurrentSettings.setParamDaysToOne(0);
                _effectiveParams.CurrentSettings.setParamDecayFactor(0);
                _effectiveParams.CurrentSettings.setParamDaysToNext(0);
                _effectiveParams.CurrentSettings.setParamMaxRt(0);
                _effectiveParams.CurrentSettings.setParamMinRt(0);
                _effectiveParams.CurrentSettings.setParamEasyRt1(0);
                _effectiveParams.CurrentSettings.setParamEasyRt2(0);
                _effectiveParams.CurrentSettings.setParamDecayFactorNext(0);
                _effectiveParams.RenewDecaySubParams();
            }
            await RenderChartMethod(false, false, !_cancellable);
        }

        public async Task InitializeFourstepParams()
        {
            ConsoleLog.Info($"[MyChart.InitializeFourstepParams] CALLED");
            if (_cancellable) {
                await getSettings();
            } else {
                _effectiveParams.CurrentSettings.setParamStartDateFourstep("");
                _effectiveParams.CurrentSettings.setParamDaysToRt1(0);
                _effectiveParams.CurrentSettings.setParamRt1(0);
                _effectiveParams.CurrentSettings.setParamDaysToRt2(0);
                _effectiveParams.CurrentSettings.setParamRt2(0);
                _effectiveParams.CurrentSettings.setParamDaysToRt3(0);
                _effectiveParams.CurrentSettings.setParamRt3(0);
                _effectiveParams.CurrentSettings.setParamDaysToRt4(0);
                _effectiveParams.CurrentSettings.setParamRt4(0);
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
            RtDecayParam rtParam = _effectiveParams.MakeRtDecayParam();
            int extDays = _drawExpectation || _drawExpectationChecked ? _extensionDays : Math.Min(_extensionDays, 5);  // 予想曲線を表示したことがないときは、余分な表示日は5日とする
            int barWidth = _drawExpectation && _estimatedBar ? Math.Max(_barWidth, _estimatedBarMinWidth) : _barWidth;
            bool bTailPadding = !_estimatedBar || barWidth > -4;
            (var jsonStr, int dispDays) = MakeJsonData(rtParam, extDays, bTailPadding, bFirst);

            double scrlbarRatio = bResetScrollBar || barWidth != _barWidth ? newlyDaysRatio(dispDays) : 0;
            _cancellable = bCancellable;
            await JSRuntime._renderChart2("chart-wrapper-home", barWidth, scrlbarRatio, jsonStr);

            if (!bFirst && !_cancellable) await _effectiveParams.CurrentSettings.SaveSettings();
        }

        private double newlyDaysRatio(int dispDays)
        {
            return dispDays > 0 ? (double)_infectData.Dates._safeCount() / dispDays : 100;
        }

        /// <summary>
        /// Chart描画のためのJsonデータを構築して返す
        /// </summary>
        /// <param name="dataIdx">描画対象(0:全国/1:東京/...)</param>
        /// <param name="endDate">グラフ表示最終日</param>
        /// <param name="aheadParamIdx">事前作成予想データインデックス(負なら使わない)</param>
        /// <param name="bAnimation">グラフアニメーションの有無</param>
        /// <returns></returns>
        public (string, int) MakeJsonData(RtDecayParam rtDecayParam, int extensionDays, bool bTailPadding = true, bool bAnimation = false)
        {
            ChartJson chartData = null;
            UserPredictData predData = null;
            string title = "";
            int dispDays = 0;       // 予想日含め、実際に表示する日付数

            InfectData infData = _infectData;
            DateTime endDate = _endDate._parseDateTime();

            if (infData != null) {
                title = infData.Title;
                var rtp = rtDecayParam;
                ConsoleLog.Info(
                    $"[MyChart.MakeJsonData] " +
                    $"pref={title}, " +
                    $"exp={_drawExpectation}, " +
                    $"det={_detailSettings}, " +
                    $"est={_estimatedBar}, " +
                    $"rtDur={_localMaxRtDuration}, " +
                    $"expDt={rtp?.EffectiveStartDate.ToShortDateString()}, " +
                    $"days={(rtp?.Fourstep == false ? rtp.DaysToOne.ToString() : "")}, " +
                    $"rt1={rtp?.EasyRt1}, ft1={rtp?.DecayFactor}, " +
                    $"rt2={rtp?.EasyRt2}, ft1={rtp?.DecayFactorNext}, " +
                    $"days1/Rt1={(rtp?.Fourstep == true ? $"{rtp.DaysToRt1}/{rtp.Rt1}" : "")}");
                //int x0 = infData.X0;
                chartData = new ChartJson { type = "bar" };
                //var borderDash = new double[] { 10, 3 };
                var fullDates = infData.Dates.Select(x => x._toShortDateString()).ToList();     // 全表示日(1年間とか)の日付文字列を格納する (ここではまだ実データの日付のみ)
                var firstDate = infData.Dates._first();    // 実データの開始日
                var lastDate = infData.Dates._last();      // 実データの最終日
                if (firstDate._isValid() && lastDate._isValid()) {
                    if (endDate._isValid() && endDate > lastDate) {
                        // endDate: 最大想定表示最終日 (例: 2021/05/31)
                        int days = Math.Min((endDate - lastDate).Days, 400);
                        fullDates.AddRange(Enumerable.Range(1, days).Select(d => lastDate.AddDays(d)._toShortDateString()));    // ここで endDate まで表示日付(候補含む)を拡張しておく
                    }
                    int realDays = (lastDate - firstDate).Days + 1;
                    //int predDays = realDays + 21;
                    int predDays = realDays + extensionDays;
                    if (_drawExpectation) {
                        predData = UserPredictData.PredictValuesEx(infData, rtDecayParam, fullDates._length(), extensionDays);
                        predDays = predData.PredDays;
                    }
                    //dispDays = Math.Max(realDays + 21, predDays) + 1;
                    dispDays = Math.Max(realDays + extensionDays, predDays) + 1;
                    if (predData != null) predData.DispDays = dispDays;

                    (double y1_max, double y1_step, double y2_max, double y2_step) = (infData.Y1_Max, infData.Y1_Step, infData.Y2_Max, infData.Y2_Step);
                    double yMax = _yAxisMax;
                    if (yMax > 0) {
                        (y1_max, y1_step) = (yMax, yMax / 10);
                    }
                    Options options = Options.CreateTwoAxes(new Ticks(y1_max, y1_step), new Ticks(y2_max, y2_step));
                    if (!bAnimation) options.AnimationDuration = 0;
                    if (bTailPadding) options.legend.SetAlignEnd();
                    options.legend.reverse = true;  // 凡例の表示を登録順とは逆順にする
                     options.SetOnlyClickEvent(_onlyOnClick);
                    chartData.options = options;

                    var dataSets = new List<Dataset>();
#if DEBUG
                    //if (predData != null) {
                    //    dataSets.Add(Dataset.CreateDotLine("逆算移動平均", predData.RevAverage.Take(predData.DispDays)._toNullableArray(1), "darkblue"));
                    //    //dataSets.Add(Dataset.CreateDotLine2("逆算Rt", predData.RevRt.Take(predData.DispDays)._toNullableArray(1), "crimson"));
                    //}
#endif
                    if (bTailPadding) { dataSets.Add(Dataset.CreateLine("                ", new double?[fullDates.Count], "rgba(0,0,0,0)", "rgba(0,0,0,0)")); }
                    double?[] predRts = null;
                    double?[] predAverage = null;
                    if (predData != null) {
                        int dashLineOrder = _expectOverReal ? 1 : 3;
                        predRts = predData.FullPredRt.Take(predDays)._toNullableArray(3);
                        dataSets.Add(Dataset.CreateDashLine2("予想実効再生産数", predRts, "brown").SetOrder(dashLineOrder).SetDispOrder(6));
                        predAverage = predData.PredAverage.Take(predDays)._toNullableArray(1);
                        dataSets.Add(Dataset.CreateDashLine("予想移動平均", predAverage, "darkgreen").SetOrder(dashLineOrder).SetDispOrder(4));
                    }
                    int lineOrder = _expectOverReal ? 3 : 1;
                    double?[] realRts = infData.Rt.Take(realDays)._toNullableArray(3);
                    dataSets.Add(Dataset.CreateLine2("実効再生産数(右軸)", realRts, "darkorange", "yellow").SetOrder(lineOrder).SetDispOrder(5));
                    double?[] realAverage = infData.Average.Take(realDays)._toNullableArray(1);
                    dataSets.Add(Dataset.CreateLine("陽性者数移動平均", realAverage, "darkblue", "lightblue").SetOrder(lineOrder).SetDispOrder(3));
                    double?[] positives = infData.Newly.Take(realDays)._toNullableArray(0, 0);
                    var positiveDataset = Dataset.CreateBar("新規陽性者数", positives, "royalblue").SetHoverColors("mediumblue").SetDispOrder(1);
                    if (predData != null && _estimatedBar) {
                        options.tooltips.intersect = false;
                        options.tooltips.SetCustomHighest();
                        dataSets.Add(positiveDataset);
                        dataSets.Add(Dataset.CreateBar("推計陽性者数", predData.PredNewly.Take(predDays)._toNullableArray(0), "darkgray").SetHoverColors("darkseagreen").SetDispOrder(2));
                    } else {
                        options.AddStackedAxis();
                        options.tooltips.SetCustomFixed();
                        dataSets.Add(positiveDataset.SetStackedAxisId());
                        //double?[] dummyBars = calcDummyData(predDays, positives, realAverage, predAverage, realRts, predRts, y1_max, y2_max);
                        double?[] dummyBar = positives.Select(v => y1_max - (v ?? 0)).ToArray()._extend(predDays, y1_max)._toNullableArray(0, 0);
                        dataSets.Add(Dataset.CreateBar("", dummyBar, "rgba(0,0,0,0)").SetStackedAxisId().SetHoverColors("rgba(10,10,10,0.1)").SetDispOrder(100));
                        //double?[] dummyBars = Dataset.CalcDummyData(predDays,
                        //    new double?[][] { positives }, new double?[][] { realAverage, predAverage }, new double?[][] { realRts, predRts },
                        //    y1_max, y2_max);
                        //dataSets.Add(Dataset.CreateBar("", dummyBars, "rgba(0,0,0,0)").SetStackedAxisId().SetHoverColors("rgba(10,10,10,0.1)").SetDispOrder(100));
                    }
                    double?[] rt1Line = new double?[fullDates.Count];
                    rt1Line[0] = rt1Line[^1] = 1.0;
                    var rtBaseline = Dataset.CreateLine2("RtBaseline", rt1Line, "grey", null).SetOrders(100, 100);
                    rtBaseline.borderWidth = 1.2;
                    rtBaseline.pointRadius = 0;
                    rtBaseline.spanGaps = true;
                    dataSets.Add(rtBaseline);

                    chartData.data = new Data {
                        labels = fullDates.Take(dispDays).ToArray(),
                        datasets = dataSets.ToArray(),
                    };
                }
            }
            return (chartData._toString(), dispDays);
        }

    }
}
