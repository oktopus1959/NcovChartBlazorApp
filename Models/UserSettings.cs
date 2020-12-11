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
        public const int MainPrefNum = 3;

        public const int FavorPrefMax = 10;

        public const int ExtensionDays = 25;

        public const int RadioIdxMax = MainPrefNum + FavorPrefMax;

        // ---- ここから下が LocalStorage に保存される (すべてのメンバーを public にしておくこと)

        public int radioIdx { get; set; }
        /// <summary> select で表示している道府県のインデックス </summary>
        public int prefIdx { get; set; }
        public int[] favorPrefIdxes { get; set; }

        public int barWidth { get; set; }
        public int[] yAxisMax { get; set; }

        public bool drawExpectation { get; set; }
        public bool estimatedBar { get; set; }
        public bool detailSettings { get; set; }
        public bool fourstepSettings { get; set; }
        public bool onlyOnClick { get; set; }
        public int extensionDays { get; set; }
        public bool useOnForecast { get; set; }

        public string[] paramRtStartDate { get; set; }
        public string[] paramRtStartDateFourstep { get; set; }
        public int[] paramRtDaysToOne { get; set; }
        public double[] paramRtDecayFactor { get; set; }
        public int[] paramRtDaysToNext { get; set; }
        public double[] paramRtMaxRt { get; set; }
        public double[] paramRtMinRt { get; set; }
        public double[] paramRtDecayFactorNext { get; set; }

        public int[] paramRtDaysToRt1 { get; set; }
        public double[] paramRtRt1 { get; set; }
        public int[] paramRtDaysToRt2 { get; set; }
        public double[] paramRtRt2 { get; set; }
        public int[] paramRtDaysToRt3 { get; set; }
        public double[] paramRtRt3 { get; set; }
        public int[] paramRtDaysToRt4 { get; set; }
        public double[] paramRtRt4 { get; set; }

        // ---- 上のところまでが LocalStorage に保存される

        public int dataIdx { get { return prefIdxByRadio(radioIdx); } }

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
            radioIdx = 0;
            prefIdx = MainPrefNum;
            favorPrefIdxes = new int[FavorPrefMax];
            barWidth = 0;
            yAxisMax = new int[numData];
            drawExpectation = false;
            estimatedBar = false;
            detailSettings = false;
            fourstepSettings = false;
            onlyOnClick = false;
            extensionDays = 0;
            useOnForecast = false;
            paramRtStartDate = new string[numData];
            paramRtStartDateFourstep = new string[numData];
            paramRtDaysToOne = new int[numData];
            paramRtDecayFactor = new double[numData];
            paramRtDaysToNext = new int[numData];
            paramRtMaxRt = new double[numData];
            paramRtMinRt = new double[numData];
            paramRtDecayFactorNext = new double[numData];
            paramRtDaysToRt1 = new int[numData];
            paramRtRt1 = new double[numData];
            paramRtDaysToRt2 = new int[numData];
            paramRtRt2 = new double[numData];
            paramRtDaysToRt3 = new int[numData];
            paramRtRt3 = new double[numData];
            paramRtDaysToRt4 = new int[numData];
            paramRtRt4 = new double[numData];
            return this;
        }

        public RtDecayParam MakeRtDecayParam(RtDecayParam iniParam, int idx = -1)
        {
            return new RtDecayParam {
                UseDetail = detailSettings,
                Fourstep = fourstepSettings,
                StartDate = myParamStartDate(idx)._parseDateTime()._orElse(iniParam.StartDate),
                StartDateFourstep = myParamStartDateFourstep(idx)._parseDateTime()._orElse(iniParam.StartDateFourstep),
                DaysToOne = myParamDaysToOne(idx)._gtZeroOr(iniParam.DaysToOne),
                DecayFactor = myParamDecayFactor(idx)._gtZeroOr(iniParam.DecayFactor),
                DaysToNext = myParamDaysToNext(idx)._gtZeroOr(iniParam.DaysToNext),
                RtMax = myParamMaxRt(idx)._gtZeroOr(iniParam.RtMax),
                RtMin = myParamMinRt(idx)._gtZeroOr(iniParam.RtMin),
                DecayFactorNext = myParamDecayFactorNext(idx)._gtZeroOr(iniParam.DecayFactorNext),
                DaysToRt1 = myParamDaysToRt1(idx)._gtZeroOr(iniParam.DaysToRt1),
                Rt1 = myParamRt1(idx)._gtZeroOr(iniParam.Rt1),
                DaysToRt2 = myParamDaysToRt2(idx)._gtZeroOr(iniParam.DaysToRt2),
                Rt2 = myParamRt2(idx)._gtZeroOr(iniParam.Rt2),
                DaysToRt3 = myParamDaysToRt3(idx)._gtZeroOr(iniParam.DaysToRt3),
                Rt3 = myParamRt3(idx)._gtZeroOr(iniParam.Rt3),
                DaysToRt4 = myParamDaysToRt4(idx)._gtZeroOr(iniParam.DaysToRt4),
                Rt4 = myParamRt4(idx)._gtZeroOr(iniParam.Rt4),
            };
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

        private const string SettingsKey = "net.oktopus59.ncov.settings.v4"; // v1008

        /// <summary>
        /// ブラウザの LocalStorage から値を取得
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private static async ValueTask<string> getLocalStorage(IJSRuntime jsRuntime)
        {
            return await jsRuntime.InvokeAsync<string>("getLocalStorage", SettingsKey);
        }

        /// <summary>
        /// 保存しておいた設定を取得する
        /// </summary>
        /// <returns></returns>
        public static async ValueTask<UserSettings> GetSettings(IJSRuntime jsRuntime, int numData)
        {
            try {
                return (await getLocalStorage(jsRuntime))._jsonDeserialize<UserSettings>().SetJsRuntime(jsRuntime).fillEmptyArray(numData);
            } catch {
                return (new UserSettings().SetJsRuntime(jsRuntime)).Initialize(numData);
            }
        }

        /// <summary>
        /// ブラウザの LocalStorage に値を保存
        /// </summary>
        /// <param name="key"></param>
        public async Task SaveSettings()
        {
            await JSRuntime.InvokeAsync<string>("setLocalStorage", SettingsKey, this._jsonSerialize());
        }

        public UserSettings fillEmptyArray(int numData)
        {
            yAxisMax = _extendArray(yAxisMax, numData);
            favorPrefIdxes = _extendArray(favorPrefIdxes, FavorPrefMax);
            paramRtStartDate = _extendArray(paramRtStartDate, numData);
            paramRtStartDateFourstep = _extendArray(paramRtStartDateFourstep, numData);
            paramRtDaysToOne = _extendArray(paramRtDaysToOne, numData);
            paramRtDecayFactor = _extendArray(paramRtDecayFactor, numData);
            paramRtDaysToNext = _extendArray(paramRtDaysToNext, numData);
            paramRtMaxRt = _extendArray(paramRtMaxRt, numData);
            paramRtMinRt = _extendArray(paramRtMinRt, numData);
            paramRtDecayFactorNext = _extendArray(paramRtDecayFactorNext, numData);
            paramRtDaysToRt1 = _extendArray(paramRtDaysToRt1, numData);
            paramRtRt1 = _extendArray(paramRtRt1, numData);
            paramRtDaysToRt2 = _extendArray(paramRtDaysToRt2, numData);
            paramRtRt2 = _extendArray(paramRtRt2, numData);
            paramRtDaysToRt3 = _extendArray(paramRtDaysToRt3, numData);
            paramRtRt3 = _extendArray(paramRtRt3, numData);
            paramRtDaysToRt4 = _extendArray(paramRtDaysToRt4, numData);
            paramRtRt4 = _extendArray(paramRtRt4, numData);
            return this;
        }

        public int myYAxisMax(int idx = -1) { return yAxisMax._getNth(idx >= 0 ? idx : dataIdx); }
        public string myParamStartDate(int idx = -1) { return paramRtStartDate._getNth(idx >= 0 ? idx : dataIdx); }
        public string myParamStartDateFourstep(int idx = -1) { return paramRtStartDateFourstep._getNth(idx >= 0 ? idx : dataIdx); }
        public int myParamDaysToOne(int idx = -1) { return paramRtDaysToOne._getNth(idx >= 0 ? idx : dataIdx); }
        public double myParamDecayFactor(int idx = -1) { return paramRtDecayFactor._getNth(idx >= 0 ? idx : dataIdx); }
        public int myParamDaysToNext(int idx = -1) { return paramRtDaysToNext._getNth(idx >= 0 ? idx : dataIdx); }
        public double myParamMaxRt(int idx = -1) { return paramRtMaxRt._getNth(idx >= 0 ? idx : dataIdx); }
        public double myParamMinRt(int idx = -1) { return paramRtMinRt._getNth(idx >= 0 ? idx : dataIdx); }
        public double myParamDecayFactorNext(int idx = -1) { return paramRtDecayFactorNext._getNth(idx >= 0 ? idx : dataIdx); }
        public int myParamDaysToRt1(int idx = -1) { return paramRtDaysToRt1._getNth(idx >= 0 ? idx : dataIdx); }
        public double myParamRt1(int idx = -1) { return paramRtRt1._getNth(idx >= 0 ? idx : dataIdx); }
        public int myParamDaysToRt2(int idx = -1) { return paramRtDaysToRt2._getNth(idx >= 0 ? idx : dataIdx); }
        public double myParamRt2(int idx = -1) { return paramRtRt2._getNth(idx >= 0 ? idx : dataIdx); }
        public int myParamDaysToRt3(int idx = -1) { return paramRtDaysToRt3._getNth(idx >= 0 ? idx : dataIdx); }
        public double myParamRt3(int idx = -1) { return paramRtRt3._getNth(idx >= 0 ? idx : dataIdx); }
        public int myParamDaysToRt4(int idx = -1) { return paramRtDaysToRt4._getNth(idx >= 0 ? idx : dataIdx); }
        public double myParamRt4(int idx = -1) { return paramRtRt4._getNth(idx >= 0 ? idx : dataIdx); }

        public void changeBarWidth(int value)
        {
            barWidth = Math.Min(Math.Max(barWidth + value, -4), 4);
        }
        public void changeYAxisMax(int value)
        {
            if (yAxisMax.Length > dataIdx) yAxisMax[dataIdx] = value;
        }

        /// <summary>
        /// prefIdx を指定ラジオボタン位置に挿入する
        /// </summary>
        /// <param name="prefIdx">DailyData.InfectDataListにおけるインデックス</param>
        /// <param name="radioPos">ラジオボタンの位置; 範囲外なら除去</param>
        public void moveFavorPref(int prefIdx, int radioPos)
        {
            if (favorPrefIdxes._notEmpty()) {
                int favorPos = radioPos - MainPrefNum;
                if (favorPos >= 0 && favorPos < favorPrefIdxes.Length) {
                    // 挿入位置を空ける
                    int fp = favorPrefIdxes._findIndex(prefIdx);
                    if (fp < 0) fp = favorPrefIdxes.Length;
                    if (favorPrefIdxes[favorPos] > 0) {
                        if (fp < favorPos) {
                            for (int i = fp; i < favorPos; ++i) favorPrefIdxes[i] = favorPrefIdxes[i + 1];
                        } else if (fp > favorPos) {
                            int ep = Math.Min(fp, favorPrefIdxes.Length - 1);
                            for (int i = ep; i > favorPos; --i) favorPrefIdxes[i] = favorPrefIdxes[i - 1];
                        }
                    } else {
                        if (fp >= 0 && fp < favorPrefIdxes.Length) favorPrefIdxes[fp] = 0;
                    }
                    favorPrefIdxes[favorPos] = prefIdx;
                } else {
                    for (int i = 0; i < favorPrefIdxes.Length; ++i) {
                        if (favorPrefIdxes[i] == prefIdx) favorPrefIdxes[i] = 0;
                    }
                }
                doCompaction();
                int rp = favorPrefIdxes._findIndex(i => i == prefIdx) + MainPrefNum;
                radioIdx = rp >= MainPrefNum && rp < selectorRadioPos ? rp : selectorRadioPos;
            }
        }

        /// <summary>
        /// 現在のラジオボタンの位置にあるPrefを左(delta=-1)または右(delta=1)に移動する
        /// </summary>
        /// <param name="delta"></param>
        public void moveRadioPref(int delta)
        {
            int pos;
            if (delta < 0) {
                pos = (radioIdx >= selectorRadioPos) ? Math.Min(radioIdx, RadioIdxMax - 1) : radioIdx - 1;
                if (pos < MainPrefNum) return;
            } else {
                pos = (radioIdx < selectorRadioPos - 1) ? radioIdx + 1 : -1;
            }
            moveFavorPref(dataIdx, pos);

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

        public void setDrawExpectation(bool value)
        {
            drawExpectation = value;
        }
        public void setEstimatedBar(bool value)
        {
            estimatedBar = value;
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
        public void setExtensionDays(int value)
        {
            extensionDays = value;
        }
        public void setUseOnForecast(bool value)
        {
            useOnForecast = value;
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

}
