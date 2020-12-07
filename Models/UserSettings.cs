using System;
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

        public int radioIdx { get; set; }
        public int prefIdx { get; set; }
        public int barWidth { get; set; }
        public int[] yAxisMax { get; set; }
        public bool drawExpectation { get; set; }
        public bool estimatedBar { get; set; }
        public bool detailSettings { get; set; }
        public bool onlyOnClick { get; set; }
        public string[] paramRtStartDate { get; set; }
        public string[] paramRtStartDateDetail { get; set; }
        public int[] paramRtDaysToOne { get; set; }
        public int[] paramRtDaysToRt1 { get; set; }
        public double[] paramRtRt1 { get; set; }
        public int[] paramRtDaysToRt2 { get; set; }
        public double[] paramRtRt2 { get; set; }
        public int[] paramRtDaysToRt3 { get; set; }
        public double[] paramRtRt3 { get; set; }
        public int[] paramRtDaysToRt4 { get; set; }
        public double[] paramRtRt4 { get; set; }
        public double[] paramRtDecayFactor { get; set; }

        public int dataIdx { get { return radioIdx < MainPrefNum ? radioIdx : prefIdx; } }

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
            barWidth = 0;
            yAxisMax = new int[numData];
            drawExpectation = false;
            estimatedBar = false;
            detailSettings = false;
            onlyOnClick = false;
            paramRtStartDate = new string[numData];
            paramRtStartDateDetail = new string[numData];
            paramRtDaysToOne = new int[numData];
            paramRtDaysToRt1 = new int[numData];
            paramRtRt1 = new double[numData];
            paramRtDaysToRt2 = new int[numData];
            paramRtRt2 = new double[numData];
            paramRtDaysToRt3 = new int[numData];
            paramRtRt3 = new double[numData];
            paramRtDaysToRt4 = new int[numData];
            paramRtRt4 = new double[numData];
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
            paramRtStartDate = _extendArray(paramRtStartDate, numData);
            paramRtStartDateDetail = _extendArray(paramRtStartDateDetail, numData);
            paramRtDaysToOne = _extendArray(paramRtDaysToOne, numData);
            paramRtDaysToRt1 = _extendArray(paramRtDaysToRt1, numData);
            paramRtRt1 = _extendArray(paramRtRt1, numData);
            paramRtDaysToRt2 = _extendArray(paramRtDaysToRt2, numData);
            paramRtRt2 = _extendArray(paramRtRt2, numData);
            paramRtDaysToRt3 = _extendArray(paramRtDaysToRt3, numData);
            paramRtRt3 = _extendArray(paramRtRt3, numData);
            paramRtDaysToRt4 = _extendArray(paramRtDaysToRt4, numData);
            paramRtRt4 = _extendArray(paramRtRt4, numData);
            paramRtDecayFactor = _extendArray(paramRtDecayFactor, numData);
            return this;
        }

        public int myYAxisMax() { return yAxisMax._getNth(dataIdx); }
        public string myParamStartDate() { return paramRtStartDate._getNth(dataIdx); }
        public string myParamStartDateDetail() { return paramRtStartDateDetail._getNth(dataIdx); }
        public int myParamDaysToOne() { return paramRtDaysToOne._getNth(dataIdx); }
        public int myParamDaysToRt1() { return paramRtDaysToRt1._getNth(dataIdx); }
        public double myParamRt1() { return paramRtRt1._getNth(dataIdx); }
        public int myParamDaysToRt2() { return paramRtDaysToRt2._getNth(dataIdx); }
        public double myParamRt2() { return paramRtRt2._getNth(dataIdx); }
        public int myParamDaysToRt3() { return paramRtDaysToRt3._getNth(dataIdx); }
        public double myParamRt3() { return paramRtRt3._getNth(dataIdx); }
        public int myParamDaysToRt4() { return paramRtDaysToRt4._getNth(dataIdx); }
        public double myParamRt4() { return paramRtRt4._getNth(dataIdx); }
        public double myParamDecayFactor() { return paramRtDecayFactor._getNth(dataIdx); }

        public void changeBarWidth(int value)
        {
            barWidth = Math.Min(Math.Max(barWidth + value, -4), 4);
        }
        public void changeYAxisMax(int value)
        {
            if (yAxisMax.Length > dataIdx) yAxisMax[dataIdx] = value;
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
        public void setOnlyOnClick(bool value)
        {
            onlyOnClick = value;
        }
        public void setParamStartDate(string value)
        {
            if (paramRtStartDate.Length > dataIdx) paramRtStartDate[dataIdx] = value;
        }
        public void setParamStartDateDetail(string value)
        {
            if (paramRtStartDateDetail.Length > dataIdx) paramRtStartDateDetail[dataIdx] = value;
        }
        public void setParamDaysToOne(int value)
        {
            if (paramRtDaysToOne.Length > dataIdx) paramRtDaysToOne[dataIdx] = value;
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
        public void setParamDecayFactor(double value)
        {
            if (paramRtDecayFactor.Length > dataIdx) paramRtDecayFactor[dataIdx] = value;
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
