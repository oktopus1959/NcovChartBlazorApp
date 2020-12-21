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

        public int InfectDataCount { get { return _infectDataList?.Count ?? 0; } }

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
                    Task.Delay(60 * 1000).Wait();
                    Initialize();
                }
            });
        }

        private SyncBool m_syncBool = new SyncBool();
        private DateTime _lastInitializedDt;
        private DateTime _lastFileDt;

        public void Initialize(bool bForce = false)
        {
#if DEBUG
            //Console.WriteLine($"{DateTime.Now} [DailyData.Initialize] CALLED");
#endif
            if (m_syncBool.BusyCheck()) return;
            using (m_syncBool) {
                // OnInitialized は2回呼び出される可能性があるので、30秒以内の再呼び出しの場合は、 DailyData の初期化をスキップする
                var prevDt = _lastInitializedDt;
                _lastInitializedDt = DateTime.Now;
                if (_lastInitializedDt < prevDt.AddSeconds(30)) return;

                const string filePath = "Data/csv/prefectures_ex.csv";
                var fileInfo = Helper.GetFileInfo(filePath);
                if (bForce || fileInfo.ModifyDt > _lastFileDt) {
                    // ファイルが更新されていたら再ロードする
                    _lastFileDt = fileInfo.ModifyDt;
                    _infectDataList = loadPrefectureData(readFile(filePath));
                    Console.WriteLine($"{_lastInitializedDt} [DailyData.Initialize] Reload:{filePath}");
                }
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
                        data.DecayParam = new RtDecayParam();   // 全て 0 の初期データ
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
                        data.DecayParam.DaysToOne = items._nth(6)._parseInt(0);
                        data.DecayParam.DecayFactor = Pages.MyChart.GetDecayFactor(items._nth(7)._parseDouble(-9999));
                        data.DecayParam.EasyRt1 = items._nth(8)._parseDouble(0);
                        data.DecayParam.EasyRt2 = items._nth(9)._parseDouble(0);
                        data.DecayParam.DecayFactorNext = Pages.MyChart.GetDecayFactor2(items._nth(10)._parseDouble(-9999));
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

            double[] adjustTotal(double[] total)
            {
                double[] adjTotal = new double[total.Length];
                Array.Copy(total, adjTotal, total.Length);
                int emptyIdx = -1;
                for (int i = 0; i < adjTotal.Length; ++i) {
                    var newVal = total._nth(i) - total._nth(i - 1);
                    if (newVal > 0) {
                        if (emptyIdx >= 0) {
                            double prevVal = adjTotal._nth(emptyIdx - 1) - adjTotal._nth(emptyIdx - 2);
                            int num = i - emptyIdx + 1;
                            if (((newVal >= 40 || prevVal >= 40) && num <= 5) || newVal >= num * 10 || prevVal >= num * 10) {
                                double delta;
                                int k, m;
                                if (prevVal > newVal) {
                                    delta = prevVal / num;
                                    k = i - 1;
                                    m = emptyIdx - 1;
                                    adjTotal[k] = adjTotal._nth(m);
                                } else {
                                    delta = newVal / num;
                                    k = i;
                                    m = emptyIdx;
                                }
                                for (int j = k - 1; j >= m; --j) adjTotal[j] = adjTotal[j + 1] - delta;
                            }
                        }
                        emptyIdx = -1;
                    } else {
                        if (emptyIdx < 0) emptyIdx = i;
                    }
                }
                return adjTotal;
            }

            InfectData makeData(PrefInfectData data)
            {
                int predatanum = data.PreDataNum;
                double total(int i) => data.Total._nth(i + predatanum);
                double newly(int i) => total(i) - total(i - 1);
                var dates = data.Dates.Skip(predatanum).ToArray();
                var newlies = (data.Total.Count - predatanum)._range().Select(i => newly(i)).ToArray();
                double[] adjustedTotal = adjustTotal(data.Total.ToArray());
                double adjTotal(int i) => adjustedTotal._nth(i + predatanum);
                //double weekly(int i) => total(i) - total(i - 7);
                double weekly(int i) => adjTotal(i) - adjTotal(i - 7);
                double average(int i) => weekly(i) / 7;
                var averages = (data.Total.Count - predatanum)._range().Select(i => average(i)).ToArray();
                double rt(int i) { double w7 = weekly(i - 7); return w7 > 0 ? Math.Pow(weekly(i) / w7, 5.0 / 7.0) : 0.0; };
                var rts = (data.Total.Count - predatanum)._range().Select(i => rt(i)).ToArray();
                int newlyMax = (int)newlies.Max();
                var y1_max = data.Y1_Max > 0 ? data.Y1_Max : newlyMax < 20 ? 20 : (newlyMax < 50 ? 50 : ((newlyMax + 100) / 100) * 100);
                var y1_step = y1_max / 10;
                var y2_max = data.Y2_Max > 0 ? data.Y2_Max : (rts[Math.Max(rts.Length - 90, 0)..].Max() > 2.5) ? 5.0 : 2.5;
                var y2_step = y2_max / 5;
                //以下を有効にしてしまうと、システム既定基準日とシステム既定検出遡及日による基準日との区別ができなくなってしまう。
                //if (data.DecayParam.StartDate._notValid()) data.DecayParam.StartDate = dates[0].AddDays(InfectData.FindRecentMaxIndex(rts));
                var infectData = new InfectData {
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
                infectData.InitialSubParams = infectData.CalcDecaySubParams();
                return infectData;
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
        public string GetChartData(InfectData infData, int yAxisMax, string endDate, bool estimatedBar, bool onlyOnClick, bool bAnimation)
        {
            //(var jsonStr, var dispDays) = MakeJsonData(infData, yAxisMax, endDate._parseDateTime(), null, 0, estimatedBar, onlyOnClick, bAnimation);
            return "";
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

        public string[] GetDecayStartDates(int localMaxRtDulation)
        {
            if (_infectDataList._isEmpty()) return new string[0];

            return _infectDataList.Select(data => (data == null) ? "" : data.GetDecayStartDateStr(localMaxRtDulation)).ToArray();
        }

    }

    /// <summary>
    /// 県別データファイルから作成される陽性者データクラス
    /// </summary>
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

        /// <summary> 県別データファイルから取得した設定値(Layer-3) </summary>
        public RtDecayParam InitialDecayParam { get; set; }

        /// <summary> 県別データファイルから事前に作成された予測設定値(Layer-2) </summary>
        public SubParams InitialSubParams { get; set; }

        public static InfectData DummyData = new InfectData() {
            InitialDecayParam = new RtDecayParam(),
            InitialSubParams = new SubParams(),
        };

        public string GetDecayStartDateStr(int localMaxRtDulation)
        {
            var dt = GetDecayStartDate(localMaxRtDulation);
            return dt._isValid() ? dt.ToShortDateString() : "";
        }

        public DateTime GetDecayStartDate(int localMaxRtDulation)
        {
            if (InitialDecayParam.StartDate._isValid()) {
                // システム既定基準日が設定されていればそれを返す
                return InitialDecayParam.StartDate;
            }
            return FindRecentMaxMinRtDate(localMaxRtDulation);
        }

        public string FindRecentMaxRtDateStr(int localMaxRtDulation)
        {
            DateTime dt = FindRecentMaxMinRtDate(localMaxRtDulation);
            return dt._isValid() ? dt.ToShortDateString() : "";
        }

        public DateTime FindRecentMaxMinRtDate(int localMaxRtDulation)
        {
            DateTime firstDt = Dates._first();
            if (firstDt._isValid()) {
                return firstDt.AddDays(FindRecentMaxMinIndex(Rt, localMaxRtDulation));
            } else {
                return Dates._last();
            }
        }

        public int FindRecentMaxMinIndex(int localMaxRtDulation)
        {
            return FindRecentMaxMinIndex(Rt, localMaxRtDulation);
        }

        public static int FindRecentMaxMinIndex(double[] rt, int localMaxRtDulation)
        {
            return Math.Min(
                findRecentMaxMinIndex(rt, (v, m) => v > m, localMaxRtDulation),
                findRecentMaxMinIndex(rt, (v, m) => v < m, localMaxRtDulation));
        }

        public static int FindRecentMinIndex(double[] rt, int localMaxRtDulation)
        {
            return findRecentMaxMinIndex(rt, (v, m) => v < m, localMaxRtDulation);
        }

        private static int findRecentMaxMinIndex(double[] rt, Func<double, double, bool> comp, int localMaxRtDulation)
        {
            const double RT_THRESHOLD = 3.0;
            const int GUARD_DURATION = 5; 

            if (localMaxRtDulation < 0) localMaxRtDulation = UserSettings.LocalMaxRtDuration;
            int mIdx = rt._safeCount();
            if (rt._notEmpty()) {
                int lastIdx = rt.Length - 1;
                var mVal = rt[lastIdx];
                mIdx = lastIdx;
                int count = localMaxRtDulation;
                while (count-- > 0 && --lastIdx >= 0) {
                    var v = rt[lastIdx];
                    if (v > RT_THRESHOLD && lastIdx < rt.Length - GUARD_DURATION) break;
                    if (v <= RT_THRESHOLD && comp(v, mVal)) {
                        mVal = v;
                        mIdx = lastIdx;
                        count = localMaxRtDulation;
                    }
                }
            }
            return mIdx;
        }

        const int TWO_STAGE_MIN_DURATION = 12;

        public class MinParams
        {
            public int days = 0;
            public double err = double.MaxValue;
            public double err2 = double.MaxValue;
            public double rt1 = 0;
            public double rt2 = 0;
            public int ftIdx1 = 0;
            public int ftIdx2 = 8;

            public SubParams MakeSubParams(DateTime dt)
            {
                return new SubParams() {
                    StartDate = dt._toDateString(),
                    Rt1 = rt1._round(3),
                    Rt2 = rt2._round(3),
                    DaysToOne = days,
                    DecayFactor = Pages.MyChart._decayFactors._nth(ftIdx1),
                    DecayFactorNext = Pages.MyChart._decayFactors2._nth(ftIdx2),
                };
            }

            public RtDecayParam CopyToDecayParam(RtDecayParam param)
            {
                param.EasyRt1 = rt1;
                param.EasyRt2 = rt2;
                param.DaysToOne = days;
                param.DecayFactor = Pages.MyChart._decayFactors[ftIdx1];
                param.DecayFactorNext = Pages.MyChart._decayFactors2[ftIdx2];
                return param;
            }

            public override string ToString()
            {
                return $"days={days}, err={(err < double.MaxValue ? err : 999999.999):f3}, err2={(err2 < double.MaxValue ? err2 : 999999.999):f3}, rt1={rt1:f3}, rt2={rt2:f3}, ftIdx1={ftIdx1}, ftIdx2={ftIdx2}";
            }
        }

        const int AVERAGE_ERR_DURATION = 7;

#if DEBUG
        static int debugLevel = 0;
        static void debugN(int n, Func<string> func)
        {
            if (debugLevel >= n) Console.WriteLine(func());
        }

        static void debug0(Func<string> func) { debugN(0, func); }
        static void debug1(Func<string> func) { debugN(1, func); }
        static void debug2(Func<string> func) { debugN(2, func); }
        static void debug3(Func<string> func) { debugN(3, func); }
        static void debug4(Func<string> func) { debugN(4, func); }
        static void debug5(Func<string> func) { debugN(5, func); }
#endif

        /// <summary>
        /// 基準日、変化点、傾きの推計
        /// </summary>
        /// <param name="localDuration"></param>
        /// <param name="dt_"></param>
        /// <returns></returns>
        public SubParams CalcDecaySubParams(int localDuration = -1, DateTime? dt_ = null)
        {

            var dt = dt_.HasValue ? dt_.Value : GetDecayStartDate(localDuration);
            double[] rts = Rt;
            int startIdx = (dt - Dates._first()).Days;
            int len = Rt._length() - startIdx - 1;
            //int days = Math.Max(len + 5, 15);
            int days = (int)(len * (4.0 / 3.0));
            MinParams minParam = new MinParams() { days = days, rt2 = -1 };

            double rt0 = Rt._nth(startIdx);

#if DEBUG
            debugLevel = 0;
           if (Title == "全国" || Title == "宮城県") {
                debugLevel = 0;
                debug0(() => $"{Title}_0: dt={dt._toDateString()}");
            }
#endif

            (var err, var rt1) = find_rt1(Rt, startIdx);
            minParam.err = err;
            minParam.rt1 = rt1;
#if DEBUG
            debug1(() => $"{Title}_1: find_rt1: {minParam}");
#endif
            // find_rt1 後の調整
            double adjustDelta = (rt1 == rt0 || len >= 10) ? 0 : (len >= 5) ? 0.01 : 0.02;
            if (rt1 < rt0) adjustDelta = -adjustDelta;
            rt1 -= adjustDelta;
            // calc days' rt1
            var days_rt1 = rt0 + (rt1 - rt0) * days / len;
            if (days_rt1 < 0) {
                days = (int)((rt0 * len) / (rt0 - rt1));
                rt1 = rt0 + (rt1 - rt0) * days / len;
            } else {
                rt1 = days_rt1;
            }
            var rt2 = (rt1 > 1) ?   1 + (rt1 - 1) * 0.3 :
                      (rt0 > rt1) ? 1 - (1 - rt1) * 1.2 :
                                    1 - (1 - rt1) * 0.8;
            minParam.days = days;
            minParam.rt1 = rt1;
            minParam.rt2 = Math.Max(rt2, 0.001);
            minParam.err2 = calcErrByAverage(AVERAGE_ERR_DURATION, dt, minParam);
#if DEBUG
            debug1(() => $"{Title}_2: adjusted: {minParam}");
#endif

            if (len >= TWO_STAGE_MIN_DURATION) {
                var minp = find_all(dt, Rt, startIdx);
#if DEBUG
                debug1(() => $"{Title}_3: find_all: {minp}");
#endif
                //if (minp.err < err) {
                if (minp.err2 < minParam.err2) {
                    minParam = minp;
                }
            }
            //if (a > 0) a *= 0.9;
            const int TAIL_ERR_DURATION = 3;
            adjustFactor1AndMakeSubParams(TAIL_ERR_DURATION, dt, minParam);
#if DEBUG
            debug1(() => $"{Title}_4: {minParam}");
#endif
            return minParam.MakeSubParams(dt);
        }

        private (double, double) find_rt1(double[] rts, int startIdx)
        {
            if (rts._isEmpty() || startIdx < 0 || startIdx >= rts.Length) return (0, 0);
            double b = rts._nth(startIdx);

            double calcSquareErr(double a, double b)
            {
                return rts.Skip(startIdx).Select((y, x) => Math.Pow(a * x + b - y, 2)).Sum();
            }

            double calcErrdiff(double a)
            {
                return rts.Skip(startIdx).Select((y, x) => (a * x + b - y) * x).Sum();
            }

            const double DOUBLING_THRESHOLD = 10.0;
            bool bSwinged = false;
            double a = 0.01;
            double delta = a / 5;
            double d = calcErrdiff(a);
            bool positive = d > 0;
            for (int i = 0; i < 100; ++i) {
#if DEBUG
                debug2(() => $"find_rt1({i}): a={a:f5}, d={d:f7}, delta={delta:f7}, posi={positive}");
#endif
                if (Math.Abs(d) < 0.0000001) break;
                a += (d > 0) ? -delta : delta;
                if ((positive && d < 0) || (!positive && d > 0)) {
                    delta /= 2;
                    positive = !positive;
                    bSwinged = true;
                } else if (Math.Abs(d) >= DOUBLING_THRESHOLD && !bSwinged) {
                    delta *= 2;
                }
                d = calcErrdiff(a);
            }
#if DEBUG
            debug1(() => $"find_rt1: a={a:f5}, b={b:f7}");
#endif
            return (calcSquareErr(a, b), a * (rts.Length - startIdx - 1) + b);
        }

        private MinParams find_all(DateTime dt, double[] rts, int startIdx)
        {
            if (rts._isEmpty() || startIdx < 0 || startIdx >= rts.Length) return new MinParams();

            double calcSquareErr(double a, double b, int begin, int end)
            {
                return rts.Skip(begin).Take(end - begin + 1).Select((y, x) => Math.Pow(a * x + b - y, 2)).Sum();
            }

            double calcSquareErr2(double rt0, double rt1, double rt2, double factor2, int DaysToOne, int begin, int end)
            {
                double a2, b2;
                if (factor2 > 0) {
                    double rt_ = ((rt0 > rt1 && rt2 > rt1) || (rt0 < rt1 && rt2 < rt1)) ? rt1 * 2 - rt0 : rt0;
                    a2 = (rt_ - rt1) * (factor2 + DaysToOne) * factor2 / DaysToOne;
                    b2 = rt_ - (a2 / factor2);
                } else {
                    double f2 = Math.Max(factor2, -4);
                    a2 = (rt2 - rt1) / (DaysToOne * (1.0 + 0.2 * f2));
                    b2 = rt1;
                }
                return rts.Skip(begin).Take(end - begin + 1).Select((y, x) => {
                    double rt = rt2;
                    if (rt2 != rt1) {
                        rt = (factor2 > 0) ? a2 / (factor2 + DaysToOne + x) + b2 : a2 * x + b2;
                        if ((rt2 < rt1 && rt < rt2) || (rt2 > rt1 && rt > rt2)) rt = rt2;
                    }
                    return Math.Pow(rt - y, 2);
                }).Sum();
            }

            
            int len = rts.Length - startIdx;
            // 変化日ごとの最小エラー値を保存
            MinParams[] minParams = new MinParams[len + 1];
            for (int i = 0; i < minParams.Length; ++i) { minParams[i] = new MinParams() { days = i }; }  // days を初期化しておく

            double rt0 = rts[startIdx];
            int[] decayFactors1 = Pages.MyChart._decayFactors;
            int[] decayFactors2 = Pages.MyChart._decayFactors2;

            int cp_mid = len / 2;
            int cp_margin = len / 4;
            int cp_beg = cp_margin;
            int cp_end = len - cp_margin;
            double delta = 0.05;
            foreach (var minp in minParams.Skip(cp_beg).Take(cp_margin + 1 + cp_margin)) {
                int cp = minp.days;
                int cp_idx = startIdx + cp;
                int cp_nxt = cp_idx + 1;
                double b1 = rt0;
                double rt_beg = Math.Max(rts[cp_idx] - 0.25, 0);
                double rt_end = rts[cp_idx] + 0.25;
                //double rt2_beg = Math.Max(rts[^3..].Min() - 0.25, 0);
                //double rt2_end = rts[^3..].Max() + 0.25;
                double rt2_beg = Math.Max(rts[^1] - 0.25, 0);
                double rt2_end = rts[^1] + 0.25;
                for (double rt1 = rt_beg; rt1 <= rt_end; rt1 += delta) {
                    var a1 = (rt1 - b1) / cp;
                    double err1 = calcSquareErr(a1, b1, startIdx, cp_nxt);

                    for (int ftIdx2 = 0; ftIdx2 < decayFactors2.Length; ++ftIdx2) {
                        for (double rt2 = rt2_beg; rt2 <= rt2_end; rt2 += delta) {
                            //var rt2 = rts.Last();
                            double err2 = calcSquareErr2(rt0, rt1, rt2, decayFactors2[ftIdx2], cp, cp_nxt + 1, rts.Length);
                            double err = err1 + err2;
                            if (err < minp.err) {
                                minp.err = err;
                                minp.rt1 = rt1;
                                minp.rt2 = rt2;
                                minp.ftIdx2 = ftIdx2;
#if DEBUG
                                debug3(() => $"min1 {minp}");
#endif
                            }
                        }
                    }
                }
                minp.err2 = calcErrByAverage(AVERAGE_ERR_DURATION, dt, minp);
#if DEBUG
                debug2(() => $"min2 {minp}");
#endif
            }

            delta = 0.005;
            double delta2 = 0.01;

            //var minps = minParams.OrderBy(p => p.err).Take(7).ToArray();
            var minps = minParams.OrderBy(p => p.err2).Take(5).ToArray();
            foreach (var minp in minps) {
                int cp = minp.days;
                int cp_idx = startIdx + cp;
                int cp_nxt = cp_idx + 1;
                double b1 = rt0;
                double rt1_beg = minp.rt1 - 0.05;
                double rt1_end = minp.rt1 + 0.05;
                double rt2_beg = minp.rt2 - 0.05;
                double rt2_end = minp.rt2 + 0.05;
                int ft2_beg = Math.Max(minp.ftIdx2 - 2, 0);
                int ft2_end = Math.Min(minp.ftIdx2 + 2, decayFactors2.Length);
                for (double rt1 = rt1_beg; rt1 <= rt1_end; rt1 += delta) {
                    var a1 = (rt1 - b1) / cp;
                    double err1 = calcSquareErr(a1, b1, startIdx, cp_nxt);
                    if (err1 < minp.err) {
                        for (int ftIdx2 = ft2_beg; ftIdx2 < ft2_end; ++ftIdx2) {
                            for (double rt2 = rt2_beg; rt2 <= rt2_end; rt2 += delta2) {
                                double err2 = calcSquareErr2(rt0, rt1, rt2, decayFactors2[ftIdx2], cp, cp_nxt + 1, rts.Length);
                                double err = err1 + err2;
                                if (err < minp.err) {
                                    minp.err = err;
                                    minp.rt1 = rt1;
                                    minp.rt2 = rt2;
                                    minp.ftIdx2 = ftIdx2;
#if DEBUG
                                    debug3(() => $"min3 {minp}");
#endif
                                }
                            }
                        }
                    }
                }
                minp.err2 = calcErrByAverage(AVERAGE_ERR_DURATION, dt, minp);
#if DEBUG
                debug2(() => $"min4 {minp}");
#endif
            }

            //return minps.Aggregate((min, w) => min.err <= w.err ? min : w);
            return minps.Aggregate((min, w) => min.err2 <= w.err2 ? min : w);
        }

        MinParams adjustFactor1AndMakeSubParams(int duration, DateTime dt, MinParams minParam)
        {
            int min_ftIdx1 = 0;
            double min_err = double.MaxValue;
            for (int ftIdx1 = 0; ftIdx1 < Pages.MyChart._decayFactors.Length; ++ftIdx1) {
                minParam.ftIdx1 = ftIdx1;
                double err = calcErrByAverage(duration, dt, minParam);
                if (err < min_err) {
                    min_err = err;
                    min_ftIdx1 = ftIdx1;
                }
#if DEBUG
                debug3(() => $"min_ft Err={err:f3}, ftIdx1={ftIdx1}");
#endif
            }
            minParam.err2 = min_err;
            minParam.ftIdx1 = min_ftIdx1;
#if DEBUG
            debug2(() =>  $"{Title}: adjust: duration={duration}, dt={dt._toDateString()}, {minParam}");
#endif
            return minParam;
        }

        double calcErrByAverage(int duration, DateTime dt, MinParams minParam)
        {
            int realDays = Dates.Length;
            RtDecayParam param = InitialDecayParam.Clone();
            param.StartDate = dt;
            minParam.CopyToDecayParam(param);
            var predData = UserPredictData.PredictValuesEx(this, param, realDays + 10, 5);  // 10 と 5 は適当
            return realDays._range(realDays - duration).Select(i => Math.Pow(predData.PredAverage[i] - Average[i], 2)).Sum();
        }

    }

}

