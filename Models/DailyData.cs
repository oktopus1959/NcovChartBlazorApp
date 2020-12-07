using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using StandardCommon;

namespace ChartBlazorApp.Models
{
    /// <summary>
    /// シングルトンデータクラス
    /// </summary>
    public class DailyData
    {
        public List<InfectData> InfectDataList {
            get {
                if (_infectDataList == null) Initialize();
                return _infectDataList;
            }
        }

        private List<InfectData> _infectDataList = null;

        public DailyData()
        {
            Initialize();
            runReloadTask();
        }

        private void runReloadTask()
        {
            Task.Run(() => {
                while (true) {
                    Task.Delay(3600 * 1000).Wait();
                    Initialize();
                }
            });
        }

        private SyncBool m_syncBool = new SyncBool();
        private DateTime _lastInitializedDt;

        public void Initialize()
        {
            //Console.WriteLine($"[{DateTime.Now}] ENTER: DailyData.Initialize");
            if (m_syncBool.BusyCheck()) return;
            using (m_syncBool) {
                // OnInitialized は2回呼び出される可能性があるので、3秒以内の再呼び出しの場合は、 DailyData の初期化をスキップする
                var _prevDt = _lastInitializedDt;
                _lastInitializedDt = DateTime.Now;
                if (_lastInitializedDt < _prevDt.AddSeconds(3)) return;

                _infectDataList = loadPrefectureData(readFile("Data/csv/prefectures_ex.csv"));
            }
        }

        private static DateTime _firstDate = "2020/6/1"._parseDateTime();

        public class PrefInfectData
        {
            public string Title { get; set; }
            public double Y1_Max { get; set; }
            public double Y2_Max { get; set; }
            public RtDecayParam DecayParam { get; set; }
            public List<DateTime> Dates { get; set; }
            public List<double> Total { get; set; }
            public int PreDataNum { get; set; }
        }

        public static double CalcRt(double[] total, int idx)
        {
            double weekly(int i) => total._nth(i) - total._nth(i - 7);
            double w7 = weekly(idx - 7);
            return w7 > 0 ? Math.Pow(weekly(idx) / w7, 5.0 / 7.0) : 0.0;
        }

        private List<InfectData> loadPrefectureData(IEnumerable<string> lines)
        {
            List<string> prefOrder = new List<string>();
            var prefDataDict = new Dictionary<string, PrefInfectData>();
            PrefInfectData getOrNewData(string[] items, bool bAddOrder)
            {
                PrefInfectData data = null;
                var dispName = items[1];
                var keyName = items[2];
                if (keyName._notEmpty()) {
                    data = prefDataDict._safeGetOrNewInsert(keyName);
                    if (data.Title._isEmpty()) {
                        data.Title = dispName;
                        data.DecayParam = RtDecayParam.CreateDefaultParam();
                        data.Dates = new List<DateTime>();
                        data.Total = new List<double>();
                    }
                    if (bAddOrder && !prefOrder.Contains(keyName)) prefOrder.Add(keyName);
                }
                return data;
            }

            foreach (var line in lines) {
                var items = line.Trim().Split(',');
                if (items._isEmpty()) continue;

                if (items[0]._startsWith("#order")) {
                    prefOrder.AddRange(items[1..]);
                } else if (items[0]._startsWith("#params")) {
                    var data = getOrNewData(items, false);
                    if (data != null) {
                        data.Y1_Max = items._nth(3)._parseDouble(0.0);
                        data.Y2_Max = items._nth(4)._parseDouble(0.0);
                        data.DecayParam.StartDate = items._nth(5)._parseDateTime();
                        data.DecayParam.DaysToOne = items._nth(6)._parseInt(RtDecayParam.DefaultParam.DaysToOne);
                        var factor = items._nth(7)._parseDouble(0);
                        if (factor < 10) factor = Pages.MyChart._decayFactors[(int)factor];
                        data.DecayParam.DecayFactor = factor;
                    }
                } else if (items[0]._startsWith("20")) {
                    var data = getOrNewData(items, true);
                    if (data != null) {
                        var dt = items._nth(0)._parseDateTime();
                        var val = items._nth(3)._parseDouble(0);
                        if (data.Dates._isEmpty() || data.Dates.Last() < dt) {
                            data.Total.Add(val);
                            data.Dates.Add(dt);
                            if (dt < _firstDate) ++data.PreDataNum;
                        } else {
                            for (int i = data.Dates.Count() - 1; i >= 0; --i) {
                                if (data.Dates[i] == dt) {
                                    data.Total[i] = val;
                                } else if (data.Dates[i] > dt) {
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            InfectData makeData(PrefInfectData data)
            {
                int predatanum = data.PreDataNum;
                double total(int i) => data.Total._nth(i + predatanum);
                double newly(int i) => total(i) - total(i - 1);
                double weekly(int i) => total(i) - total(i - 7);
                double average(int i) => weekly(i) / 7;
                double rt(int i) { double w7 = weekly(i - 7); return w7 > 0 ? Math.Pow(weekly(i) / w7, 5.0 / 7.0) : 0.0; };
                var dates = data.Dates.Skip(predatanum).ToArray();
                var newlies = (data.Total.Count - predatanum)._range().Select(i => newly(i)).ToArray();
                var averages = (data.Total.Count - predatanum)._range().Select(i => average(i)).ToArray();
                var rts = (data.Total.Count - predatanum)._range().Select(i => rt(i)).ToArray();
                int newlyMax = (int)newlies.Max();
                var y1_max = data.Y1_Max > 0 ? data.Y1_Max : newlyMax < 20 ? 20 : (newlyMax < 50 ? 50 : ((newlyMax + 100) / 100) * 100);
                var y1_step = y1_max / 10;
                var y2_max = data.Y2_Max > 0 ? data.Y2_Max : (rts[Math.Max(rts.Length - 90, 0)..].Max() > 2.5) ? 5.0 : 2.5;
                var y2_step = y2_max / 5;
                if (data.DecayParam.StartDate._notValid()) data.DecayParam.StartDate = dates[0].AddDays(findRecentMaxIndex(rts));
                return new InfectData {
                    Title = data.Title,
                    Y1_Max = y1_max,
                    Y1_Step = y1_step,
                    Y2_Max = y2_max,
                    Y2_Step = y2_step,
                    Dates = dates,
                    Newly = newlies,
                    Average = averages,
                    Rt = rts,
                    InitialDecayParam = data.DecayParam,
                };
            }

            var infectList = new List<InfectData>();
            foreach (var pref in prefOrder) {
                var data = prefDataDict._safeGet(pref);
                if (data != null) infectList.Add(makeData(data));
            }
            return infectList;
        }

        // [JSInvokable] 属性を付加すると JavaScript から呼び出せるようになる。(今回は使用していないが参考のため残してある)
        // 呼び出し方法については _Host.cshtml の renderChart0 関数、および GompertzInterop クラスを参照のこと。
        // 参照: https://docs.microsoft.com/ja-jp/aspnet/core/blazor/call-dotnet-from-javascript?view=aspnetcore-3.1
        [JSInvokable]
        public string GetChartData(int dataIdx, int yAxisMax, string endDate, bool estimatedBar, bool onlyOnClick, bool bAnimation)
        {
            var json = MakeJsonData(dataIdx, yAxisMax, endDate._parseDateTime(), null, estimatedBar, onlyOnClick, bAnimation);
            return json?.chartData == null ? "" : JsonConvert.SerializeObject(json.chartData, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Include, });
        }

        private static List<string> getFiles(string dirPath, string type)
        {
            try {
                return System.IO.Directory.GetFiles(dirPath, $"*.{type}.csv", System.IO.SearchOption.TopDirectoryOnly).OrderBy(x => x).ToList();
            } catch (Exception) {
                return new List<string>();
            }
        }

        private string[] readFile(string path)
        {
            try {
                using (System.IO.StreamReader sr = new System.IO.StreamReader(path)) {
                    return sr.ReadToEnd().Trim().Split('\n').Select(x => x.Trim()).ToArray();
                }
            } catch {
                return new string[0];
            }
        }

        /// <summary>
        /// Chart描画のためのJsonデータを構築して返す
        /// </summary>
        /// <param name="dataIdx">描画対象(0:全国/1:東京/...)</param>
        /// <param name="endDate">グラフ表示最終日</param>
        /// <param name="aheadParamIdx">事前作成予想データインデックス(負なら使わない)</param>
        /// <param name="bAnimation">グラフアニメーションの有無</param>
        /// <returns></returns>
        public JsonData MakeJsonData(int dataIdx, double yAxisMax, DateTime endDate, RtDecayParam rtDecayParam, bool estimatedBar, bool onlyOnClick, bool bAnimation = false)
        {
            JsonData jsonData = null;
            string title = "";
            var data = InfectDataList._nth(dataIdx);
            if (data != null) {
                title = data.Title;
                Console.WriteLine($"{DateTime.Now} [MakeJsonData] idx={dataIdx}, pref={title}");
                //int x0 = data.X0;
                var chartData = new ChartJson { type = "bar" };
                //var borderDash = new double[] { 10, 3 };
                var fullDates = data.Dates.Select(x => x._toShortDateString()).ToList();
                var firstDate = data.Dates?.First() ?? DateTime.MinValue;
                var lastDate = data.Dates?.Last() ?? DateTime.MinValue;
                if (firstDate._isValid() && lastDate._isValid()) {
                    if (endDate._isValid() && endDate > lastDate) {
                        int days = Math.Min((endDate - lastDate).Days, 400);
                        fullDates.AddRange(Enumerable.Range(1, days).Select(d => lastDate.AddDays(d)._toShortDateString()));
                    }
                    int realDays = (lastDate - firstDate).Days + 1;
                    int predDays = realDays + 21;

                    if (rtDecayParam != null) {
                        predDays = data.PredictValuesEx(rtDecayParam, fullDates._length());
                        //for (int i = data.FullPredRt.Length - 1; i >= (predDays - 14); --i) {
                        //    if (Math.Abs(data.FullPredRt[i] - 1.0) < 0.005) {
                        //        predDays = i + 14;
                        //        break;
                        //    }
                        //}
                    }

                    data.DispDays = Math.Max(realDays + 21, predDays) + 1;

                    (double y1_max, double y1_step, double y2_max, double y2_step) = (data.Y1_Max, data.Y1_Step, data.Y2_Max, data.Y2_Step);
                    if (yAxisMax > 0) {
                        (y1_max, y1_step) = (yAxisMax, yAxisMax / 10);
                    }
                    Options options = Options.CreateTwoAxes(new Ticks(y1_max, y1_step), new Ticks(y2_max, y2_step));
                    if (!bAnimation) options.AnimationDuration = 0;
                    options.legend.SetAlignEnd();
                    options.legend.reverse = true;  // 凡例の表示を登録順とは逆順にする
                     options.SetOnlyClickEvent(onlyOnClick);
                    chartData.options = options;

                    var dataSets = new List<Dataset>();
#if DEBUG
                    if (rtDecayParam != null) {
                        dataSets.Add(Dataset.CreateDotLine("逆算移動平均", data.RevAverage.Take(data.DispDays)._toNullableArray(1), "darkblue"));
                        //dataSets.Add(Dataset.CreateDotLine2("逆算Rt", data.RevRt.Take(data.DispDays)._toNullableArray(1), "crimson"));
                    }
#endif
                    dataSets.Add(Dataset.CreateLine("                ", new double?[fullDates.Count], "rgba(0,0,0,0)", "rgba(0,0,0,0)"));

                    double?[] predRts = null;
                    double?[] predAverage = null;
                    if (rtDecayParam != null) {
                        predRts = data.FullPredRt.Take(predDays)._toNullableArray(3);
                        dataSets.Add(Dataset.CreateDashLine2("予想実効再生産数", predRts, "brown").SetDispOrder(6));
                        predAverage = data.PredAverage.Take(predDays)._toNullableArray(1);
                        dataSets.Add(Dataset.CreateDashLine("予想移動平均", predAverage, "darkgreen").SetDispOrder(4));
                    }
                    double?[] realRts = data.Rt.Take(realDays)._toNullableArray(3);
                    dataSets.Add(Dataset.CreateLine2("実効再生産数(右軸)", realRts, "darkorange", "yellow").SetDispOrder(5));
                    double?[] realAverage = data.Average.Take(realDays)._toNullableArray(1);
                    dataSets.Add(Dataset.CreateLine("陽性者数移動平均", realAverage, "darkblue", "lightblue").SetDispOrder(3));
                    double?[] positives = data.Newly.Take(realDays)._toNullableArray(0, 0);
                    var positiveDataset = Dataset.CreateBar("新規陽性者数", positives, "royalblue").SetHoverColors("mediumblue").SetDispOrder(1);
                    if (rtDecayParam != null && estimatedBar) {
                        options.tooltips.intersect = false;
                        options.tooltips.SetCustomHighest();
                        dataSets.Add(positiveDataset);
                        dataSets.Add(Dataset.CreateBar("推計陽性者数", data.PredNewly.Take(predDays)._toNullableArray(0), "darkgray").SetHoverColors("darkseagreen").SetDispOrder(2));
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
                        labels = fullDates.Take(data.DispDays).ToArray(),
                        datasets = dataSets.ToArray(),
                    };

                    jsonData = new JsonData() {
                        chartData = chartData,
                    };
                }
            }
            return jsonData;
        }

        public string FindDecayStartDate(int dataIdx)
        {
            var data = InfectDataList._nth(dataIdx);
            if (data != null) {
                var dt = data.InitialDecayParam.StartDate;
                if (dt._isValid()) {
                    return dt._toShortDateString();
                }
            }
            return "";
        }

        public int findRecentMaxIndex(double[] rt)
        {
            const int FIND_MAX_DURATION = 10;
            int lastIdx = rt.Length - 1;
            int maxIdx = lastIdx;
            var maxVal = rt[lastIdx];
            int count = FIND_MAX_DURATION;
            while (count-- > 0 && --lastIdx >= 0) {
                var v = rt[lastIdx];
                if (v > maxVal) {
                    maxVal = v;
                    maxIdx = lastIdx;
                    count = FIND_MAX_DURATION;
                }
            }
            return maxIdx;
        }

    }

    public class InfectData
    {
        public string Title { get; set; }

        /// <summary>Y-1軸の Max <summary>
        public double Y1_Max { get; set; }

        /// <summary>Y-1軸の Step <summary>
        public double Y1_Step { get; set; }

        /// <summary>Y-2軸の Max <summary>
        public double Y2_Max { get; set; }

        /// <summary>Y-2軸の Step <summary>
        public double Y2_Step { get; set; }

        /// <summary>実データの存在する日付の列</summary>
        public DateTime[] Dates { get; set; }

        public double[] Newly { get; set; }

        public double[] Average { get; set; }

        public double[] Rt { get; set; }

        //public GompertzParam[][] GomParams { get; set; }

        /// <summary> 事前に作成された予想データ </summary>
        public RtDecayParam InitialDecayParam { get; set; }

        /// <summary> 予想実効再生産数 </summary>
        public double[] FullPredRt { get; private set; }
        /// <summary> 逆算移動平均 </summary>
        public double[] RevAverage { get; private set; }
        /// <summary> 推計移動平均 </summary>
        public double[] PredAverage { get; private set; }
        /// <summary> 予想開始日位置 </summary>
        public int PredStartIdx { get; private set; } = 0;
        ///// <summary> 予想Rt </summary>
        //public double[] PredRt { get; private set; }
        /// <summary> 推計陽性者数 </summary>
        public double[] PredNewly { get; private set; }
        /// <summary> 逆算Rt </summary>
        public double[] RevRt { get; private set; }
        /// <summary> 表示日数 </summary>
        public int DispDays { get; set; } = 1;

        /// <summary>
        /// 各種推計値の計算
        /// </summary>
        /// <param name="rtDecayParam"></param>
        /// <param name="numFullDays"></param>
        /// <param name="predStartDt">予測開始日</param>
        /// <returns>表示開始から予測終了までの日数</returns>
        public int PredictValuesEx(RtDecayParam rtDecayParam, int numFullDays, DateTime? predStartDt = null)
        {
            if (rtDecayParam == null) rtDecayParam = InitialDecayParam;
            var firstRealDate = Dates.First();
            DateTime realEndDate = predStartDt?.AddDays(-1) ?? DateTime.MaxValue;
            if (realEndDate > Dates.Last()) realEndDate = Dates.Last();
            int realDays = (realEndDate - firstRealDate).Days + 1;

            RevAverage = new double[numFullDays];
            FullPredRt = new double[numFullDays];

            if (rtDecayParam.StartDate._notValid()) rtDecayParam.StartDate = InitialDecayParam.StartDate;
            if (rtDecayParam.StartDateDetail._notValid()) rtDecayParam.StartDateDetail = RtDecayParam.DefaultParam.StartDate;
            if (rtDecayParam.StartDate > realEndDate) rtDecayParam.StartDate = realEndDate;
            if (rtDecayParam.StartDateDetail > realEndDate) rtDecayParam.StartDateDetail = realEndDate;
            PredStartIdx = (rtDecayParam.EffectiveStartDate - firstRealDate).Days;
            Array.Copy(Average, RevAverage, PredStartIdx);
            int predRtLen = rtDecayParam.CalcAndCopyPredictRt(Rt, PredStartIdx, FullPredRt, realDays);

            for (int i = 0; i < predRtLen; ++i) {
                int idx = PredStartIdx + i;
                var rt = FullPredRt[idx];
                if (idx >= 7 && idx < RevAverage.Length && rt > 0) {
                    RevAverage[idx] = Math.Pow(rt, 7.0 / 5.0) * RevAverage[idx - 7];
                }
            }

            predRtLen -= 4;
            double[] revAveAverage = new double[numFullDays];   // 逆算移動平均の平均
            for (int i = 0; i < predRtLen; ++i) {
                int idx = PredStartIdx + i;
                int beg = idx - 3;
                int end = idx + 4;
                if (beg >= 0 && end <= RevAverage.Length) {
                    revAveAverage[idx] = RevAverage[beg..end].Sum() / 7;
                }
            }
            PredAverage = revAveAverage._extend(numFullDays);
            PredNewly = predictInfect(Newly, Average, PredAverage, realDays, PredStartIdx);
            double total = 0;
            double[] totals = PredNewly.Select((n, i) => total += (n > 0 ? n : Newly._nth(i))).ToArray();
            RevRt = new double[numFullDays];
            for (int i = PredStartIdx; i < PredNewly.Length; ++i) {
                RevRt[i] = DailyData.CalcRt(totals, i);
            }

            return PredStartIdx + predRtLen;
        }

        private static double[] predictInfect(double[] newly, double[] average, double[] pred, int realDays, int predStartIdx)
        {
            double newly_offset = 3.0;
            double pred_offset = 10.0;

            // dayRatio: 当日増減率
            double[] dayRatio = new double[pred.Length];
            double calcRatio(double n, double? p) => (p > 0) ? Math.Pow((n + newly_offset) / (p.Value + pred_offset), 1.0) : 1.0;
            for (int i = 0; i < realDays && i < dayRatio.Length; ++i) {
                dayRatio[i] = calcRatio(newly._nth(i), (i < predStartIdx) ? average._nth(i) : pred._nth(i));
            }

            // dowRatio: 当日、-7日と-14日の平均増減率
            double[] dowRatio = new double[pred.Length];
            double calcRatio3(int idx) => Enumerable.Range(0, 3).Select(w => dayRatio._nth(idx - w*7, 1.0)).Sum() / 3;
            for (int i = 0; i < dowRatio.Length; ++i) {
                dowRatio[i] = (i < realDays) ? Math.Max(Math.Min(calcRatio3(i), 2.0), 0.3) : dowRatio._nth(i - 7, 1.0);
            }

            //return pred.Select((p, i) => i < realDays && i <= approxDayPos ? null : p * dowRatio._nth(i - 7)).ToArray();
            // 一週間前の曜日増減率を参照する
            return pred.Select((p, i) => (i <= predStartIdx) ? 0 : p * dowRatio._nth(i - 7)).ToArray();
        }

    }

    //public class ManualPredictData
    //{
    //    public GompertzParam GompParam { get; set; }
    //    public DateTime PredDate { get; set; }
    //}

    public class JsonData
    {
        /// <summary>
        /// 近似計算日
        /// </summary>
        //public string approxDate { get; set; }

        public ChartJson chartData { get; set; }
    }

    public class RtDecayParam
    {
        public bool UseDetail { get; set; }

        /// <summary>予測開始日</summary>
        public DateTime StartDate { get; set; }

        public DateTime StartDateDetail { get; set; }

        public DateTime EffectiveStartDate { get { return UseDetail ? StartDateDetail : StartDate; } }

        public int DaysToOne { get; set; }

        public int DaysToRt1 { get; set; }
        public double Rt1 { get; set; }

        public int DaysToRt2 { get; set; }
        public double Rt2 { get; set; }

        public int DaysToRt3 { get; set; }
        public double Rt3 { get; set; }

        public int DaysToRt4 { get; set; }
        public double Rt4 { get; set; }

        public double DecayFactor { get; set; }

        public const int DaysToNextRt = 15;

        public static RtDecayParam CreateDefaultParam()
        {
            return new RtDecayParam() {
                UseDetail = false,
                StartDateDetail = new DateTime(2020, 7, 4),
                DaysToOne = DaysToNextRt,
                DaysToRt1 = 45,
                Rt1 = 0.83,
                DaysToRt2 = 60,
                Rt2 = 1,
                DaysToRt3 = 26,
                Rt3 = 1.4,
                DaysToRt4 = 30,
                Rt4 = 0.83,
                DecayFactor = 1000,
            };
        }

        public static RtDecayParam DefaultParam { get; } = CreateDefaultParam();

        public const int ExtensionDays = 25;
        public const int ExtensionDaysEx = 15;

        /// <summary> y = (a / x) + b の形の関数として Rt の減衰を計算する </summary>
        /// <returns>計算された予測Rtの日数を返す</returns>
        public int CalcAndCopyPredictRt(double[] rts, int startIdx, double[] predRt, int realDays)
        {
            double startValue = rts[startIdx];
            if (UseDetail) {
                double decayFactor = 10000;
                double rt1 = Rt1;
                int daysToRt1 = DaysToRt1;
                if (daysToRt1 < 1) daysToRt1 = DaysToNextRt;
                double a1 = (startValue - rt1) * (decayFactor + daysToRt1) * decayFactor / daysToRt1;
                double b1 = startValue - (a1 / decayFactor);
                // Rt1に到達してから
                double rt2 = Rt2;
                int daysToRt2 = DaysToRt2;
                if (daysToRt2 < 1) daysToRt2 = DaysToNextRt;
                double a2 = (rt1 - rt2) * (decayFactor + daysToRt2) * decayFactor / daysToRt2;
                double b2 = rt1 - (a2 / decayFactor);
                // Rt2に到達してから
                double rt3 = Rt3;
                int daysToRt3 = DaysToRt3;
                if (daysToRt3 < 1) daysToRt3 = DaysToNextRt;
                double a3 = (rt2 - rt3) * (decayFactor + daysToRt3) * decayFactor / daysToRt3;
                double b3 = rt2 - (a3 / decayFactor);
                // Rt3に到達してから
                double rt4 = Rt4;
                int daysToRt4 = DaysToRt4;
                if (daysToRt4 < 1) daysToRt4 = DaysToNextRt;
                double a4 = (rt3 - rt4) * (decayFactor + daysToRt4) * decayFactor / daysToRt4;
                double b4 = rt3 - (a4 / decayFactor);

                int copyLen = Math.Min(Math.Max(daysToRt1 + daysToRt2 + daysToRt3 + daysToRt4 + ExtensionDaysEx, realDays - startIdx + ExtensionDays), predRt.Length - startIdx);
                for (int i = 0; i < copyLen; ++i) {
                    double rt;
                    if (i <= daysToRt1) {
                        rt = a1 / (decayFactor + i) + b1;
                    } else if (i <= daysToRt1 + daysToRt2) {
                        rt = a2 / (decayFactor + i - daysToRt1) + b2;
                        if (rt1 > rt2) {
                            if (rt < rt2) rt = rt2;
                        } else {
                            if (rt > rt2) rt = rt2;
                        }
                    } else if (i <= daysToRt1 + daysToRt2 + daysToRt3) {
                        rt = a3 / (decayFactor + i - daysToRt1 - daysToRt2) + b3;
                        if (rt2 > rt3) {
                            if (rt < rt3) rt = rt3;
                        } else {
                            if (rt > rt3) rt = rt3;
                        }
                    } else {
                        rt = a4 / (decayFactor + i - daysToRt1 - daysToRt2 - daysToRt3) + b4;
                        if (rt3 > rt4) {
                            if (rt < rt4) rt = rt4;
                        } else {
                            if (rt > rt4) rt = rt4;
                        }
                    }
                    predRt[startIdx + i] = rt;
                }
                return copyLen;
            } else {
                double rt1 = 1;
                double factor1 = DecayFactor;
                double a1 = (startValue - rt1) * (factor1 + DaysToOne) * factor1 / DaysToOne;
                double b1 = startValue - (a1 / factor1);
                // Rt1に到達してから
                double rt2 = startValue >= rt1 ? 0.75 : 1.2;
                double decayFactorAfterReachedRt1 = 50;
                double factor2 = Math.Min(DecayFactor, decayFactorAfterReachedRt1);
                double a2 = (startValue - rt1) * (factor2 + DaysToOne) * factor2 / DaysToOne;
                double b2 = startValue - (a2 / factor2);
                int copyLen = Math.Min(Math.Max(DaysToOne + ExtensionDaysEx, realDays - startIdx + ExtensionDays), predRt.Length - startIdx);
                for (int i = 0; i < copyLen; ++i) {
                    predRt[startIdx + i] = Math.Max(i < DaysToOne ? a1 / (factor1 + i) + b1 : a2 / (factor2 + i) + b2, rt2);
                }
                return copyLen;
            }
        }
    }

    public class GompertzParam
    {
        public DateTime StartDate { get; set; }

        public double StartValue { get; set; }

        public int StartOffset { get; set; }

        public int PeakDays { get; set; }

        public double PeakValue { get; set; }

        /// <summary> Gompertz計算 </summary>
        /// <param name="x">表示開始日を 0 とする</param>
        /// <returns></returns>
        public double gompertz(int x)
        {
            if (!calculated) {
                calculated = true;
                if (PeakDays > 0 && PeakValue > 0) {
                    if (StartValue < PeakValue) {
                        if (PeakValue - StartValue < 2) PeakValue = StartValue + 2;
                        calcW();
                    } else {
                        if (StartValue - PeakValue < 2) PeakValue = StartValue - 2;
                        calcW2();
                    }
                }
            }
            return _gompertz(x - StartOffset - (StartValue < PeakValue ? PeakDays : 0));
        }

        private double _gompertz(int x)
        {
            return H * Math.Exp(-Math.Exp(-w * x));
        }

        private bool calculated = false;
        private double w = 0;
        private double H = 0;

        private void calcW()
        {
            double delta = 0.09999;
            w = 0.1;
            H = calcH();
            for (int i = 0; i < 100; ++i) {
                double sv = calcStartVal();
                if (Math.Abs(sv - StartValue) < 0.1) break;
                w += (sv > StartValue) ? delta : -delta;
                H = calcH();
                delta /= 2;
            }
        }

        private void calcW2()
        {
            double delta = 0.09999;
            w = 0.1;
            H = calcH2();
            for (int i = 0; i < 100; ++i) {
                double pv = calcPeakVal();
                if (Math.Abs(pv - PeakValue) < 0.1) break;
                w += (pv > PeakValue) ? delta : -delta;
                H = calcH2();
                delta /= 2;
            }
        }

        private double calcH()
        {
            return PeakValue / (Math.Exp(-1) - Math.Exp(-Math.Exp(w)));
        }

        private double calcH2()
        {
            return StartValue / (Math.Exp(-1) - Math.Exp(-Math.Exp(w)));
        }

        /// <summary> 基準日の gompertz 増分 </summary>
        private double calcStartVal()
        {
            var g0 = _gompertz(-PeakDays);
            var g1 = _gompertz(-PeakDays - 1);
            return g0 - g1;
        }

        private double calcPeakVal()
        {
            var g0 = _gompertz(PeakDays);
            var g1 = _gompertz(PeakDays - 1);
            return g0 - g1;
        }

        public void PrintValues(string title)
        {
            Console.WriteLine($"{title}: StartDate={StartDate._dateString()}, PeakDays={PeakDays}, H={H:f1}, w={w:f5}");
        }
    }

    public static partial class GompertzExtensions
    {
        public static string[] _toString(this IEnumerable<double> data)
        {
            return data.Select(x => $"{x:f1}").ToArray();
        }

        public static string[] _toString(this IEnumerable<double?> data)
        {
            return data.Select(x => x.HasValue ? $"{x:f1}" : null).ToArray();
        }

        public static double[] _toDiff(this double[] data)
        {
            return data.Select((x, i) => i > 0 ? x - data[i - 1] : 0).ToArray();
        }

        public static double?[] _toNullableArray(this IEnumerable<double> array, int roundDigit, double? defval = null)
        {
            return array.Select(x => x > 0 ? Math.Round(x, roundDigit) : defval).ToArray();
        }
    }
}

