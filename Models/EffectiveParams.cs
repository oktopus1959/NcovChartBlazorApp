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
        /// <summary>
        /// 最も基底となる予測設定値(Layer-1)
        /// </summary>
        /// <returns></returns>
        //public static RtDecayParam DefaultRtParam = RtDecayParam.CreateDefaultParam();

        public DailyData DailyData;

        /// <summary> ユーザ設定値(Layer-5) </summary>
        public UserSettings CurrentSettings;

        public EffectiveParams(DailyData dailyData, UserSettings settings)
        {
            DailyData = dailyData;
            CurrentSettings = settings ?? UserSettings.CreateInitialSettings();
        }

        public static async ValueTask<EffectiveParams> CreateByGettingUserSettings(IJSRuntime jsRuntime, DailyData dailyData)
        {
            var settings = (await UserSettings.GetSettings(jsRuntime, dailyData.InfectDataCount, dailyData)).Cleanup();
            return new EffectiveParams(dailyData, settings).RenewDecaySubParams();
        }

        /// <summary> ユーザ設定から作成した予測設定値(Layer-4) </summary>
        private SubParams?[] _subParamsCache = new SubParams?[50];

        object syncObj = new object();

        private SubParams getOrNewDecaySubParams(int idx, bool bRenew = false)
        {
            lock (syncObj) {
                int dataIdx = myDataIdx(idx);
                if (dataIdx >= _subParamsCache.Length) {
                    _subParamsCache = _subParamsCache._extend(dataIdx + 1);
                }
                SubParams? subParam = _subParamsCache[dataIdx];
                if (bRenew || subParam == null) {
                    var data = DailyData?.InfectDataList._nth(dataIdx);
                    if (data != null) {
                        int duration = CurrentSettings.localMaxRtDuration ?? -1;
                        var dt = CurrentSettings.myParamStartDate(dataIdx)._parseDateTime();
                        _subParamsCache[dataIdx] = subParam = data.CalcDecaySubParams(duration, dt._isValid() ? (DateTime?)dt : null);
                    }
                }
                return subParam.HasValue ? subParam.Value : new SubParams();
            }
        }

        public EffectiveParams RenewDecaySubParams()
        {
            getOrNewDecaySubParams(-1, true);
            return this;
        }

        // データの数
        public int InfectDataCount { get { return DailyData?.InfectDataCount ?? 0; } }

        public int MyDataIdx { get { return CurrentSettings.dataIdx; } }

        private int myDataIdx(int idx = -1) { return idx < 0 ? MyDataIdx : idx; }

        public InfectData NthInfectData(int n) { return DailyData?.InfectDataList._nth(myDataIdx(n)) ?? InfectData.DummyData; }

        public InfectData MyInfectData { get { return NthInfectData(MyDataIdx); } }

        // タイトル（地域名）
        public string GetTitle(int n) { return (DailyData?.InfectDataList)._nth(n)?.Title ?? ""; }

        /// <summary>idx は ラジオボタンのインデックス</summary>
        public string RadioPrefName(int idx) { return GetTitle(CurrentSettings.prefIdxByRadio(idx)); }

        public int RadioIdx { get { return CurrentSettings.radioIdx; } set { CurrentSettings.radioIdx = value; } }

        public int PrefIdx { get { return Math.Max(CurrentSettings.prefIdx, UserSettings.MainPrefNum); } set { CurrentSettings.prefIdx = value; } }

        public int BarWidth { get { return CurrentSettings.barWidth; } }

        public double YAxisMax { get { var res = CurrentSettings.myYAxisMax(); return res > 0 ? res : MyInfectData.Y1_Max; } }

        public bool DrawExpectation { get { return CurrentSettings.drawExpectation; } }

        public bool EstimatedBar { get { return CurrentSettings.estimatedBar; } }

        public int EstimatedBarMinWidth { get { return CurrentSettings.estimatedBarMinWidth; } }

        public bool DetailSettings { get { return CurrentSettings.detailSettings; } }

        public bool FourstepSettings { get { return CurrentSettings.fourstepSettings; } }

        public bool OnlyOnClick { get { return CurrentSettings.onlyOnClick; } }

        public int ExtensionDays { get { return CurrentSettings.myExtensionDays(); } }

        //public bool UseOnForecast { get { return CurrentSettings.useOnForecast; } }

        public int LocalMaxRtDuration { get { return CurrentSettings.myLocalMaxRtDuration(); } }

        public string ParamStartDate { get { return getParamStartDate(); } }
        public string getParamStartDate(int idx = -1) {
            if (DetailSettings) {
                var dt = CurrentSettings.myParamStartDate(idx)._orElse(() => getOrNewDecaySubParams(idx).StartDate);
                if (dt._notEmpty()) return dt;
            }
            var startDt = NthInfectData(idx).InitialDecayParam.StartDate;
            if (startDt._isValid()) return startDt._toDateString();

            return NthInfectData(idx).InitialSubParams.StartDate._orElse(() => RtDecayParam.DefaultParam.StartDate._toDateString());
        }

        public string ParamStartDateFourstepStr { get { return getParamStartDateFourstepStr(); } }
        public string getParamStartDateFourstepStr(int idx = -1) {
            var dt = CurrentSettings.myParamStartDateFourstep(idx);
            if (dt._notEmpty()) return dt;

            var startDt = NthInfectData(idx).InitialDecayParam.StartDateFourstep;
            if (startDt._isValid()) return startDt._toDateString();

            return RtDecayParam.DefaultParam.StartDateFourstep._toDateString();
        }

        //public string DefaultDaysToOne { get { var v = CurrentSettings.myParamDaysToOne(); return v > 0 ? "" : $"({NthInfectData(idx).InitialDecayParam.DaysToOne})"; } }

        public int ParamDaysToOne { get { return getParamDaysToOne(); } }
        public int getParamDaysToOne(int idx = -1) {
            if (DetailSettings) {
                int days = CurrentSettings.myParamDaysToOne(idx)._gtZeroOr(() => getOrNewDecaySubParams(idx).DaysToOne);
                if (days > 0) return days;
            }
            return NthInfectData(idx).InitialDecayParam.DaysToOne.
                _gtZeroOr(() => NthInfectData(idx).InitialSubParams.DaysToOne).
                _gtZeroOr(() => RtDecayParam.DefaultParam.DaysToOne);
        }

        public double ParamDecayFactor { get { return getParamDecayFactor(); } }
        public double getParamDecayFactor(int idx = -1) {
            if (DetailSettings) {
                var factor = CurrentSettings.myParamDecayFactor(idx)._neZeroOr(() => getOrNewDecaySubParams(idx).DecayFactor);
                if (factor != 0) return factor;
            }
            return NthInfectData(idx).InitialDecayParam.DecayFactor.
                _neZeroOr(() => NthInfectData(idx).InitialSubParams.DecayFactor).
                _neZeroOr(() => RtDecayParam.DefaultParam.DecayFactor);
        }

        public int ParamDaysToNext { get { return getParamDaysToNext(); } }
        public int getParamDaysToNext(int idx = 1) {
            if (DetailSettings) {
                var days = CurrentSettings.myParamDaysToNext(idx);
                if (days > 0) return days;
            }
            return NthInfectData(idx).InitialDecayParam.DaysToNext._gtZeroOr(() => RtDecayParam.DefaultParam.DaysToNext);
        }

        public double ParamDecayFactorNext { get { return getParamDecayFactorNext(); } }
        public double getParamDecayFactorNext(int idx = -1) {
            if (DetailSettings) {
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
        public double getParamEasyRt1(int idx = -1) {
            double rt = 0;
            if (DetailSettings) { rt = CurrentSettings.myParamEasyRt1(idx)._gtZeroOr(() => getOrNewDecaySubParams(idx).Rt1); }
            return rt._gtZeroOr(() => NthInfectData(idx).InitialDecayParam.EasyRt1).
                _gtZeroOr(() => NthInfectData(idx).InitialSubParams.Rt1).
                _gtZeroOr(() => RtDecayParam.DefaultParam.EasyRt1);
        }

        public double ParamEasyRt2 { get { return getParamEasyRt2();} }
        public double getParamEasyRt2(int idx = -1) {
            double rt = 0;
            if (DetailSettings) { rt = CurrentSettings.myParamEasyRt2(idx)._gtZeroOr(() => getOrNewDecaySubParams(idx).Rt2); }
            return rt._gtZeroOr(() => NthInfectData(idx).InitialDecayParam.EasyRt2).
                _gtZeroOr(() => NthInfectData(idx).InitialSubParams.Rt2).
                _gtZeroOr(() => RtDecayParam.DefaultParam.EasyRt2);
        }

        public int ParamDaysToRt1 { get { return getParamDaysToRt1(); } }
        public int getParamDaysToRt1(int idx = -1) {
            return CurrentSettings.myParamDaysToRt1(idx).
                _gtZeroOr(() => NthInfectData(idx).InitialDecayParam.DaysToRt1).
                _gtZeroOr(() => RtDecayParam.DefaultParam.DaysToRt1);
        }

        public double ParamRt1 { get { return getParamRt1(); } }
        public double getParamRt1(int idx = -1) {
            return CurrentSettings.myParamRt1(idx).
                _gtZeroOr(() => NthInfectData(idx).InitialDecayParam.Rt1).
                _gtZeroOr(() => RtDecayParam.DefaultParam.Rt1);
        }

        public int ParamDaysToRt2 { get { return getParamDaysToRt2(); } }
        public int getParamDaysToRt2(int idx = -1) {
            return CurrentSettings.myParamDaysToRt2(idx).
                _gtZeroOr(() => NthInfectData(idx).InitialDecayParam.DaysToRt2).
                _gtZeroOr(() => RtDecayParam.DefaultParam.DaysToRt2);
        }

        public double ParamRt2 { get { return getParamRt2(); } }
        public double getParamRt2(int idx = -1) {
            return CurrentSettings.myParamRt2(idx).
                _gtZeroOr(() => NthInfectData(idx).InitialDecayParam.Rt2).
                _gtZeroOr(() => RtDecayParam.DefaultParam.Rt2);
        }

        public int ParamDaysToRt3 { get { return getParamDaysToRt3(); } }
        public int getParamDaysToRt3(int idx = -1) {
            return CurrentSettings.myParamDaysToRt3(idx).
                _gtZeroOr(() => NthInfectData(idx).InitialDecayParam.DaysToRt3).
                _gtZeroOr(() => RtDecayParam.DefaultParam.DaysToRt3);
        }

        public double ParamRt3 { get { return getParamRt3(); } }
        public double getParamRt3(int idx = -1) {
            return CurrentSettings.myParamRt3(idx).
                _gtZeroOr(() => NthInfectData(idx).InitialDecayParam.Rt3).
                _gtZeroOr(() => RtDecayParam.DefaultParam.Rt3);
        }

        public int ParamDaysToRt4 { get { return getParamDaysToRt4(); } }
        public int getParamDaysToRt4(int idx = -1) {
            return CurrentSettings.myParamDaysToRt4(idx).
                _gtZeroOr(() => NthInfectData(idx).InitialDecayParam.DaysToRt4).
                _gtZeroOr(() => RtDecayParam.DefaultParam.DaysToRt4);
        }

        public double ParamRt4 { get { return getParamRt4();}}
        public double getParamRt4(int idx = -1) {
            return CurrentSettings.myParamRt4(idx).
                _gtZeroOr(() => NthInfectData(idx).InitialDecayParam.Rt4).
                _gtZeroOr(() => RtDecayParam.DefaultParam.Rt4);
        }

        public int FavorPrefNum { get { return CurrentSettings.favorPrefNum; } }

        public int SelectorPos { get { return CurrentSettings.selectorRadioPos; } }

        public RtDecayParam MakeRtDecayParam(int idx = -1)
        {
            return new RtDecayParam {
                //UseOnForecast = UseOnForecast,
                Fourstep = FourstepSettings,
                StartDate = getParamStartDate(idx)._parseDateTime(),
                StartDateFourstep = getParamStartDateFourstepStr(idx)._parseDateTime(),
                DaysToOne = getParamDaysToOne(idx),
                DecayFactor = getParamDecayFactor(idx),
                DaysToNext = getParamDaysToNext(idx),
                RtMax = getParamMaxRt(idx),
                RtMin = getParamMinRt(idx),
                EasyRt1 = getParamEasyRt1(idx),
                EasyRt2 = getParamEasyRt2(idx),
                DecayFactorNext = getParamDecayFactorNext(idx),
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

        public void SetExtensionDays(string value)
        {
            CurrentSettings.setExtensionDays(value._parseInt(0));
        }

        public void SetParamDaysToOne(string value)
        {
            CurrentSettings.setParamDaysToOne(Math.Min(value._parseInt(0), 100));
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
            CurrentSettings.setParamDaysToRt1(Math.Min(value._parseInt(0), 100));
        }

        public void SetParamRt1(string value)
        {
            CurrentSettings.setParamRt1(value._parseDouble(0));
        }

        public void SetParamDaysToRt2(string value)
        {
            CurrentSettings.setParamDaysToRt2(Math.Min(value._parseInt(0), 100));
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
            CurrentSettings.setParamDaysToRt3(Math.Min(value._parseInt(0), 100));
        }

        public void SetParamRt4(string value)
        {
            CurrentSettings.setParamRt4(value._parseDouble(0));
        }

        public void SetParamDaysToRt4(string value)
        {
            CurrentSettings.setParamDaysToRt4(Math.Min(value._parseInt(0), 100));
        }

        public void SetFourstepSettings(string value)
        {
            CurrentSettings.setFourstepSettings(value == "fourstep");
        }

    }
}
