using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

using StandardCommon;

namespace ChartBlazorApp.Models
{
    /// <summary>
    /// 最上位の予測設定値クラス
    /// </summary>
    public class EffectiveParams
    {
        private static ConsoleLog logger = ConsoleLog.GetLogger();

        private DailyData _dailyData;

        public int DebugLevel { get; set; }

        /// <summary> ユーザ設定値(Layer-5) </summary>
        public UserSettings CurrentSettings;

        public EffectiveParams(DailyData dailyData, UserSettings settings, int debugLevel = 0)
        {
            _dailyData = dailyData;
            DebugLevel = debugLevel >= 0 ? debugLevel : ConsoleLog.DEBUG_LEVEL;
            CurrentSettings = settings ?? UserSettings.CreateInitialSettings();
        }

        public static async ValueTask<EffectiveParams> CreateByGettingUserSettings(IJSRuntime jsRuntime, DailyData dailyData, int debugLevel = 0)
        {
            var settings = (await UserSettings.GetSettings(jsRuntime, dailyData.InfectDataCount, dailyData)).Cleanup();
            return new EffectiveParams(dailyData, settings, debugLevel).RenewDecaySubParams();
        }

        /// <summary> ユーザ設定から作成した予測設定値(Layer-4) </summary>
        private SubParams[] _subParamsCache = new SubParams[50];

        object syncObj = new object();

        private SubParams getOrNewDecaySubParams(int idx, bool bRenew = false)
        {
            lock (syncObj) {
                int dataIdx = myDataIdx(idx);
                if (dataIdx >= _subParamsCache.Length) {
                    _subParamsCache = _subParamsCache._extend(dataIdx + 1);
                }
                SubParams subParam = _subParamsCache[dataIdx];
                var infData = NthInfectData(idx);
                if (infData != null && (bRenew || subParam == null || subParam.UpdateDt < _dailyData.LastUpdateDt)) {
                    //int duration = CurrentSettings.localMaxRtDuration ?? -1;
                    int duration = CurrentSettings.extremeRtDetectDuration ?? -1;
                    var dt = CurrentSettings.myParamStartDate(idx)._parseDateTime();
                    int days = CurrentSettings.myParamDaysToOne(idx);
                    if (days > 0 && dt._notValid()) dt = getInitailParamStartDate(idx);
                    _subParamsCache[dataIdx] = subParam = infData.CalcDecaySubParamsEx(duration, dt._isValid() ? (DateTime?)dt : null, days, idx == -1 ? DebugLevel : 0);
                }
                if (subParam != null) return subParam;

                logger.Warn("No effective SubParams. Return new SubParams().");
                return new SubParams();
            }
        }

        public EffectiveParams RenewDecaySubParams()
        {
            getOrNewDecaySubParams(-1, true);
            return this;
        }

        public string TimeMachineData {
            get { return CurrentSettings.timeMachineData; }
            set { CurrentSettings.timeMachineData = value; }
        }

        public bool TimeMachineMode {
            get { return CurrentSettings.timeMachineMode; }
            set { CurrentSettings.timeMachineMode = value; }
        }

        // TimeMachine用の InfectData
        private InfectData _timeMachineInfectData = null;

        /// <summary>
        /// ソースデータからテスト用InfectDataを作成して、整形されたデータを保存する
        /// </summary>
        /// <param name="srcIdx"></param>
        /// <returns></returns>
        public void UseTimeMachineInfectData(string tmData)
        {
            logger.Info($"{GetTitle()}: {tmData}");
            _timeMachineInfectData = null;
            var trimData = tmData._strip()._orElse("0");
            int[] extraData = trimData._isEmpty() ? null : trimData._split(',').Select(x => x._strip()._parseInt(0)).ToArray();
            if (extraData._notEmpty() && extraData[0] < 0) extraData[0] = extraData[0]._lowLimit(-90);
            _timeMachineInfectData = NthInfectData(-1).CreateData(extraData);
            RenewDecaySubParams();
            TimeMachineData = extraData._join(",");
            TimeMachineMode = true;
        }

        /// <summary>
        /// 保存してあるタイムマシンデータからInfectDataを作成する
        /// </summary>
        public void MakeTimeMachineInfectData()
        {
            if (TimeMachineData._notEmpty()) {
                _timeMachineInfectData = null;
                _timeMachineInfectData = NthInfectData(-1).CreateData(TimeMachineData._split(',').Select(x => x._strip()._parseInt(0)).ToArray());
                RenewDecaySubParams();
            }
        }

        public void ClearTimeMachineInfectData()
        {
            _timeMachineInfectData = null;
            TimeMachineMode = false;
            RenewDecaySubParams();
        }

        public bool TimeMachineInfectDataEnabled => _timeMachineInfectData != null;

        public int TimeMachineInfectDataIdx => TimeMachineInfectDataEnabled ? InfectDataCount : -1;

        // データの数
        public int InfectDataCount => _dailyData?.InfectDataCount ?? 0;

        public int MyDataIdx => TimeMachineInfectDataEnabled ? InfectDataCount : CurrentSettings.dataIdx;

        private int myDataIdx(int idx = -1) { return idx < 0 ? MyDataIdx : idx; }

        public InfectData NthInfectData(int n) {
            if (_dailyData == null) return InfectData.DummyData;

            if ((n < 0 || n >= InfectDataCount) && _timeMachineInfectData != null) return _timeMachineInfectData;

            int myIdx = myDataIdx(n);
            var data = _dailyData.InfectDataList._nth(myIdx);
            if (data == null) {
                logger.Warn($"n={n}, myDataIdx({n})={myIdx}, DailyData.InfectDataList.Length={_dailyData.InfectDataList._length()}");
                data = InfectData.DummyData;
            }
            return data;
        }

        public InfectData MyInfectData { get { return NthInfectData(MyDataIdx); } }

        // 表示開始日
        public string DispStartDate => CurrentSettings.dispStartDate._orElse(() => DateTime.Now.AddMonths(-(6 + (DateTime.Now.Month % 3)))._toFirstInMonth()._toDateString());

        public void SetDispStartDate(string value) { CurrentSettings.dispStartDate = value; }

        // タイトル（地域名）
        public string GetTitle(int n = -1) { return NthInfectData(n).Title ?? ""; }

        /// <summary>idx は ラジオボタンのインデックス</summary>
        public string RadioPrefName(int idx) { return GetTitle(CurrentSettings.prefIdxByRadio(idx)); }

        public int RadioIdx {
            get { return CurrentSettings.radioIdx; }
            set {
                CurrentSettings.radioIdx = value;
                _timeMachineInfectData = null;
            }
        }

        public int PrefIdx {
            get { return Math.Max(CurrentSettings.prefIdx, Constants.MAIN_PREF_NUM); }
            set {
                CurrentSettings.prefIdx = value;
                _timeMachineInfectData = null;
            }
        }

        public int BarWidth { get { return CurrentSettings.barWidth; } }

        public double YAxisMax { get { var res = CurrentSettings.myYAxisMax(); return res > 0 ? res : MyInfectData.Y1_Max; } }

        public double YAxisMin { get { return CurrentSettings.yAxisMin; } }

        public double YAxis2Max { get { var res = CurrentSettings.myYAxis2Max(); return res > 0 ? res : MyInfectData.Y2_Max; } }

        public bool DrawExpectation { get { return CurrentSettings.drawExpectation; } }

        public bool EstimatedBar { get { return CurrentSettings.estimatedBar; } }

        public int EstimatedBarMinWidth { get { return CurrentSettings.estimatedBarMinWidth; } }

        public bool DrawPosiRates { get { return CurrentSettings.drawPosiRates; } }
        public bool PosiRatePercent { get { return CurrentSettings.posiRatePercent; } }

        public bool DetailSettings { get { return CurrentSettings.detailSettings; } }

        public bool FourstepSettings { get { return CurrentSettings.fourstepSettings; } }

        public bool FourstepEnabled { get { return FourstepSettings && FourstepDataValid; } }

        public bool FourstepDataValid {
            get {
                return ParamDaysToRt1 > 0 ||
                     CurrentSettings.myParamDaysToRt2() > 0 ||
                     CurrentSettings.myParamDaysToRt3() > 0 ||
                     CurrentSettings.myParamDaysToRt4() > 0;
            }
        }

        public bool FourstepDataDefault {
            get {
                return CurrentSettings.myParamDaysToRt1() > 0 ||
                     CurrentSettings.myParamDaysToRt2() > 0 ||
                     CurrentSettings.myParamDaysToRt3() > 0 ||
                     CurrentSettings.myParamDaysToRt4() > 0;
            }
        }

        public bool OnlyOnClick { get { return CurrentSettings.onlyOnClick; } }

        public bool OtherForecastCharts { get { return CurrentSettings.otherForecastCharts; } }

        public bool ThinForecastBar { get { return CurrentSettings.thinForecastBar; } }

        public bool ExpectOverReal { get { return CurrentSettings.expectOverReal; } }

        public int ExtensionDays { get { return CurrentSettings.myExtensionDays(); } }

        //public bool UseOnForecast { get { return CurrentSettings.useOnForecast; } }

        //public int LocalMaxRtDuration { get { return CurrentSettings.myLocalMaxRtDuration(); } }

        public int ExtremeRtDetectDuration { get { return CurrentSettings.myExtremeRtDetectDuration(); } }

        public bool UseDateForChangePoint { get { return CurrentSettings.useDateForChangePoint; } }

        public bool UsePostDecayRt1 { get { return CurrentSettings.usePostDecayRt1; } }

        public double PostDecayFactorRt2 { get { return CurrentSettings.postDecayFactorRt2._gtZeroOr(Constants.PostDecayFactorRt2); } }
        public double getPostDecayFactorRt2(bool bSystem) { return bSystem ? Constants.PostDecayFactorRt2 : PostDecayFactorRt2; }
        public void SetPostDecayFactorRt2(string value)
        {
            CurrentSettings.setPostDecayFactorRt2(value._isEmpty() ? Constants.PostDecayFactorRt2 : value._parseDouble(0)._gtZeroOr(0.0000001));
        }

        public string Events => getEvents();
        public string getEvents(int idx = -1)
        {
            var evt = CurrentSettings.myEvents(idx);
            if (evt._notEmpty()) return evt;
            return NthInfectData(idx).Events;
        }

        public string ParamStartDate { get { return getParamStartDate(); } }
        public string getParamStartDate(int idx = -1, bool bSystem = false) {
            if (!bSystem && DetailSettings) {
                var dt = CurrentSettings.myParamStartDate(idx);
                if (dt._parseDateTime()._isValid()) return dt;
                if (dt._notEmpty()) logger.Warn($"CurrentSettings.myParamStartDate({idx})={dt} is invalid.");
                dt = getOrNewDecaySubParams(idx).StartDate;
                if (dt._parseDateTime()._isValid()) return dt;
                if (dt._notEmpty()) logger.Warn($"getOrNewDecaySubParams({idx}).StartDate={dt} is invalid.");
            }
            return getInitailParamStartDate(idx)._toDateString();
        }
        private DateTime getInitailParamStartDate(int idx)
        {
            var dt = NthInfectData(idx).InitialDecayParam.StartDate;
            if (dt._isValid()) return dt;

            dt = NthInfectData(idx).InitialSubParams.StartDate._parseDateTime();
            if (dt._isValid()) return dt;

            logger.Info($"RtDecayParam.DefaultParam.StartDat={dt._toDateString()} is returned.");
            return RtDecayParam.DefaultParam.StartDate;
        }

        public string ParamStartDateFourstepStr { get { return getParamStartDateFourstepStr(); } }
        public string getParamStartDateFourstepStr(int idx = -1) {
            var dt = CurrentSettings.myParamStartDateFourstep(idx);
            if (dt._notEmpty()) return dt;

            return NthInfectData(idx).Dates.Last()._toDateString();
            //var startDt = NthInfectData(idx).InitialDecayParam.StartDateFourstep;
            //if (startDt._isValid()) return startDt._toDateString();

            //return RtDecayParam.DefaultParam.StartDateFourstep._toDateString();
        }

        //public string DefaultDaysToOne { get { var v = CurrentSettings.myParamDaysToOne(); return v > 0 ? "" : $"({NthInfectData(idx).InitialDecayParam.DaysToOne})"; } }

        public int ParamDaysToOne { get { return getParamDaysToOne(); } }
        public int getParamDaysToOne(int idx = -1, bool bSystem = false) {
            if (!bSystem && DetailSettings) {
                int days = CurrentSettings.myParamDaysToOne(idx)._gtZeroOr(() => getOrNewDecaySubParams(idx).DaysToOne);
                if (days > 0) return days;
            }
            return NthInfectData(idx).InitialDecayParam.DaysToOne.
                _gtZeroOr(() => NthInfectData(idx).InitialSubParams.DaysToOne).
                _gtZeroOr(() => RtDecayParam.DefaultParam.DaysToOne);
        }

        public string getParamDateOnOne(int idx = -1, bool bSystem = false)
        {
            return getParamStartDate(idx, bSystem)._parseDateTime().AddDays(getParamDaysToOne(idx, bSystem))._toDateString();
        }

        public double ParamDecayFactor { get { return getParamDecayFactor(); } }
        public double getParamDecayFactor(int idx = -1, bool bSystem = false) {
            if (!bSystem && DetailSettings) {
                var factor = CurrentSettings.myParamDecayFactor(idx)._neZeroOr(() => getOrNewDecaySubParams(idx).DecayFactor);
                if (factor != 0) return factor;
            }
            return NthInfectData(idx).InitialDecayParam.DecayFactor.
                _neZeroOr(() => NthInfectData(idx).InitialSubParams.DecayFactor).
                _neZeroOr(() => RtDecayParam.DefaultParam.DecayFactor);
        }

        public int ParamDaysToNext { get { return getParamDaysToNext(); } }
        public int getParamDaysToNext(int idx = 1, bool bSystem = false) {
            if (!bSystem && DetailSettings) {
                var days = CurrentSettings.myParamDaysToNext(idx);
                if (days > 0) return days;
            }
            return NthInfectData(idx).InitialDecayParam.DaysToNext._gtZeroOr(() => RtDecayParam.DefaultParam.DaysToNext);
        }

        public double ParamDecayFactorNext { get { return getParamDecayFactorNext(); } }
        public double getParamDecayFactorNext(int idx = -1, bool bSystem = false) {
            if (!bSystem && DetailSettings) {
                var factor = CurrentSettings.myParamDecayFactorNext(idx)._neZeroOr(() => getOrNewDecaySubParams(idx).DecayFactorNext);
                if (factor != 0) return factor;
            }
            return NthInfectData(idx).InitialDecayParam.DecayFactorNext.
                _neZeroOr(() => NthInfectData(idx).InitialSubParams.DecayFactorNext).
                _neZeroOr(() => RtDecayParam.DefaultParam.DecayFactorNext);
        }

        public double ParamMaxRt { get { return getParamMaxRt(); } }
        public double getParamMaxRt(int idx = -1) {
            return CurrentSettings.myParamMaxRt(idx).
                _gtZeroOr(() => NthInfectData(idx).InitialDecayParam.RtMax).
                _gtZeroOr(() => RtDecayParam.DefaultParam.RtMax);
        }

        public double ParamMinRt { get { return getParamMinRt(); } }
        public double getParamMinRt(int idx = -1) {
                return CurrentSettings.myParamMinRt(idx).
                _gtZeroOr(() => NthInfectData(idx).InitialDecayParam.RtMin).
                _gtZeroOr(() => RtDecayParam.DefaultParam.RtMin);
        }

        public double ParamEasyRt1 { get { return getParamEasyRt1();} }
        public double getParamEasyRt1(int idx = -1, bool bSystem = false) {
            double rt = 0;
            if (!bSystem && DetailSettings) { rt = CurrentSettings.myParamEasyRt1(idx)._gtZeroOr(() => getOrNewDecaySubParams(idx).Rt1); }
            return rt._gtZeroOr(() => NthInfectData(idx).InitialDecayParam.EasyRt1).
                _gtZeroOr(() => NthInfectData(idx).InitialSubParams.Rt1).
                _gtZeroOr(() => RtDecayParam.DefaultParam.EasyRt1);
        }

        public double ParamEasyRt2 { get { return getParamEasyRt2();} }
        public double getParamEasyRt2(int idx = -1, bool bSystem = false) {
            double rt = 0;
            if (!bSystem && DetailSettings) { rt = CurrentSettings.myParamEasyRt2(idx)._gtZeroOr(() => getOrNewDecaySubParams(idx).Rt2); }
            return rt._gtZeroOr(() => NthInfectData(idx).InitialDecayParam.EasyRt2).
                _gtZeroOr(() => NthInfectData(idx).InitialSubParams.Rt2).
                _gtZeroOr(() => RtDecayParam.DefaultParam.EasyRt2);
        }

        public int ParamDaysToRt1 { get { return getParamDaysToRt1(); } }
        public int getParamDaysToRt1(int idx = -1) {
            return CurrentSettings.myParamDaysToRt1(idx)._neZeroOr(10);
            //    return CurrentSettings.myParamDaysToRt1(idx).
            //_gtZeroOr(() => NthInfectData(idx).InitialDecayParam.DaysToRt1).
            //_gtZeroOr(() => RtDecayParam.DefaultParam.DaysToRt1);
        }

        public double ParamRt1 { get { return getParamRt1(); } }
        public double getParamRt1(int idx = -1) {
            return CurrentSettings.myParamRt1(idx).
                _gtZeroOr(() => NthInfectData(idx).InitialDecayParam.Rt1).
                _gtZeroOr(() => RtDecayParam.DefaultParam.Rt1);
        }

        public int ParamDaysToRt2 { get { return getParamDaysToRt2(); } }
        public int getParamDaysToRt2(int idx = -1) {
            return CurrentSettings.myParamDaysToRt2(idx);
            //return CurrentSettings.myParamDaysToRt2(idx).
            //    _neZeroOr(() => NthInfectData(idx).InitialDecayParam.DaysToRt2).
            //    _neZeroOr(() => RtDecayParam.DefaultParam.DaysToRt2);
        }

        public double ParamRt2 { get { return getParamRt2(); } }
        public double getParamRt2(int idx = -1) {
            return CurrentSettings.myParamRt2(idx).
                _gtZeroOr(() => NthInfectData(idx).InitialDecayParam.Rt2).
                _gtZeroOr(() => RtDecayParam.DefaultParam.Rt2);
        }

        public int ParamDaysToRt3 { get { return getParamDaysToRt3(); } }
        public int getParamDaysToRt3(int idx = -1) {
            return CurrentSettings.myParamDaysToRt3(idx);
            //return CurrentSettings.myParamDaysToRt3(idx).
            //    _neZeroOr(() => NthInfectData(idx).InitialDecayParam.DaysToRt3).
            //    _neZeroOr(() => RtDecayParam.DefaultParam.DaysToRt3);
        }

        public double ParamRt3 { get { return getParamRt3(); } }
        public double getParamRt3(int idx = -1) {
            return CurrentSettings.myParamRt3(idx).
                _gtZeroOr(() => NthInfectData(idx).InitialDecayParam.Rt3).
                _gtZeroOr(() => RtDecayParam.DefaultParam.Rt3);
        }

        public int ParamDaysToRt4 { get { return getParamDaysToRt4(); } }
        public int getParamDaysToRt4(int idx = -1) {
            return CurrentSettings.myParamDaysToRt4(idx);
            //return CurrentSettings.myParamDaysToRt4(idx).
            //    _neZeroOr(() => NthInfectData(idx).InitialDecayParam.DaysToRt4).
            //    _neZeroOr(() => RtDecayParam.DefaultParam.DaysToRt4);
        }

        public double ParamRt4 { get { return getParamRt4();}}
        public double getParamRt4(int idx = -1) {
            return CurrentSettings.myParamRt4(idx).
                _gtZeroOr(() => NthInfectData(idx).InitialDecayParam.Rt4).
                _gtZeroOr(() => RtDecayParam.DefaultParam.Rt4);
        }

        public int FavorPrefNum { get { return CurrentSettings.favorPrefNum; } }

        public int SelectorPos { get { return CurrentSettings.selectorRadioPos; } }

        public RtDecayParam MakeRtDecayParam(bool bSystem, int idx = -1)
        {
            return new RtDecayParam {
                //UseOnForecast = UseOnForecast,
                PostDecayFactorRt2 = getPostDecayFactorRt2(bSystem),
                Fourstep = DetailSettings && FourstepEnabled,
                StartDate = getParamStartDate(idx, bSystem)._parseDateTime(),
                StartDateFourstep = getParamStartDateFourstepStr(idx)._parseDateTime(),
                DaysToOne = getParamDaysToOne(idx, bSystem),
                DecayFactor = getParamDecayFactor(idx, bSystem),
                DaysToNext = getParamDaysToNext(idx, bSystem),
                RtMax = getParamMaxRt(idx),
                RtMin = getParamMinRt(idx),
                EasyRt1 = getParamEasyRt1(idx, bSystem),
                EasyRt2 = getParamEasyRt2(idx, bSystem),
                DecayFactorNext = getParamDecayFactorNext(idx, bSystem),
                DaysToRt1 = getParamDaysToRt1(idx),
                Rt1 = getParamRt1(idx),
                DaysToRt2 = getParamDaysToRt2(idx),
                Rt2 = getParamRt2(idx),
                DaysToRt3 = getParamDaysToRt3(idx),
                Rt3 = getParamRt3(idx),
                DaysToRt4 = getParamDaysToRt4(idx),
                Rt4 = getParamRt4(idx),
            };
        }

        public void SetLocalMaxRtDuration(string value)
        {
            CurrentSettings.setLocalMaxRtDuration(value._parseInt(-1));
            RenewDecaySubParams();
        }

        public void SetExtremeRtDetectDuration(string value)
        {
            CurrentSettings.setExtremeRtDetectDuration(value._parseInt(-1));
            RenewDecaySubParams();
        }

        public void SetExtensionDays(string value)
        {
            CurrentSettings.setExtensionDays(value._parseInt(0));
        }

        public void SetParamDaysToOne(string value)
        {
            CurrentSettings.setParamDaysToOne(Math.Min(value._parseInt(0), 300));
        }

        public void SetParamDecayFactor(string value)
        {
            CurrentSettings.setParamDecayFactor(value._parseDouble(1000));
        }

        public void SetParamDaysToNext(string value)
        {
            CurrentSettings.setParamDaysToNext(Math.Min(value._parseInt(0), 15));
        }

        public void SetParamMaxRt(string value)
        {
            CurrentSettings.setParamMaxRt(value._parseDouble(0));
        }

        public void SetParamMinRt(string value)
        {
            CurrentSettings.setParamMinRt(value._parseDouble(0));
        }

        public void SetParamEasyRt1(string value)
        {
            CurrentSettings.setParamEasyRt1(value._parseDouble(0));
        }

        public void SetParamEasyRt2(string value)
        {
            CurrentSettings.setParamEasyRt2(value._parseDouble(0));
        }

        public void SetParamDecayFactorNext(string value)
        {
            CurrentSettings.setParamDecayFactorNext(value._parseDouble(50));
        }

        public void SetParamDaysToRt1(string value)
        {
            CurrentSettings.setParamDaysToRt1(Math.Min(value._parseInt(0), 300));
        }

        public void SetParamRt1(string value)
        {
            CurrentSettings.setParamRt1(value._parseDouble(0));
        }

        public void SetParamDaysToRt2(string value)
        {
            CurrentSettings.setParamDaysToRt2(Math.Min(value._parseInt(0), 300));
        }

        public void SetParamRt2(string value)
        {
            CurrentSettings.setParamRt2(value._parseDouble(0));
        }

        public void SetParamRt3(string value)
        {
            CurrentSettings.setParamRt3(value._parseDouble(0));
        }

        public void SetParamDaysToRt3(string value)
        {
            CurrentSettings.setParamDaysToRt3(Math.Min(value._parseInt(0), 300));
        }

        public void SetParamRt4(string value)
        {
            CurrentSettings.setParamRt4(value._parseDouble(0));
        }

        public void SetParamDaysToRt4(string value)
        {
            CurrentSettings.setParamDaysToRt4(Math.Min(value._parseInt(0), 300));
        }

        public void SetFourstepSettings(string value)
        {
            CurrentSettings.setFourstepSettings(value == "fourstep");
        }

    }
}
