using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StandardCommon;

namespace ChartBlazorApp.Models
{
    /// <summary>
    /// 県別データファイルから作成される陽性者データクラス
    /// </summary>
    public class InfectData
    {
        private static ConsoleLog logger = ConsoleLog.GetLogger();

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

        public static InfectData DummyData { get {
                //logger.Warn($"Used");
                return _dummyData;
            } }

        private static readonly InfectData _dummyData = new InfectData() {
            InitialDecayParam = new RtDecayParam(),
            InitialSubParams = new SubParams() { StartDate = "2020/12/1" },
        };

        public string GetDecayStartDateStr(int duration)
        {
            var dt = GetDecayStartDate(duration);
            return dt._isValid() ? dt.ToShortDateString() : "";
        }

        public DateTime GetDecayStartDate(int duration)
        {
            if (InitialDecayParam.StartDate._isValid()) {
                // システム既定基準日が設定されていればそれを返す
                return InitialDecayParam.StartDate;
            }
            //return FindRecentMaxMinRtDate(localMaxRtDulation);
            return FindOldestExtremumRtDate(duration);
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

        /// <summary>
        /// 直近の極値の位置を求める
        /// </summary>
        /// <param name="rt"></param>
        /// <param name="comp"></param>
        /// <param name="localMaxRtDulation"></param>
        /// <returns></returns>
        private static int findRecentMaxMinIndex(double[] rt, Func<double, double, bool> comp, int localMaxRtDulation)
        {
            if (logger.DebugLevel > 1) {
                logger.Debug("ENTER");
            }

            if (localMaxRtDulation < 0) localMaxRtDulation = Constants.LOCAL_MAX_RT_BACK_DURATION;
            int mIdx = rt._safeCount();
            if (rt._notEmpty()) {
                int lastBackIdx = (rt.Length - Constants.MAX_BACK_DAYS - 1)._lowLimit(0);
                int lastIdx = rt.Length - 1;
                var mVal = rt[lastIdx];
                mIdx = lastIdx;
                int count = localMaxRtDulation;
                while (count-- > 0 && --lastIdx >= lastBackIdx) {
                    var v = rt[lastIdx];
                    if (v > Constants.RT_THRESHOLD && lastIdx < rt.Length - Constants.TAIL_GUARD_DURATION) break;
                    if (v <= Constants.RT_THRESHOLD && comp(v, mVal)) {
                        mVal = v;
                        mIdx = lastIdx;
                        count = localMaxRtDulation;
                    }
                }
            }
            if (logger.DebugLevel > 1) {
                logger.Debug("LEAVE");
            }
            return mIdx;
        }

        public DateTime FindOldestExtremumRtDate(int duration)
        {
            DateTime firstDt = Dates._first();
            if (firstDt._isValid()) {
                return firstDt.AddDays(findOldestExremumIndex(Rt, duration));
            } else {
                return Dates._last();
            }
        }

        /// <summary>
        /// 最大過去の極値の位置を求める
        /// </summary>
        /// <param name="rt"></param>
        /// <param name="comp"></param>
        /// <param name="localMaxRtDulation"></param>
        /// <returns></returns>
        private static int findOldestExremumIndex(double[] rt, int duration)
        {
            return findExremumIndexes(rt, duration).Min();
        }

        /// <summary>
        /// 極値の位置を求める
        /// </summary>
        /// <param name="rt"></param>
        /// <param name="comp"></param>
        /// <param name="localMaxRtDulation"></param>
        /// <returns></returns>
        private static int[] findExremumIndexes(double[] rt, int duration)
        {
            if (logger.DebugLevel >= 1) {
                logger.Debug("ENTER");
            }

            HashSet<int> result = new HashSet<int>();

            int halfDuration = ((duration < 1) ? Constants.EXTREMUM_DETECTION_DURATION : duration) / 2;
            //int mIdx = rt._safeCount();
            if (rt._notEmpty()) {
                int lastIdx = (rt.Length - Constants.MAX_EXTREMUM_BACK_DAYS - 1)._lowLimit(0);
                int lastValidIdx = lastIdx;
                bool invalidValue = true;
                int minimumIdx = lastIdx;
                int maximumIdx = lastIdx;
                var minimumVal = rt[lastIdx];
                var maximumVal = rt[lastIdx];
                logger.Debug(() => $"find Extremum: lastIdx={lastIdx}, rt.Len={rt.Length}");
                for (int i = lastIdx + 1; i < rt.Length; ++i) {
                    var v = rt[i];
                    if (v <= 0 || v > Constants.RT_THRESHOLD2) {
                        invalidValue = true;
                    } else if (v <= Constants.RT_THRESHOLD) {
                        if (invalidValue) {
                            lastValidIdx = i;
                            invalidValue = false;
                            minimumVal = v;
                            minimumIdx = i;
                            maximumVal = v;
                            maximumIdx = i;
                        } else {
                            if (v <= minimumVal) {
                                minimumVal = v;
                                minimumIdx = i;
                            }
                            if (v >= maximumVal) {
                                maximumVal = v;
                                maximumIdx = i;
                            }
                        }
                    }
                }
                result.Add(minimumIdx);
                result.Add(maximumIdx);
                //mIdx = Math.Min(minimumIdx, maximumIdx);
                //logger.Debug($"mIdx={mIdx}, minIdx={minimumIdx}, minVal={minimumVal:f3}, maxIdx={maximumIdx}, maxVal={maximumVal:f3}, rt.Length={rt.Length}");
                logger.Debug(() => $"minimumIdx={minimumIdx}, minVal={minimumVal:f3}, maximumIdx={maximumIdx}, maxVal={maximumVal:f3}, rt.Length={rt.Length}");
                logger.Debug(() => $"lastValidIdx={lastValidIdx}, lastIdx={lastIdx}, halfDuration={halfDuration}");

                int find_extremal_idx(int begin, int end, Func<double, double, bool> comp, string pfx)
                {
                    logger.Debug(() => $"{pfx}: begin={begin}, end={end}");
                    for (int i = begin; i <= end; ++i) {
                        var v = rt[i];
                        if (v > 0 && v <= Constants.RT_THRESHOLD) {
                            bool found = true;
                            for (int j = (i - halfDuration)._lowLimit(0); j <= i + halfDuration; ++j) {
                                if (j != i && comp(rt[j], v)) {
                                    found = false;
                                    break;
                                }
                            }
                            if (found) {
                                logger.Debug(() => $"{pfx}imalIdx={i}, {pfx}imalVal={v:f3}");
                                return i;
                            }
                        }
                    }
                    return int.MaxValue;
                }

                void find_extremal_indexes(int begin, int end, int earlyEnd, Func<double, double, bool> comp, string pfx)
                {
                    logger.Debug(() => $"{pfx}: begin={begin}, end={end}, earlyEnd={earlyEnd}");
                    bool found = false;
                    while (true) {
                        int mIdx = find_extremal_idx(begin, end, comp, "min");
                        if (mIdx > end || (found && mIdx > earlyEnd)) break;
                        result.Add(mIdx);
                        begin = mIdx + 1;
                        found = true;
                    }
                }

                //int begin = Helper.Array(lastValidIdx, lastIdx, halfDuration).Max();
                //int end = (rt.Length - halfDuration - 1)._highLimit(mIdx);
                int begin = Helper.Array(lastValidIdx, lastIdx, halfDuration).Max();
                int end = rt.Length - halfDuration - 1;
                int earlyEnd = rt.Length - Constants.EXTREMAL_POSTAMBLE_DAYS - 1;
                logger.Debug(() => $"find Extremal: begin={begin}, end={end}, earlyEnd={earlyEnd}");
                find_extremal_indexes(begin, end, earlyEnd, (a, b) => a < b, "min");
                find_extremal_indexes(begin, end, earlyEnd, (a, b) => a > b, "max");
                logger.Debug(() => $"Result: {result.Select(x => x.ToString())._join(", ")}");
            }
            if (logger.DebugLevel >= 1) {
                logger.Debug("LEAVE");
            }
            return result.ToArray();
        }

        public class MinParams
        {
            /// <summary> 基準日</summary>
            public DateTime startDt = DateTime.MaxValue;
            /// <summary> 変化日までの日数(基準日を0としたときのインデックス; 誤差の計算日数-1 なので注意)</summary>
            public int dayIdx = 0;
            public double errRt = Constants.MAX_ERROR;
            public double errAve = Constants.MAX_ERROR;
            public double rt1 = 0;
            public double rt2 = 0;
            public int ftIdx1 = 0;
            public int ftIdx2 = 8;

            public int ft1 { get { return Pages.MyChart._decayFactors._nth(ftIdx1); } }
            public int ft2 { get { return Pages.MyChart._decayFactors2._nth(ftIdx2); } }

            public SubParams MakeSubParams()
            {
                return new SubParams() {
                    StartDate = startDt._toDateString(),
                    Rt1 = rt1._round(3),
                    Rt2 = rt2._round(3),
                    DaysToOne = dayIdx,
                    DecayFactor = ft1,
                    DecayFactorNext = ft2,
                };
            }

            public RtDecayParam CopyToDecayParam(RtDecayParam param)
            {
                param.EasyRt1 = rt1;
                param.EasyRt2 = rt2;
                param.DaysToOne = dayIdx;
                param.DecayFactor = ft1;
                param.DecayFactorNext = ft2;
                return param;
            }

            public override string ToString()
            {
                return $"days={dayIdx}, "
                    + $"errRt={(errRt < double.MaxValue ? errRt : Constants.MAX_ERROR):f3}, "
                    + $"errAve={(errAve < double.MaxValue ? errAve : Constants.MAX_ERROR):f3}, "
                    + $"rt1={rt1:f3}, "
                    + $"rt2={rt2:f3}, "
                    + $"ftIdx1={ftIdx1}, "
                    + $"ftIdx2={ftIdx2}";
            }

            public double calcCoefficientA1(double rt0)
            {
                return Constants.CalcCoefficientA1(rt0, rt1, ft1, dayIdx);
            }

            public double calcCoefficientB1(double rt0, double a1)
            {
                return Constants.CalcCoefficientB1(rt0, a1, ft1);
            }

            public double calcExtendedRt1(double rt0, int dtIdx)
            {
                double a = calcCoefficientA1(rt0);
                double b = calcCoefficientB1(rt0, a);
                return Constants.CalcRt1(a, b, ft1, dtIdx);
            }

            public MinParams extendDaysAndCalcRt1(double rt0, int extendedDayIdx)
            {
                double a = calcCoefficientA1(rt0);
                double b = calcCoefficientB1(rt0, a);
                var rt = Constants.CalcRt1(a, b, ft1, extendedDayIdx);
                if (rt > 0) {
                    rt1 = rt;
                    dayIdx = extendedDayIdx;
                }
                return this;
            }
        }

        /// <summary>
        /// デフォルトの基準日、変化点、傾きの推計
        /// </summary>
        /// <param name="localDuration"></param>
        /// <param name="dt_"></param>
        /// <returns></returns>
        public SubParams CalcDecaySubParams()
        {
            return CalcDecaySubParamsEx(-1, null, 0, 0);
        }

        /// <summary>
        /// 基準日、変化点、傾きの推計
        /// (dt_: nullでなければ基準日の指定, days: 0 でなければ変化日までの日数)
        /// </summary>
        /// <param name="localDuration"></param>
        /// <param name="dt_"></param>
        /// <returns></returns>
        public SubParams CalcDecaySubParamsEx(int localDuration, DateTime? dt_, int days, int debugLevel)
        {
            if (dt_.HasValue) {
                logger.DebugNL();
                var minp = CalcDecaySubParams1(dt_.Value, days, debugLevel);
#if DEBUG
                minp.errAve = calcErrByAverage(Constants.AVERAGE_ERR_EXT_DURATION, minp);
                logger.Debug(() => $"Err duration={Constants.AVERAGE_ERR_EXT_DURATION}: {minp}");
                logger.DebugNL();
#endif
                return minp.MakeSubParams();
            } else {
                DateTime idxToDt(int idx) => idx < int.MaxValue ? Dates._first().AddDays(idx) : DateTime.MaxValue;
                return findExremumIndexes(Rt, localDuration).
                    Select(idx => {
                        var minp = CalcDecaySubParams1(idxToDt(idx), days, debugLevel);
                        minp.errAve = calcErrByAverage(Constants.AVERAGE_ERR_EXT_DURATION, minp);
#if DEBUG
                        logger.Debug(() => $"Err duration={Constants.AVERAGE_ERR_EXT_DURATION}: {minp}");
                        logger.DebugNL();
#endif
                        return minp;
                    }).
                    Aggregate((x, w) => x.errAve <= w.errAve ? x : w).
                    MakeSubParams();
            }
        }

        /// <summary>
        /// 基準日、変化点、傾きの推計
        /// (dt: 基準日の指定, days: 0 でなければ変化日までの日数)
        /// </summary>
        /// <param name="localDuration"></param>
        /// <param name="dt_"></param>
        /// <returns></returns>
        private MinParams CalcDecaySubParams1(DateTime dt, int days, int debugLevel)
        {
            logger.DebugLevel = debugLevel;

            if (debugLevel > 0) {
                // ブレークポイントを仕掛ける場所
                logger.DebugNL();
                logger.Debug(() => $"ENTER ---- {Title}, dt={dt._toDateString()}");
            }

            if (dt._notValid()) return new MinParams();

            double[] rts = Rt;
            int startIdx = (dt - Dates._first()).Days;

            double rt0 = Rt._nth(startIdx);

           // 1st stage
            var minParam = find_rt1(dt, Rt, startIdx, days);
            logger.Debug(() => $"{Title}_1: find_rt1: {minParam}");
            // rt1 と days の調整： days は STAGE1_MIN_DURATION 以上とする
            if (minParam.errRt < Constants.MAX_ERROR) {
                if (minParam.dayIdx < Constants.STAGE1_MIN_DURATION) {
                    int dura = minParam.dayIdx * 2;
                    if (dura > Constants.STAGE1_MIN_DURATION) dura = Constants.STAGE1_MIN_DURATION;
                    minParam.extendDaysAndCalcRt1(rt0, dura);
                }
                var rt1 = minParam.rt1;
                // rt2 の調整： rt1 の80%にする
                //var rt2 = (rt1 > 1) ? 1 + (rt1 - 1) * 0.8 : rt1 * 1.2 - rt0 * 0.2;
                // rt2 の調整： 上昇中なら上昇分*30%、下降中なら下降分*20%、さらに下げる
                var rt2 = (rt1 > rt0) ? rt1 - (rt1 - rt0) * 0.3 : rt1 - (rt0 - rt1) * 0.2;
                minParam.rt2 = rt2._lowLimit(0.001);
                minParam.ftIdx2 = (minParam.ftIdx1 - Pages.MyChart._decayFactors2Start)._lowLimit(minParam.ftIdx2);
                minParam.errAve = calcErrByAverage(Constants.AVERAGE_ERR_DURATION, minParam);
                logger.Debug(() => $"{Title}_2: adjusted duration={Constants.AVERAGE_ERR_DURATION}: {minParam}");
            }

           // both stage
            int duration = Rt._length() - startIdx - 1;
            if (duration >= Constants.STAGE2_MIN_DURATION) {
                var minp = find_both(dt, Rt, startIdx, days);
                logger.Debug(() => $"{Title}_3: find_both: {minp}");
                adjustFactor1AndMakeSubParams(Constants.AVERAGE_ERR_TAIL_DURATION, minp);
                //if (minp.err < err)
                if (minp.errAve < minParam.errAve) {
                    minParam = minp;
                }
            }
            //if (a > 0) a *= 0.9;
            if (debugLevel > 0) {
                // ブレークポイントを仕掛ける場所
                logger.Debug(() => $"LEAVE ---- {Title}_4: {minParam}");
                logger.DebugNL();
            }
            return minParam;
        }

        /// <summary>
        /// Rt1, dasyTo1, factor1 を求める
        /// </summary>
        /// <param name="rts"></param>
        /// <param name="startIdx"></param>
        /// <returns></returns>
        private MinParams find_rt1(DateTime dt, double[] rts, int startIdx, int days)
        {
            MinParams minp = new MinParams() { startDt = dt };
            if (rts._isEmpty() || startIdx < 0 || startIdx >= rts.Length) return minp;

            int daysToEnd = rts.Length - startIdx - 1;
            if (days > 0 && days < daysToEnd) return minp;

            int chgDtIdx = days < 1 ? daysToEnd : days._highLimit(100);  // 変日化のインデックス
            minp.dayIdx = chgDtIdx;

            double calcSquareErr(double rt0, double rt1, double factor1)
            {
                double a = Constants.CalcCoefficientA1(rt0, rt1, factor1, chgDtIdx);
                double b = Constants.CalcCoefficientB1(rt0, a, factor1);
                return rts.Skip(startIdx).Select((y, x) => Math.Pow(Constants.CalcRt1(a, b, factor1, x) - y, 2)).Sum();
            }

            double rt0 = rts[startIdx]._highLimit(Constants.RT_THRESHOLD2);
            int[] decayFactors1 = Pages.MyChart._decayFactors;

            double delta = 0.05;
            double tail_rt = rts[^1]._highLimit(Constants.RT_THRESHOLD2);
            double rt_beg = Math.Max(tail_rt - 0.5, 0);
            double rt_end = tail_rt + 0.5;
            for (double rt1 = rt_beg; rt1 <= rt_end; rt1 += delta) {
                for (int ftIdx1 = 0; ftIdx1 < decayFactors1.Length; ++ftIdx1) {
                    double err = calcSquareErr(rt0, rt1, decayFactors1[ftIdx1]);
                    logger.Trace4(() => $"find_rt1: err={err:f3}, rt1={rt1:f3}, ftIdx1={ftIdx1}");
                    if (err < minp.errRt) {
                        minp.errRt = err;
                        minp.rt1 = rt1;
                        minp.ftIdx1 = ftIdx1;
                        logger.Trace2(() => $"find_rt1: min1 {minp}");
                    }
                }
            }

            delta = 0.005;
            rt_beg = Math.Max(minp.rt1 - 0.05, 0);
            rt_end = minp.rt1 + 0.05;
            int ftIdx1_beg = Math.Max(minp.ftIdx1 - 1, 0);
            int ftIdx1_end = Math.Min(minp.ftIdx1 + 1, decayFactors1.Length - 1);
            for (double rt1 = rt_beg; rt1 <= rt_end; rt1 += delta) {
                for (int ftIdx1 = ftIdx1_beg; ftIdx1 <= ftIdx1_end; ++ftIdx1) {
                    double err = calcSquareErr(rt0, rt1, decayFactors1[ftIdx1]);
                    logger.Trace4(() => $"find_rt1: err={err:f3}, rt1={rt1:f3}, ftIdx1={ftIdx1}");
                    if (err < minp.errRt) {
                        minp.errRt = err;
                        minp.rt1 = rt1;
                        minp.ftIdx1 = ftIdx1;
                        logger.Trace2(() => $"find_rt1: min2 {minp}");
                    }
                }
            }
            logger.Debug(() => $"find_rt1: {minp}");
            return minp;
        }

        /// <summary>
        /// Rt1, Rt2, factor1, factor2 を求める
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="rts"></param>
        /// <param name="startIdx"></param>
        /// <returns></returns>
        private MinParams find_both(DateTime dt, double[] rts, int startIdx, int days)
        {
            if (rts._isEmpty() || startIdx < 0 || startIdx >= rts.Length || startIdx + days >= rts.Length) return new MinParams() { startDt = dt };

            double calcSquareErrForLinearRt1(double a, double b, int begin, int end)
            {
                return rts.Skip(begin).Take(end - begin + 1).Select((y, x) => Math.Pow(a * x + b - y, 2)).Sum();
            }

            double calcSquareErr2(double rt0, double rt1, double rt2, double factor2, int daysToOne, int begin, int end)
            {
                (double a2, double b2) = Constants.CalcCoefficients2(rt0, rt1, rt2, factor2, daysToOne);
                return rts.Skip(begin).Take(end - begin + 1).Select((y, x) => {
                    double rt = Constants.CalcRt2(a2, b2, rt1, rt2, factor2, x + daysToOne, x);
                    return Math.Pow(rt - y, 2);
                }).Sum();
            }

            
            int len = days < 1 ? rts.Length - startIdx : 0;
            // 変化日ごとの最小エラー値を保存
            MinParams[] minParams = new MinParams[len + 1];
            if (len == 0) {
                minParams[0] = new MinParams() { startDt = dt, dayIdx = days };
            } else {
                for (int i = 0; i < minParams.Length; ++i) { minParams[i] = new MinParams() { startDt = dt, dayIdx = i }; }  // days を初期化しておく
            }

            double rt0 = rts[startIdx]._highLimit(Constants.RT_THRESHOLD2);
            int[] decayFactors1 = Pages.MyChart._decayFactors;
            int[] decayFactors2 = Pages.MyChart._decayFactors2;

            int cp_mid = len / 2;
            int cp_margin = len / 4;
            int cp_beg = cp_margin;
            int cp_end = len - cp_margin;
            //double rt2_beg = Math.Max(rts[^3..].Min() - 0.25, 0);
            //double rt2_end = rts[^3..].Max() + 0.25;
            double tail_rt = rts[^1]._highLimit(Constants.RT_THRESHOLD2);
            //double rt2_low_limit = (tail_rt - Math.Abs(tail_rt - 1.0) * 0.5)._lowLimit(0);
            double rt2_low_limit = tail_rt > 1 ? 1 : 0;
            //double rt2_beg = (tail_rt - 0.25)._lowLimit(0);
            double rt2_beg = (tail_rt - 0.25)._lowLimit(rt2_low_limit);
            double rt2_end = tail_rt < 1 ? (tail_rt + 0.25)._highLimit(1) : tail_rt + 0.03;      // 末尾Rtが1未満なら、最終的なRtも1を超えないようにする。1以上なら、末尾Rtを最終Rtとする
            logger.Trace3(() => $"find_both: rough rt2_beg={rt2_beg:f3}, rt2_end={rt2_end:f3}");
            double delta = 0.05;
            foreach (var minp in minParams.Skip(cp_beg).Take(cp_end - cp_beg + 1)) {
                int cp = minp.dayIdx;
                int cp_idx = startIdx + cp;
                int cp_nxt = cp_idx + 1;
                double b1 = rt0;
                double cp_rt = rts[cp_idx]._highLimit(Constants.RT_THRESHOLD2);
                double rt_beg = Math.Max(cp_rt - 0.25, 0);
                double rt_end = cp_rt + 0.25;
                for (double rt1 = rt_beg; rt1 <= rt_end; rt1 += delta) {
                    var a1 = (rt1 - b1) / cp;
                    double err1 = calcSquareErrForLinearRt1(a1, b1, startIdx, cp_nxt);

                    for (int ftIdx2 = 0; ftIdx2 < decayFactors2.Length; ++ftIdx2) {
                        for (double rt2 = rt2_beg; rt2 <= rt2_end; rt2 += delta) {
                            //var rt2 = rts.Last();
                            double err2 = calcSquareErr2(rt0, rt1, rt2, decayFactors2[ftIdx2], cp, cp_nxt + 1, rts.Length);
                            double err = err1 + err2;
                            if (err < minp.errRt) {
                                minp.errRt = err;
                                minp.rt1 = rt1;
                                minp.rt2 = rt2;
                                minp.ftIdx2 = ftIdx2;
                                logger.Trace4(() => $"find_both: rough {minp}");
                            }
                        }
                    }
                }
                logger.Trace3(() => $"find_both: rough minp={minp}");
                // 下記調整はあまりよろしくない
                //adjustFactor1AndMakeSubParams(Constants.AVERAGE_ERR_TAIL_DURATION, minp);
                //adjustFactor1AndMakeSubParams(Constants.AVERAGE_ERR_DURATION, minp);
                //adjustFactor1AndMakeSubParams(Constants.AVERAGE_ERR_EXT_DURATION, minp);
                minp.errAve = calcErrByAverage(Constants.AVERAGE_ERR_DURATION, minp);
                logger.Trace3(() => $"find_both: rough duration={Constants.AVERAGE_ERR_DURATION}: {minp}");
            }

            delta = 0.005;
            double delta2 = 0.01;

            //var minps = minParams.OrderBy(p => p.err).Take(7).ToArray();
            var minps = minParams.OrderBy(p => p.errAve).Take(5).ToArray();
            foreach (var minp in minps) {
                int cp = minp.dayIdx;
                int cp_idx = startIdx + cp;
                int cp_nxt = cp_idx + 1;
                double b1 = rt0;
                double rt1_beg = minp.rt1 - 0.05;
                double rt1_end = minp.rt1 + 0.05;
                //rt2_beg = minp.rt2 - 0.05;
                rt2_beg = (minp.rt2 - 0.05)._lowLimit(rt2_low_limit);
                rt2_end = minp.rt2 + 0.05;
                //rt2_end = minp.rt2 < 1 ? rt2_end._highLimit(1) : minp.rt2 < tail_rt ? rt2_end._highLimit(tail_rt) : rt2_end;      // 末尾Rtが1未満なら、最終的なRtも1を超えないようにする。1以上なら、末尾Rtを最終Rtとする
                rt2_end = minp.rt2 < 1 ? rt2_end._highLimit(1) : rt2_end._highLimit(tail_rt);      // 末尾Rtが1未満なら、最終的なRtも1を超えないようにする。1以上なら、末尾Rtを最終Rtとする
                logger.Trace3(() => $"find_both: fine rt2_beg={rt2_beg:f3}, rt2_end={rt2_end:f3}");
                int ft2_beg = Math.Max(minp.ftIdx2 - 2, 0);
                int ft2_end = Math.Min(minp.ftIdx2 + 2, decayFactors2.Length);
                minp.errRt = Constants.MAX_ERROR;   // 誤差を初期化しておく(必ずフェーズ2の解が用いられるようにするため)
                for (double rt1 = rt1_beg; rt1 <= rt1_end; rt1 += delta) {
                    var a1 = (rt1 - b1) / cp;
                    double err1 = calcSquareErrForLinearRt1(a1, b1, startIdx, cp_nxt);
                    if (err1 < minp.errRt) {
                        for (int ftIdx2 = ft2_beg; ftIdx2 < ft2_end; ++ftIdx2) {
                            for (double rt2 = rt2_beg; rt2 <= rt2_end; rt2 += delta2) {
                                double err2 = calcSquareErr2(rt0, rt1, rt2, decayFactors2[ftIdx2], cp, cp_nxt + 1, rts.Length);
                                double err = err1 + err2;
                                if (err < minp.errRt) {
                                    minp.errRt = err;
                                    minp.rt1 = rt1;
                                    minp.rt2 = rt2;
                                    minp.ftIdx2 = ftIdx2;
                                    logger.Trace4(() => $"find_both: fine {minp}");
                                }
                            }
                        }
                    }
                }
                logger.Trace3(() => $"find_both: fine minp={minp}");
                minp.errAve = calcErrByAverage(Constants.AVERAGE_ERR_DURATION, minp);
                logger.Trace3(() => $"find_both: fine duration={Constants.AVERAGE_ERR_DURATION}:  {minp}");
            }

            //return minps.Aggregate((min, w) => min.err <= w.err ? min : w);
            return minps.Aggregate((min, w) => min.errAve <= w.errAve ? min : w);
        }

        MinParams adjustFactor1AndMakeSubParams(int duration, MinParams minParam)
        {
            int min_ftIdx1 = 0;
            double min_err = double.MaxValue;
            for (int ftIdx1 = 0; ftIdx1 < Pages.MyChart._decayFactors.Length; ++ftIdx1) {
                minParam.ftIdx1 = ftIdx1;
                double err = calcErrByAverage(duration, minParam);
                if (err < min_err) {
                    min_err = err;
                    min_ftIdx1 = ftIdx1;
                }
                logger.Trace4(() => $"adjust: min_ft Err={err:f3}, ftIdx1={ftIdx1}");
            }
            minParam.errAve = min_err;
            minParam.ftIdx1 = min_ftIdx1;
            logger.Trace(() =>  $"{Title}: adjust: duration={duration}, dt={minParam.startDt._toDateString()}, {minParam}");
            return minParam;
        }

        double calcErrByAverage(int duration, MinParams minParam)
        {
            int realDays = Dates.Length;
            RtDecayParam param = InitialDecayParam.Clone();
            param.StartDate = minParam.startDt;
            minParam.CopyToDecayParam(param);
            var predData = UserPredictData.PredictValuesEx(this, param, realDays + 10, 5);  // 10 と 5 は適当
            return realDays._range(realDays - duration).Select(i => Math.Pow(predData.PredAverage[i] - Average[i], 2)).Sum();
        }

    }

}
