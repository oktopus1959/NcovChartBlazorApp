﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using System.Text.Json;
using StandardCommon;

namespace ChartBlazorApp.Models
{
    public class UserSettings
    {
        private static ConsoleLog logger = ConsoleLog.GetLogger();

        private const int MainPrefNum = Constants.MAIN_PREF_NUM;

        // ---- ここから下が LocalStorage に保存される (すべてのメンバーを public かつ { get; set; } にしておくこと)

        public bool _oldValuesCopied { get; set; }

        /// <summary> グラフ表示開始日 </summary>
        public string dispStartDate { get; set; }

        public int radioIdx { get; set; }
        /// <summary> select で表示している道府県のインデックス </summary>
        public int prefIdx { get; set; }
        public int[] favorPrefIdxes { get; set; }

        public int barWidth { get; set; }
        public int lastBarWidthRange { get; set; }

        public int yAxisFixed { get; set; }
        public int yAxis2SeriousFixed { get; set; }

        public int[] yAxisMax { get; set; }
        public double[] yAxis2Max { get; set; }

        public bool drawExpectation { get; set; }
        public bool estimatedBar { get; set; }
        public int estimatedBarMinWidth { get; set; }
        public bool drawPosiRates { get; set; }
        public bool posiRatePercent { get; set; }
        public bool drawComplements { get; set; }
        public bool detailSettings { get; set; }
        public bool fourstepSettings { get; set; }
        public bool onlyOnClick { get; set; }
        public bool expectOverReal { get; set; }
        public int extensionDays { get; set; }
        //public bool useOnForecast { get; set; }
        public int? localMaxRtDuration { get; set; }
        public int? extremeRtDetectDuration { get; set; }
        public bool useDateForChangePoint { get; set; }
        public bool usePostDecayRt1 { get; set; }
        public double postDecayFactorRt2 { get; set; }
        public bool otherForecastCharts { get; set; }
        public bool thinForecastBar { get; set; }
        public bool forecastFullWidthCharts { get; set; }
        public bool forecastExpandChartDates { get; set; }

        public string[] paramRtStartDate { get; set; }
        public string[] paramRtStartDateFourstep { get; set; }
        public int[] paramRtDaysToOne { get; set; }
        public double[] paramRtDecayFactor { get; set; }
        public int[] paramRtDaysToNext { get; set; }
        public double[] paramRtMaxRt { get; set; }
        public double[] paramRtMinRt { get; set; }
        public double[] paramRtEasyRt1 { get; set; }
        public double[] paramRtEasyRt2 { get; set; }
        public double[] paramRtDecayFactorNext { get; set; }

        public int[] paramRtDaysToRt1 { get; set; }
        public double[] paramRtRt1 { get; set; }
        public int[] paramRtDaysToRt2 { get; set; }
        public double[] paramRtRt2 { get; set; }
        public int[] paramRtDaysToRt3 { get; set; }
        public double[] paramRtRt3 { get; set; }
        public int[] paramRtDaysToRt4 { get; set; }
        public double[] paramRtRt4 { get; set; }
        public int[] paramRtDaysToRt5 { get; set; }
        public double[] paramRtRt5 { get; set; }
        public int[] paramRtDaysToRt6 { get; set; }
        public double[] paramRtRt6 { get; set; }
        /// <summary> 多段階設定の保存用</summary>
        public string[] paramRtMultiSave { get; set; }

        /// <summary> アノテーション </summary>
        public string[] events { get; set; }

        /// <summary> タイムマシンデータ </summary>
        public string timeMachineData { get; set; }

        public bool timeMachineMode { get; set; }

        public bool adminFlag { get; set; }

        // ---- 上のところまでが LocalStorage に保存される

        public int favorPrefNum {
            get {
                if (_favorPrefNum < 0) countFavorPref();
                return Math.Max(_favorPrefNum, 0);
            }
        }

        private int _favorPrefNum = -1;

        private void countFavorPref()
        {
            _favorPrefNum = favorPrefIdxes?.Count(n => n > 0) ?? -1;
        }

        public int selectorRadioPos { get { return MainPrefNum + favorPrefNum; } }

        public int prefIdxByRadio(int radioIdx)
        {
            if (radioIdx < MainPrefNum) return radioIdx;
            int i = radioIdx - MainPrefNum;
            return i < favorPrefNum ? favorPrefIdxes._nth(i) : prefIdx;
        }

        private IJSRuntime JSRuntime { get; set; }

        public UserSettings SetJsRuntime(IJSRuntime jsRuntime)
        {
            JSRuntime = jsRuntime;
            return this;
        }

        public static UserSettings CreateInitialSettings()
        {
            return new UserSettings().Initialize(1);
        }

        public UserSettings Initialize(int numData)
        {
            dispStartDate = "";
            radioIdx = 0;
            prefIdx = MainPrefNum;
            favorPrefIdxes = new int[Constants.FAVORITE_PREF_MAX];
            barWidth = 0;
            lastBarWidthRange = 0;
            yAxisFixed = 0;
            yAxis2SeriousFixed = 0;
            yAxisMax = new int[numData];
            yAxis2Max = new double[numData];
            drawExpectation = false;
            estimatedBar = false;
            estimatedBarMinWidth = 0;
            drawPosiRates = false;
            posiRatePercent = false;
            drawComplements = false;
            detailSettings = false;
            fourstepSettings = false;
            onlyOnClick = false;
            expectOverReal = false;
            extensionDays = 0;
            //useOnForecast = false;
            localMaxRtDuration = null;
            extremeRtDetectDuration = null;
            useDateForChangePoint = false;
            usePostDecayRt1 = false;
            postDecayFactorRt2 = 0;
            thinForecastBar = false;
            forecastFullWidthCharts = false;
            forecastExpandChartDates = false;
            otherForecastCharts = false;
            paramRtStartDate = new string[numData];
            paramRtStartDateFourstep = new string[numData];
            paramRtDaysToOne = new int[numData];
            paramRtDecayFactor = new double[numData];
            paramRtDaysToNext = new int[numData];
            paramRtMaxRt = new double[numData];
            paramRtMinRt = new double[numData];
            paramRtEasyRt1 = new double[numData];
            paramRtEasyRt2 = new double[numData];
            paramRtDecayFactorNext = new double[numData];
            paramRtDaysToRt1 = new int[numData];
            paramRtRt1 = new double[numData];
            paramRtDaysToRt2 = new int[numData];
            paramRtRt2 = new double[numData];
            paramRtDaysToRt3 = new int[numData];
            paramRtRt3 = new double[numData];
            paramRtDaysToRt4 = new int[numData];
            paramRtRt4 = new double[numData];
            paramRtDaysToRt5 = new int[numData];
            paramRtRt5 = new double[numData];
            paramRtDaysToRt6 = new int[numData];
            paramRtRt6 = new double[numData];
            paramRtMultiSave = new string[numData];
            events = new string[numData];
            timeMachineData = "";
            timeMachineMode = false;
            adminFlag = false;
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

        private const string SettingsKey = Constants.SETTINGS_KEY;

        public class MyException : Exception
        {
            public MyException(string msg) : base(msg) { }
        }

        /// <summary>
        /// ブラウザの LocalStorage から値を取得
        /// </summary>
        /// <returns></returns>
        private static async ValueTask<UserSettings> getLocalStorage(IJSRuntime jsRuntime, int numData)
        {
            var jsonStr = await jsRuntime._getSettings();
            if (jsonStr._isEmpty()) throw new MyException("Settings is empty");
            logger.Debug(() => $"Load json len={jsonStr.Length}");
            return jsonStr._jsonDeserialize<UserSettings>().SetJsRuntime(jsRuntime).fillEmptyValues(numData);
        }

        /// <summary>
        /// 保存しておいた設定を取得する
        /// </summary>
        /// <returns></returns>
        public static async ValueTask<UserSettings> GetSettings(IJSRuntime jsRuntime, int numData, DailyData dailyData)
        {
            try {
                return await getLocalStorage(jsRuntime, numData);
            } catch (Exception e) {
                if (e is MyException) {
                    logger.Warn($"settings read failed: {e.Message}");
                } else {
                    logger.Error($"settings read failed.\n{e}");
                }
                logger.Info($"Use initial settings instead.");
                return (new UserSettings().SetJsRuntime(jsRuntime)).Initialize(numData);
            }
        }

        /// <summary>
        /// ブラウザの LocalStorage に値を保存
        /// </summary>
        /// <param name="key"></param>
        public async Task SaveSettings()
        {
            try {
                string jsonStr = this._jsonSerialize();
                logger.Debug(() => $"Save json len={jsonStr.Length}");
                await JSRuntime._saveSettings(jsonStr);
            } catch (Exception e) {
                logger.Error($"settings write failed.\n{e}");
            }
        }

        public UserSettings fillEmptyValues(int numData)
        {
            yAxisMax = _extendArray(yAxisMax, numData);
            yAxis2Max = _extendArray(yAxis2Max, numData);
            favorPrefIdxes = _extendArray(favorPrefIdxes, Constants.FAVORITE_PREF_MAX);
            paramRtStartDate = _extendArray(paramRtStartDate, numData);
            paramRtStartDateFourstep = _extendArray(paramRtStartDateFourstep, numData);
            paramRtDaysToOne = _extendArray(paramRtDaysToOne, numData);
            paramRtDecayFactor = _extendArray(paramRtDecayFactor, numData);
            paramRtDaysToNext = _extendArray(paramRtDaysToNext, numData);
            paramRtMaxRt = _extendArray(paramRtMaxRt, numData);
            paramRtMinRt = _extendArray(paramRtMinRt, numData);
            paramRtEasyRt1 = _extendArray(paramRtEasyRt1, numData);
            paramRtDecayFactorNext = _extendArray(paramRtDecayFactorNext, numData);
            paramRtDaysToRt1 = _extendArray(paramRtDaysToRt1, numData);
            paramRtRt1 = _extendArray(paramRtRt1, numData);
            paramRtDaysToRt2 = _extendArray(paramRtDaysToRt2, numData);
            paramRtRt2 = _extendArray(paramRtRt2, numData);
            paramRtDaysToRt3 = _extendArray(paramRtDaysToRt3, numData);
            paramRtRt3 = _extendArray(paramRtRt3, numData);
            paramRtDaysToRt4 = _extendArray(paramRtDaysToRt4, numData);
            paramRtRt4 = _extendArray(paramRtRt4, numData);
            paramRtDaysToRt5 = _extendArray(paramRtDaysToRt5, numData);
            paramRtRt5 = _extendArray(paramRtRt5, numData);
            paramRtDaysToRt6 = _extendArray(paramRtDaysToRt6, numData);
            paramRtRt6 = _extendArray(paramRtRt6, numData);
            paramRtMultiSave = _extendArray(paramRtMultiSave, numData);
            events = _extendArray(events, numData);

            if (_oldValuesCopied) {
                logger.Debug($"Old value already copied");
                paramRtEasyRt2 = _extendArray(paramRtEasyRt2, numData);
            } else {
                paramRtEasyRt2 = new double[paramRtMinRt.Length];
                Array.Copy(paramRtMinRt, paramRtEasyRt2, paramRtMinRt.Length);
                _oldValuesCopied = true;
                logger.Info($"Old value now copied");
            }
            return this;
        }

        public int myExtensionDays() { return extensionDays._gtZeroOr(Constants.EXTENSION_DAYS); }
        //public int myLocalMaxRtDuration() { return localMaxRtDuration._geZeroOr(Constants.LOCAL_MAX_RT_BACK_DURATION); }
        public int myExtremeRtDetectDuration() { return extremeRtDetectDuration._geZeroOr(Constants.EXTREMUM_DETECTION_DURATION); }

        public int dataIdx { get { return prefIdxByRadio(radioIdx); } }

        public int myYAxisMax(int idx = -1) { return yAxisMax._getNth(idx >= 0 ? idx : dataIdx); }
        public double myYAxis2Max(int idx = -1) { return yAxis2Max._getNth(idx >= 0 ? idx : dataIdx); }
        public string myParamStartDate(int idx = -1) { return paramRtStartDate._getNth(idx >= 0 ? idx : dataIdx); }
        public string myParamStartDateFourstep(int idx = -1) { return paramRtStartDateFourstep._getNth(idx >= 0 ? idx : dataIdx); }
        public int myParamDaysToOne(int idx = -1) { return paramRtDaysToOne._getNth(idx >= 0 ? idx : dataIdx); }
        public double myParamDecayFactor(int idx = -1) { return paramRtDecayFactor._getNth(idx >= 0 ? idx : dataIdx); }
        public int myParamDaysToNext(int idx = -1) { return paramRtDaysToNext._getNth(idx >= 0 ? idx : dataIdx); }
        public double myParamMaxRt(int idx = -1) { return paramRtMaxRt._getNth(idx >= 0 ? idx : dataIdx); }
        public double myParamMinRt(int idx = -1) { return paramRtMinRt._getNth(idx >= 0 ? idx : dataIdx); }
        public double myParamEasyRt1(int idx = -1) { return paramRtEasyRt1._getNth(idx >= 0 ? idx : dataIdx); }
        public double myParamEasyRt2(int idx = -1) { return paramRtEasyRt2._getNth(idx >= 0 ? idx : dataIdx); }
        public double myParamDecayFactorNext(int idx = -1) { return paramRtDecayFactorNext._getNth(idx >= 0 ? idx : dataIdx); }
        public int myParamDaysToRt1(int idx = -1) { return paramRtDaysToRt1._getNth(idx >= 0 ? idx : dataIdx); }
        public double myParamRt1(int idx = -1) { return paramRtRt1._getNth(idx >= 0 ? idx : dataIdx); }
        public int myParamDaysToRt2(int idx = -1) { return paramRtDaysToRt2._getNth(idx >= 0 ? idx : dataIdx); }
        public double myParamRt2(int idx = -1) { return paramRtRt2._getNth(idx >= 0 ? idx : dataIdx); }
        public int myParamDaysToRt3(int idx = -1) { return paramRtDaysToRt3._getNth(idx >= 0 ? idx : dataIdx); }
        public double myParamRt3(int idx = -1) { return paramRtRt3._getNth(idx >= 0 ? idx : dataIdx); }
        public int myParamDaysToRt4(int idx = -1) { return paramRtDaysToRt4._getNth(idx >= 0 ? idx : dataIdx); }
        public double myParamRt4(int idx = -1) { return paramRtRt4._getNth(idx >= 0 ? idx : dataIdx); }
        public int myParamDaysToRt5(int idx = -1) { return paramRtDaysToRt5._getNth(idx >= 0 ? idx : dataIdx); }
        public double myParamRt5(int idx = -1) { return paramRtRt5._getNth(idx >= 0 ? idx : dataIdx); }
        public int myParamDaysToRt6(int idx = -1) { return paramRtDaysToRt6._getNth(idx >= 0 ? idx : dataIdx); }
        public double myParamRt6(int idx = -1) { return paramRtRt6._getNth(idx >= 0 ? idx : dataIdx); }
        public string myEvents(int idx = -1) { return events._getNth(idx >= 0 ? idx : dataIdx); }

        public void setBarWidth(int value)
        {
            barWidth = value._lowLimit(-4)._highLimit(4);
        }
        public void changeBarWidth(int value)
        {
            barWidth = Math.Min(Math.Max(barWidth + value, -4), 4);
        }
        public void changeYAxisMax(int value)
        {
            if (yAxisMax.Length > dataIdx) yAxisMax[dataIdx] = value;
        }

        public void changeYAxisFixed(int value)
        {
            yAxisFixed = value;
        }

        public void changeYAxis2SeriousFixed(int value)
        {
            yAxis2SeriousFixed = value;
        }

        public void changeYAxis2Max(double value)
        {
            if (yAxis2Max.Length > dataIdx) yAxis2Max[dataIdx] = value;
        }

        /// <summary>
        /// 現在のラジオボタンの位置にあるPrefを左(delta=-1)または右(delta=1)に移動する
        /// </summary>
        /// <param name="delta"></param>
        public void moveRadioPref(int delta)
        {
            int radioPos; // 移動先の位置
            if (delta < 0) {
                radioPos = (radioIdx >= selectorRadioPos) ? Math.Min(radioIdx, Constants.RADIO_IDX_MAX - 1) : radioIdx - 1;
                if (radioPos < MainPrefNum) return;
            } else {
                radioPos = (radioIdx < selectorRadioPos - 1) ? radioIdx + 1 : -1;
            }

            // 以下、 dataIdx を radioPos に移動する汎用的な作りになっている。radioPos が範囲外なら dataIdx が除去される。
            if (favorPrefIdxes._notEmpty()) {
                int pfIdx = dataIdx;                        // pfIdx: 移動対象の prefIdx
                int favorPos = radioPos - MainPrefNum;      // favorPos: favorPrefIdxes[] の中での挿入位置
                if (favorPos >= 0 && favorPos < favorPrefIdxes.Length) {
                    // favorPrefIdxes[] に挿入
                    int fp = favorPrefIdxes._findIndex(pfIdx);  // fp: favorPrefIdxes[] 内で移動対象 prefIdx の位置。なければ -1
                    if (fp < 0) fp = favorPrefIdxes.Length;
                    if (favorPrefIdxes[favorPos] > 0) {
                        // 移動先に別のprefが入っている
                        if (fp < favorPos) {
                            // 移動対象が favorPrefIdxes[] の中で、移動先より前の位置にある
                            for (int i = fp; i < favorPos; ++i) favorPrefIdxes[i] = favorPrefIdxes[i + 1];
                        } else {
                            int ep = Math.Min(fp, favorPrefIdxes.Length - 1);
                            if (ep > favorPos) {
                                // 移動対象が favorPrefIdxes[] の中で、移動先より後の位置にある
                                for (int i = ep; i > favorPos; --i) favorPrefIdxes[i] = favorPrefIdxes[i - 1];
                            } else {
                                // 移動対象が favorPrefIdxes[] の中で移動先と同じ位置にある、または移動対象がセレクタ位置にあって移動先が除去される
                                prefIdx = favorPrefIdxes[favorPos];
                            }
                        }
                    } else {
                        // 移動先が空なので、移動元(もしあれば)を空にしておく
                        if (fp >= 0 && fp < favorPrefIdxes.Length) favorPrefIdxes[fp] = 0;
                    }
                    // 移動先に移動対象 prefIdx をセット
                    favorPrefIdxes[favorPos] = pfIdx;
                } else {
                    // favorPrefIdxes[] から除去
                    for (int i = 0; i < favorPrefIdxes.Length; ++i) {
                        if (favorPrefIdxes[i] == pfIdx) {
                            // pref を除去して selector に設定する
                            favorPrefIdxes[i] = 0;
                            prefIdx = pfIdx;
                        }
                    }
                }
                doCompaction();
                int rp = favorPrefIdxes._findIndex(i => i == pfIdx) + MainPrefNum;
                radioIdx = rp >= MainPrefNum && rp < selectorRadioPos ? rp : selectorRadioPos;
            }

        }

        private void doCompaction()
        {
            int i = 0;
            int k = 0;
            while (i < favorPrefIdxes.Length) {
                if (favorPrefIdxes[i] >= MainPrefNum) favorPrefIdxes[k++] = favorPrefIdxes[i];
                ++i;
            }
            while (k < favorPrefIdxes.Length) {
                favorPrefIdxes[k++] = 0;
            }
            countFavorPref();
        }

        public UserSettings Cleanup()
        {
            if (favorPrefIdxes._notEmpty()) {
                // 同値のものは一つだけ残す
                HashSet<int> founds = new HashSet<int>();
                for (int i = 0; i < favorPrefIdxes.Length; ++i) {
                    var x = favorPrefIdxes[i];
                    if (x > 0) {
                        if (founds.Contains(x)) {
                            favorPrefIdxes[i] = 0;
                        } else {
                            founds.Add(x);
                        }
                    }
                }
                doCompaction();
            }
            return this;
        }

        public void SetEasyParams(int idx, string startDt, int daysToOne, double rt1, double factor1, double rt2, double factor2)
        {
            if (idx < 0) idx = dataIdx;
            paramRtStartDate[idx] = startDt;
            paramRtDaysToOne[idx] = daysToOne;
            paramRtEasyRt1[idx] = rt1;
            paramRtDecayFactor[idx] = factor1;
            paramRtEasyRt2[idx] = rt2;
            paramRtDecayFactorNext[idx] = factor2;
        }

        public void setDrawExpectation(bool value)
        {
            drawExpectation = value;
        }
        public void setEstimatedBar(bool value)
        {
            estimatedBar = value;
        }
        public void setEstimatedBarMinWidth(int value)
        {
            estimatedBarMinWidth = value;
        }
        public void setDrawPosiRates(bool value)
        {
            drawPosiRates = value;
        }
        public void setDrawComplements(bool value)
        {
            drawComplements = value;
        }
        public void setDetailSettings(bool value)
        {
            detailSettings = value;
        }
        public void setFourstepSettings(bool value)
        {
            fourstepSettings = value;
        }
        public void setOnlyOnClick(bool value)
        {
            onlyOnClick = value;
        }
        public void setThinForecastBar(bool value)
        {
            thinForecastBar = value;
        }
        public void setOtherForecastCharts(bool value)
        {
            otherForecastCharts = value;
        }
        public void setExpectOverReal(bool value)
        {
            expectOverReal = value;
        }
        public void setExtensionDays(int value)
        {
            extensionDays = value;
        }
        //public void setUseOnForecast(bool value)
        //{
        //    useOnForecast = value;
        //}
        public void setLocalMaxRtDuration(int value)
        {
            localMaxRtDuration = value;
        }
        public void setExtremeRtDetectDuration(int value)
        {
            extremeRtDetectDuration = value;
        }
        public void setUseDateForChangePoint(bool value)
        {
            useDateForChangePoint = value;
        }
        public void setUsePostDecayRt1(bool value)
        {
            usePostDecayRt1 = value;
        }
        public void setPostDecayFactorRt2(double value)
        {
            postDecayFactorRt2 = value;
        }
        public void setParamStartDate(string value)
        {
            if (paramRtStartDate.Length > dataIdx) paramRtStartDate[dataIdx] = value;
        }
        public void setParamStartDateFourstep(string value)
        {
            if (paramRtStartDateFourstep.Length > dataIdx) paramRtStartDateFourstep[dataIdx] = value;
        }
        public void setParamDaysToOne(int value)
        {
            if (paramRtDaysToOne.Length > dataIdx) paramRtDaysToOne[dataIdx] = value;
        }
        public void setParamDecayFactor(double value)
        {
            if (paramRtDecayFactor.Length > dataIdx) paramRtDecayFactor[dataIdx] = value;
        }
        public void setParamDaysToNext(int value)
        {
            if (paramRtDaysToNext.Length > dataIdx) paramRtDaysToNext[dataIdx] = value;
        }
        public void setParamMaxRt(double value)
        {
            if (paramRtMaxRt.Length > dataIdx) paramRtMaxRt[dataIdx] = value;
        }
        public void setParamMinRt(double value)
        {
            if (paramRtMinRt.Length > dataIdx) paramRtMinRt[dataIdx] = value;
        }
        public void setParamEasyRt1(double value)
        {
            if (paramRtEasyRt1.Length > dataIdx) paramRtEasyRt1[dataIdx] = value;
        }
        public void setParamEasyRt2(double value)
        {
            if (paramRtEasyRt2.Length > dataIdx) paramRtEasyRt2[dataIdx] = value;
        }
        public void setParamDecayFactorNext(double value)
        {
            if (paramRtDecayFactorNext.Length > dataIdx) paramRtDecayFactorNext[dataIdx] = value;
        }
        public void setParamDaysToRt1(int value)
        {
            if (paramRtDaysToRt1.Length > dataIdx) paramRtDaysToRt1[dataIdx] = value;
        }
        public void setParamRt1(double value)
        {
            if (paramRtRt1.Length > dataIdx) paramRtRt1[dataIdx] = value;
        }
        public void setParamDaysToRt2(int value)
        {
            if (paramRtDaysToRt2.Length > dataIdx) paramRtDaysToRt2[dataIdx] = value;
        }
        public void setParamRt2(double value)
        {
            if (paramRtRt2.Length > dataIdx) paramRtRt2[dataIdx] = value;
        }
        public void setParamDaysToRt3(int value)
        {
            if (paramRtDaysToRt3.Length > dataIdx) paramRtDaysToRt3[dataIdx] = value;
        }
        public void setParamRt3(double value)
        {
            if (paramRtRt3.Length > dataIdx) paramRtRt3[dataIdx] = value;
        }
        public void setParamDaysToRt4(int value)
        {
            if (paramRtDaysToRt4.Length > dataIdx) paramRtDaysToRt4[dataIdx] = value;
        }
        public void setParamRt4(double value)
        {
            if (paramRtRt4.Length > dataIdx) paramRtRt4[dataIdx] = value;
        }
        public void setParamDaysToRt5(int value)
        {
            if (paramRtDaysToRt5.Length > dataIdx) paramRtDaysToRt5[dataIdx] = value;
        }
        public void setParamRt5(double value)
        {
            if (paramRtRt5.Length > dataIdx) paramRtRt5[dataIdx] = value;
        }
        public void setParamDaysToRt6(int value)
        {
            if (paramRtDaysToRt6.Length > dataIdx) paramRtDaysToRt6[dataIdx] = value;
        }
        public void setParamRt6(double value)
        {
            if (paramRtRt6.Length > dataIdx) paramRtRt6[dataIdx] = value;
        }
        public void setEvent(string value)
        {
            if (events.Length > dataIdx) events[dataIdx] = value;
        }

        public const int MULTI_PARAMS_SAVE_NUM = 5;

        public static string[] multiSettingsItemNames = new string[] { "初期設定", "保存先1", "保存先2", "保存先3", "保存先4", "作業中" };

        public int getMultiSettingsIdx()
        {
            var items = paramRtMultiSave._nth(dataIdx)._split('|');
            return (items._length() < 2) ? -999 : items[0]._parseInt(-999);
        }

        public void setMultiSettingsIdx(int n)
        {
            if (paramRtMultiSave.Length > dataIdx) {
                string values = paramRtMultiSave._nth(dataIdx)._split('|')[^1];
                paramRtMultiSave[dataIdx] = $"{n}|{values}";
            }
        }

        public void saveRtMultiParams(int n = MULTI_PARAMS_SAVE_NUM)
        {
            if (paramRtMultiSave.Length > dataIdx && n > 0 && n <= MULTI_PARAMS_SAVE_NUM) {
                int idx = getMultiSettingsIdx();
                string[] items = getMultiSettingsItems();
                --n;
                items[n] = serializeMultiSettings();
                paramRtMultiSave[dataIdx] = $"{idx}|{items._join(";")}";
            }
        }

        public void loadRtMultiParams(int n = MULTI_PARAMS_SAVE_NUM)
        {
            if (n == 0) {
                clearRtMultiParams();
            } else {
                --n;
                var items = getMultiSettingsItems()._nth(n)._split(',');
                int idx = 0;
                setParamStartDateFourstep(items._nth(idx++));
                setParamDaysToRt1(items._nth(idx++)._parseInt(0));
                setParamRt1(items._nth(idx++)._parseDouble(0));
                setParamDaysToRt2(items._nth(idx++)._parseInt(0));
                setParamRt2(items._nth(idx++)._parseDouble(0));
                setParamDaysToRt3(items._nth(idx++)._parseInt(0));
                setParamRt3(items._nth(idx++)._parseDouble(0));
                setParamDaysToRt4(items._nth(idx++)._parseInt(0));
                setParamRt4(items._nth(idx++)._parseDouble(0));
                setParamDaysToRt5(items._nth(idx++)._parseInt(0));
                setParamRt5(items._nth(idx++)._parseDouble(0));
                setParamDaysToRt6(items._nth(idx++)._parseInt(0));
                setParamRt6(items._nth(idx++)._parseDouble(0));
            }
        }

        private static string INITIAL_SERIALIZED = ",0,0.000,0,0.000,0,0.000,0,0.000,0,0.000,0,0.000";

        public void clearRtMultiParams()
        {
            setParamStartDateFourstep("");
            setParamDaysToRt1(0);
            setParamRt1(0);
            setParamDaysToRt2(0);
            setParamRt2(0);
            setParamDaysToRt3(0);
            setParamRt3(0);
            setParamDaysToRt4(0);
            setParamRt4(0);
            setParamDaysToRt5(0);
            setParamRt5(0);
            setParamDaysToRt6(0);
            setParamRt6(0);
        }

        public int findCurrentMultiSettings(int checkFirst = -1)
        {
            string serialized = serializeMultiSettings();
            bool isInitial = serialized._equalsTo(INITIAL_SERIALIZED);
            if (checkFirst == 0 && isInitial) return 0;

            string[] items = getMultiSettingsItems();
            if (checkFirst > 0 && (serialized._equalsTo(items._nth(checkFirst - 1)) || (isInitial && items._nth(checkFirst-1)._isEmpty()))) return checkFirst;

            if (isInitial) return 0;
            int idx = items._findIndex(serialized);
            return idx >= 0 ? idx + 1 : -1;
        }

        private string[] getMultiSettingsItems()
        {
            return  paramRtMultiSave._nth(dataIdx)._split('|')[^1]._split(';')._extend(MULTI_PARAMS_SAVE_NUM, "");
        }

        public string serializeMultiSettings()
        {
            return $"{paramRtStartDateFourstep._nth(dataIdx)._reReplace("/0", "")}," +
                $"{paramRtDaysToRt1._nth(dataIdx)},{paramRtRt1._nth(dataIdx):f3},{paramRtDaysToRt2._nth(dataIdx)},{paramRtRt2._nth(dataIdx):f3}," +
                $"{paramRtDaysToRt3._nth(dataIdx)},{paramRtRt3._nth(dataIdx):f3},{paramRtDaysToRt4._nth(dataIdx)},{paramRtRt4._nth(dataIdx):f3}," +
                $"{paramRtDaysToRt5._nth(dataIdx)},{paramRtRt5._nth(dataIdx):f3},{paramRtDaysToRt6._nth(dataIdx)},{paramRtRt6._nth(dataIdx):f3}";
        }
    }

    /// <summary>
    /// ヘルパー拡張メソッド
    /// </summary>
    public static class JsonExtensions
    {
        private static JsonSerializerOptions SerializerOpt = new JsonSerializerOptions { IgnoreNullValues = true };

        public static string _jsonSerialize<T>(this T json)
        {
            return json == null ? "" : JsonSerializer.Serialize<T>(json, SerializerOpt);
        }

        public static T _jsonDeserialize<T>(this string jsonStr)
        {
            return JsonSerializer.Deserialize<T>(jsonStr);
        }
    }

    public static class SettingsHelper
    {
        public static string[] ReadFile(string path)
        {
            try {
                using (System.IO.StreamReader sr = new System.IO.StreamReader(path)) {
                    return sr.ReadToEnd().Trim().Split('\n').Select(x => x.Trim()).ToArray();
                }
            } catch {
                return new string[0];
            }
        }
    }

}
