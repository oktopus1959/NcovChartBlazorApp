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

        public double[] PosiRates { get; set; }

        public double[] Rt { get; set; }

        /// <summary> イベント("日付:アノテーション,..."形式) </summary>
        public string Events { get; set; }

        /// <summary> 県別データファイルから取得した設定値(Layer-3) </summary>
        public RtDecayParam InitialDecayParam { get; set; }

        /// <summary> 県別データファイルから事前に作成された予測設定値(Layer-2) </summary>
        public SubParams InitialSubParams { get; set; }

        public PrefInfectData PrefData { get; set; }

        public InfectData CreateData(int[] extraData)
        {
            return PrefData.MakeData(extraData, InitialSubParams);
        }

        public InfectData ShiftStartDate(DateTime startDt)
        {
            InfectData data = (InfectData)this.MemberwiseClone();
            int idx = Dates._findIndex(startDt);
            if (idx > 0) {
                data.Dates = Dates.Skip(idx).ToArray();
                data.Newly = Newly.Skip(idx).ToArray();
                data.Average = Average.Skip(idx).ToArray();
                data.PosiRates = PosiRates.Skip(idx).ToArray();
                data.Rt = Rt.Skip(idx).ToArray();
            }
            return data;
        }

        public static InfectData DummyData { get; } = new InfectData() {
            Dates = new DateTime[] { DateTime.Now._toDate() },
            Newly = new double[] { 1 },
            Average = new double[] { 1 },
            PosiRates = new double[] { 1 },
            Rt = new double[] { 1 },
            InitialDecayParam = new RtDecayParam(),
            InitialSubParams = new SubParams() { StartDate = "2020/12/1" },
        };

        public string GetDecayStartDateStr(int duration)
        {
            var dt = getDecayStartDate(duration);
            return dt._isValid() ? dt.ToShortDateString() : "";
        }

        private DateTime getDecayStartDate(int duration)
        {
            if (InitialDecayParam.StartDate._isValid()) {
                // システム既定基準日が設定されていればそれを返す
                return InitialDecayParam.StartDate;
            }
            //return FindRecentMaxMinRtDate(localMaxRtDulation);
            return findOldestExtremumRtDate(duration);
        }

        //public string FindRecentMaxRtDateStr(int localMaxRtDulation)
        //{
        //    DateTime dt = FindRecentMaxMinRtDate(localMaxRtDulation);
        //    return dt._isValid() ? dt.ToShortDateString() : "";
        //}

        //public DateTime FindRecentMaxMinRtDate(int localMaxRtDulation)
        //{
        //    DateTime firstDt = Dates._first();
        //    if (firstDt._isValid()) {
        //        return firstDt.AddDays(FindRecentMaxMinIndex(Rt, localMaxRtDulation));
        //    } else {
        //        return Dates._last();
        //    }
        //}

        //public int FindRecentMaxMinIndex(int localMaxRtDulation)
        //{
        //    return FindRecentMaxMinIndex(Rt, localMaxRtDulation);
        //}

        //public static int FindRecentMaxMinIndex(double[] rt, int localMaxRtDulation)
        //{
        //    return Math.Min(
        //        findRecentMaxMinIndex(rt, (v, m) => v > m, localMaxRtDulation),
        //        findRecentMaxMinIndex(rt, (v, m) => v < m, localMaxRtDulation));
        //}

        //public static int FindRecentMinIndex(double[] rt, int localMaxRtDulation)
        //{
        //    return findRecentMaxMinIndex(rt, (v, m) => v < m, localMaxRtDulation);
        //}

        ///// <summary>
        ///// 直近の極値の位置を求める
        ///// </summary>
        ///// <param name="rt"></param>
        ///// <param name="comp"></param>
        ///// <param name="localMaxRtDulation"></param>
        ///// <returns></returns>
        //private static int findRecentMaxMinIndex(double[] rt, Func<double, double, bool> comp, int localMaxRtDulation)
        //{
        //    logger.Trace("ENTER");

        //    if (localMaxRtDulation < 0) localMaxRtDulation = Constants.LOCAL_MAX_RT_BACK_DURATION;
        //    int mIdx = rt._safeCount();
        //    if (rt._notEmpty()) {
        //        int lastBackIdx = (rt.Length - Constants.MAX_BACK_DAYS - 1)._lowLimit(0);
        //        int lastIdx = rt.Length - 1;
        //        var mVal = rt[lastIdx];
        //        mIdx = lastIdx;
        //        int count = localMaxRtDulation;
        //        while (count-- > 0 && --lastIdx >= lastBackIdx) {
        //            var v = rt[lastIdx];
        //            if (v > Constants.RT_THRESHOLD && lastIdx < rt.Length - Constants.TAIL_GUARD_DURATION) break;
        //            if (v <= Constants.RT_THRESHOLD && comp(v, mVal)) {
        //                mVal = v;
        //                mIdx = lastIdx;
        //                count = localMaxRtDulation;
        //            }
        //        }
        //    }
        //    logger.Trace("LEAVE");
        //    return mIdx;
        //}

        /// <summary>
        /// 最大過去の極値の日付を求める
        /// </summary>
        private DateTime findOldestExtremumRtDate(int duration)
        {
            DateTime firstDt = Dates._first();
            if (firstDt._isValid()) {
                return firstDt.AddDays(findExremumIndexes(Rt, duration).Min());
            } else {
                return Dates._last();
            }
        }

        ///// <summary>
        ///// 最大過去の極値の位置を求める
        ///// </summary>
        ///// <param name="rt"></param>
        ///// <param name="comp"></param>
        ///// <param name="localMaxRtDulation"></param>
        ///// <returns></returns>
        //private static int findOldestExremumIndex(double[] rt, int duration)
        //{
        //    return findExremumIndexes(rt, duration).Min();
        //}

        /// <summary>
        /// 候補極値の位置(複数あり)を求める
        /// </summary>
        /// <param name="rt"></param>
        /// <param name="comp"></param>
        /// <param name="localMaxRtDulation"></param>
        /// <returns></returns>
        private static int[] findExremumIndexes(double[] rt, int duration)
        {
            logger.Trace("ENTER");

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
                int minSlopeIdx = lastIdx;
                int maxSlopeIdx = lastIdx;
                int earlyEnd = rt.Length - Constants.MULTI_EXTREMAL_POST_MARGIN_DAYS - 1;
                int earlySlopeEnd = rt.Length - Math.Max(Constants.MULTI_EXTREMAL_POST_MARGIN_DAYS, Constants.MAX_SLOPE_DETECTION_DURATION) - 1;
                double calcSlope(int idx) => idx <= earlySlopeEnd ? rt[idx + Constants.MAX_SLOPE_DETECTION_DURATION] - rt[idx] : 0;
                var minSlopeVal = calcSlope(lastIdx);
                var maxSlopeVal = minSlopeVal;
                logger.Trace(() => $"find Extremum: lastIdx={lastIdx}, rt.Len={rt.Length}");
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
                            minSlopeVal = calcSlope(i);
                            minSlopeIdx = i;
                            maxSlopeVal = minSlopeVal;
                            maxSlopeIdx = i;
                        } else {
                            if (v <= minimumVal) {
                                minimumVal = v;
                                minimumIdx = i;
                            }
                            if (v >= maximumVal) {
                                maximumVal = v;
                                maximumIdx = i;
                            }
                            var slp = calcSlope(i);
                            if (slp <= minSlopeVal) {
                                minSlopeVal = slp;
                                minSlopeIdx = i;
                            }
                            if (slp >= maxSlopeVal) {
                                maxSlopeVal = slp;
                                maxSlopeIdx = i;
                            }
                        }
                    }
                }
                result.Add(minimumIdx);
                result.Add(maximumIdx);
                result.Add(minSlopeIdx);
                result.Add(maxSlopeIdx);
                //mIdx = Math.Min(minimumIdx, maximumIdx);
                //logger.Trace($"mIdx={mIdx}, minIdx={minimumIdx}, minVal={minimumVal:f3}, maxIdx={maximumIdx}, maxVal={maximumVal:f3}, rt.Length={rt.Length}");
                logger.Trace(() => $"minimumIdx={minimumIdx}, minVal={minimumVal:f3}, maximumIdx={maximumIdx}, maxVal={maximumVal:f3}, minSlopeIdx={minSlopeIdx}, maxSlopeIdx={maxSlopeIdx}, rt.Length={rt.Length}");
                logger.Trace(() => $"lastValidIdx={lastValidIdx}, lastIdx={lastIdx}, halfDuration={halfDuration}");

                int find_extremal_idx(int begin, int end, Func<double, double, bool> comp, string pfx)
                {
                    logger.Trace(() => $"find_extremal_idx: {pfx}: begin={begin}, end={end}");
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
                                logger.Trace(() => $"find_extremal_idx: FOUND: {pfx}imalIdx={i}, {pfx}imalVal={v:f3}");
                                return i;
                            }
                        }
                    }
                    return int.MaxValue;
                }

                void find_extremal_indexes(int begin, int end, Func<double, double, bool> comp, string pfx)
                {
                    logger.Trace(() => $"find_extremal_indexes: {pfx}: begin={begin}, end={end}, earlyEnd={earlyEnd}");
                    bool found = false;
                    while (true) {
                        int mIdx = find_extremal_idx(begin, end, comp, pfx);
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
                logger.Trace(() => $"find Extremal: begin={begin}, end={end}, earlyEnd={earlyEnd}");
                find_extremal_indexes(begin, end, (a, b) => a < b, "min");
                find_extremal_indexes(begin, end, (a, b) => a > b, "max");
                logger.Trace(() => $"Result: {result.Select(x => x.ToString())._join(", ")}");
            }
            logger.Trace("LEAVE");
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

            public MinParams extendDaysAndCalcRt1(double rt0, int extendedDayIdx)
            {
                //double a = calcCoefficientA1(rt0);
                //double b = calcCoefficientB1(rt0, a);
                (double a, double b) = Constants.CalcCoefficient1(rt0, rt1, ft1, dayIdx);
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
            return CalcDecaySubParamsEx(-1, null, 0);
        }

        /// <summary>
        /// 基準日、変化点、傾きの推計
        /// (dt_: nullでなければ基準日の指定, days: 0 でなければ変化日までの日数)
        /// </summary>
        /// <param name="localDuration"></param>
        /// <param name="dt_"></param>
        /// <returns></returns>
        public SubParams CalcDecaySubParamsEx(int localDuration, DateTime? dt_, int days, int debugLevel = 0)
        {
            logger.Trace(() => $"\nENTER: debugLevel={debugLevel}");
            logger.SetLocalDebugLevel(debugLevel);
            try {

                if (dt_.HasValue) {
                    logger.DebugNL();
                    var minp = CalcDecaySubParams1(dt_.Value, days);
                    minp.errAve = calcErrByAverage(Constants.AVERAGE_ERR_EXT_DURATION, minp);
                    logger.Debug(() => $"Err duration={Constants.AVERAGE_ERR_EXT_DURATION}: {minp}");
                    logger.DebugNL();
                    return minp.MakeSubParams();
                } else {
                    DateTime idxToDt(int idx) => idx < int.MaxValue ? Dates._first().AddDays(idx) : DateTime.MaxValue;
                    return findExremumIndexes(Rt, localDuration).
                        Select(idx => {
                            var minp = CalcDecaySubParams1(idxToDt(idx), days);
                            minp.errAve = calcErrByAverage(Constants.AVERAGE_ERR_EXT_DURATION, minp);
                            logger.Debug(() => $"findExtremum: Err duration={Constants.AVERAGE_ERR_EXT_DURATION}: {minp}\n");
                            return minp;
                        }).
                        Aggregate((x, w) => x.errAve <= w.errAve ? x : w).
                        MakeSubParams();
                }
            } finally {
                logger.ResetLocalDebugLevel();
                logger.Trace("LEAVE\n");
            }
        }

        /// <summary>
        /// 基準日、変化点、傾きの推計
        /// (dt: 基準日の指定, days: 0 でなければ変化日までの日数)
        /// </summary>
        /// <param name="localDuration"></param>
        /// <param name="dt_"></param>
        /// <returns></returns>
        private MinParams CalcDecaySubParams1(DateTime dt, int days)
        {
            logger.Trace(() => $"\nENTER ---- {Title}, dt={dt._toDateString()}"); // ブレークポイントを仕掛ける場所

            if (dt._notValid()) return new MinParams();

            double[] rts = Rt;
            int startIdx = (dt - Dates._first()).Days;

            double rt0 = Rt._nth(startIdx);

            // 1st stage
            var minParam = find_rt1(dt, Rt, startIdx, days);
            logger.Trace(() => $"{Title}_1: find_rt1: {minParam}");
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
                logger.Trace(() => $"{Title}_2: adjusted duration={Constants.AVERAGE_ERR_DURATION}: {minParam}");
            }

            // both stage
            int duration = Rt._length() - startIdx - 1;
            if (duration >= Constants.STAGE2_MIN_DURATION) {
                var minp = find_both(dt, Rt, startIdx, days);
                logger.Trace(() => $"{Title}_3: find_both: {minp}");
                adjustFactor1AndMakeSubParams(Constants.AVERAGE_ERR_TAIL_DURATION, minp);
                //if (minp.err < err)
                if (minp.errAve < minParam.errAve) {
                    minParam = minp;
                }
            }
            //if (a > 0) a *= 0.9;
            logger.Trace(() => $"LEAVE ---- {Title}_4: {minParam}\n");    // ブレークポイントを仕掛ける場所

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
                //double a = Constants.CalcCoefficientA1(rt0, rt1, factor1, chgDtIdx);
                //double b = Constants.CalcCoefficientB1(rt0, a, factor1);
                (double a, double b) = Constants.CalcCoefficient1(rt0, rt1, factor1, chgDtIdx);
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
                    logger.Trace3(() => $"find_rt1: err={err:f3}, rt1={rt1:f3}, ftIdx1={ftIdx1}");
                    if (err < minp.errRt) {
                        minp.errRt = err;
                        minp.rt1 = rt1;
                        minp.ftIdx1 = ftIdx1;
                        logger.Trace2(() => $"find_rt1: min1 {minp}");
                    }
                }
            }

            double delta1 = 0.005;
            rt_beg = Math.Max(minp.rt1 - delta, 0);
            rt_end = minp.rt1 + delta;
            int ftIdx1_beg = Math.Max(minp.ftIdx1 - 1, 0);
            int ftIdx1_end = Math.Min(minp.ftIdx1 + 1, decayFactors1.Length - 1);
            for (double rt1 = rt_beg; rt1 <= rt_end; rt1 += delta1) {
                for (int ftIdx1 = ftIdx1_beg; ftIdx1 <= ftIdx1_end; ++ftIdx1) {
                    double err = calcSquareErr(rt0, rt1, decayFactors1[ftIdx1]);
                    logger.Trace3(() => $"find_rt1: err={err:f3}, rt1={rt1:f3}, ftIdx1={ftIdx1}");
                    if (err < minp.errRt) {
                        minp.errRt = err;
                        minp.rt1 = rt1;
                        minp.ftIdx1 = ftIdx1;
                        logger.Trace2(() => $"find_rt1: min2 {minp}");
                    }
                }
            }
            logger.Trace(() => $"find_rt1: {minp}");
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

            // daysToOneの翌日からrtsの終わりまでを累算する
            double calcSquareErr2(double rt0, double rt1, double rt2, double factor2, int daysToOne)
            {
                (double a2, double b2) = Constants.CalcCoefficients2(rt0, rt1, rt2, factor2, daysToOne);
                int rt1Idx = startIdx + daysToOne;
                return rts.Length._range(rt1Idx + 1).Select(x => {
                    double rt = Constants.CalcRt2(a2, b2, rt1, rt2, factor2, x - startIdx, x - rt1Idx);
                    return Math.Pow(rt - rts[x], 2);
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

            //double delta = 0.02;
            double delta = 0.05;
            double rt_margin = delta * 5;

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
            double rt2_beg = (tail_rt - rt_margin)._lowLimit(rt2_low_limit);
            double rt2_end = tail_rt < 1 ? (tail_rt + rt_margin)._highLimit(1) : tail_rt + 0.03;      // 末尾Rtが1未満なら、最終的なRtも1を超えないようにする。1以上なら、末尾Rtを最終Rtとする
            logger.Trace4(() => $"find_both: rough rt2_beg={rt2_beg:f3}, rt2_end={rt2_end:f3}");

            foreach (var minp in minParams.Skip(cp_beg).Take(cp_end - cp_beg + 1)) {
                int cp = minp.dayIdx;
                int cp_idx = startIdx + cp;
                int cp_nxt_idx = cp_idx + 1;
                double b1 = rt0;
                double cp_rt = rts[cp_idx]._highLimit(Constants.RT_THRESHOLD2);
                double rt_beg = Math.Max(cp_rt - delta * 5, 0);
                double rt_end = cp_rt + delta * 5;
                for (double rt1 = rt_beg; rt1 <= rt_end; rt1 += delta) {
                    var a1 = (rt1 - b1) / cp;
                    double err1 = calcSquareErrForLinearRt1(a1, b1, startIdx, cp_nxt_idx);

                    for (int ftIdx2 = 0; ftIdx2 < decayFactors2.Length; ++ftIdx2) {
                        for (double rt2 = rt2_beg; rt2 <= rt2_end; rt2 += delta) {
                            //if (Title == "大阪" && dt.Day == 24 && ((cp == 10 && rt1 >= 1.002 && rt1 < 1.004 && rt2 >= 1.594 && rt2 <= 1.596 && ftIdx2 <= 1) || (cp == 12 && rt1 >= 1.03 && rt2 >= 1.59 && ftIdx2 <= 1))) {
                            //    logger.Trace("BREAK POINT");
                            //}
                            double err2 = calcSquareErr2(rt0, rt1, rt2, decayFactors2[ftIdx2], cp);
                            double err = err1 + err2;
                            if (cp == 12) logger.Trace5(() => $"find_both: rough: err={err:f3}, err1={err1:f3}, err2={err2:f3}, days={cp}, rt1={rt1:f3}, rt2={rt2:f3}, ftIdx2={ftIdx2}");
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
                logger.Trace4(() => $"find_both: rough minp={minp}");
                // 下記調整はあまりよろしくない
                //adjustFactor1AndMakeSubParams(Constants.AVERAGE_ERR_TAIL_DURATION, minp);
                //adjustFactor1AndMakeSubParams(Constants.AVERAGE_ERR_DURATION, minp);
                //adjustFactor1AndMakeSubParams(Constants.AVERAGE_ERR_EXT_DURATION, minp);
                minp.errAve = calcErrByAverage(Constants.AVERAGE_ERR_DURATION, minp);
                logger.Trace4(() => $"find_both: rough duration={Constants.AVERAGE_ERR_DURATION}: {minp}");
            }

            double delta1 = delta / 10;     // 0.005;
            double delta2 = 0.01;
            double rt1_margin = delta1 * 5; // 10
            double rt2_margin = delta2 * 5;

            //var minps = minParams.OrderBy(p => p.err).Take(7).ToArray();
            var minps = minParams.OrderBy(p => p.errAve).Take(5).ToArray();
            foreach (var minp in minps) {
                int cp = minp.dayIdx;
                int cp_idx = startIdx + cp;
                int cp_nxt_idx = cp_idx + 1;
                double b1 = rt0;
                double rt1_beg = minp.rt1 - rt1_margin;
                double rt1_end = minp.rt1 + rt1_margin;
                //rt2_beg = minp.rt2 - 0.05;
                rt2_beg = (minp.rt2 - rt2_margin)._lowLimit(rt2_low_limit);
                rt2_end = minp.rt2 + rt2_margin;
                //rt2_end = minp.rt2 < 1 ? rt2_end._highLimit(1) : minp.rt2 < tail_rt ? rt2_end._highLimit(tail_rt) : rt2_end;      // 末尾Rtが1未満なら、最終的なRtも1を超えないようにする。1以上なら、末尾Rtを最終Rtとする
                rt2_end = minp.rt2 < 1 ? rt2_end._highLimit(1) : rt2_end._highLimit(tail_rt);      // 末尾Rtが1未満なら、最終的なRtも1を超えないようにする。1以上なら、末尾Rtを最終Rtとする
                logger.Trace4(() => $"find_both: fine rt2_beg={rt2_beg:f3}, rt2_end={rt2_end:f3}");
                int ft2_beg = Math.Max(minp.ftIdx2 - 2, 0);
                int ft2_end = Math.Min(minp.ftIdx2 + 2, decayFactors2.Length);
                minp.errRt = Constants.MAX_ERROR;   // 誤差を初期化しておく(必ずフェーズ2の解が用いられるようにするため)
                for (double rt1 = rt1_beg; rt1 <= rt1_end; rt1 += delta1) {
                    var a1 = (rt1 - b1) / cp;
                    double err1 = calcSquareErrForLinearRt1(a1, b1, startIdx, cp_nxt_idx);
                    if (err1 < minp.errRt) {
                        for (int ftIdx2 = ft2_beg; ftIdx2 < ft2_end; ++ftIdx2) {
                            for (double rt2 = rt2_beg; rt2 <= rt2_end; rt2 += delta2) {
                                double err2 = calcSquareErr2(rt0, rt1, rt2, decayFactors2[ftIdx2], cp);
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
                logger.Trace4(() => $"find_both: fine minp={minp}");
                minp.errAve = calcErrByAverage(Constants.AVERAGE_ERR_DURATION, minp);
                logger.Trace4(() => $"find_both: fine duration={Constants.AVERAGE_ERR_DURATION}:  {minp}");
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
            logger.Debug(() => $"{Title}: adjust: duration={duration}, dt={minParam.startDt._toDateString()}, {minParam}");
            return minParam;
        }

        double calcErrByAverage(int duration, MinParams minParam)
        {
            int realDays = Dates.Length;
            RtDecayParam param = InitialDecayParam.Clone();
            param.StartDate = minParam.startDt;
            minParam.CopyToDecayParam(param);
            var predData = UserPredictData.PredictValuesEx(this, param, realDays + 10, 5);  // 10 と 5 は適当
            return predData.CalcAverageMSE(Average, realDays - duration, realDays);
            //return realDays._range(realDays - duration).Select(i => Math.Pow(predData.PredAverage[i] - Average[i], 2)).Sum();
        }

    } // class InfectData

    public class PrefInfectData
    {
        private static ConsoleLog logger = ConsoleLog.GetLogger();

        private static DateTime _firstDate = Constants.FIRST_EFFECTIVE_DATE._parseDateTime();

        private string Title { get; set; }

        private string Events { get; set; }

        private (DateTime, DateTime)[] ShiftRanges { get; set; }

        private double Y1_Max { get; set; }
        private double Y2_Max { get; set; }
        private RtDecayParam DecayParam { get; set; } = new RtDecayParam();
        //public List<DateTime> Dates { get; set; }
        //public List<double> Total { get; set; }
        //public int PreDataNum { get; set; }

        private Dictionary<DateTime, double> posiCumulatives = new Dictionary<DateTime, double>();
        private Dictionary<DateTime, double> testCumulatives = new Dictionary<DateTime, double>();

        public void InitializeIfNecessary(string dispName)
        {
            if (Title._isEmpty()) {
                Title = dispName;
                //DecayParam = new RtDecayParam();   // 全て 0 の初期データ
                //Dates = new List<DateTime>();
                //Total = new List<double>();
            }
        }

        public void AddEvents(string[] events)
        {
            Events = events._join(",");
            if (Events._notEmpty()) logger.Debug($"{Title}: Events: {Events}");
        }

        public void AddShiftRanges(string[] ranges)
        {
            (DateTime, DateTime) toDtPair(string[] dates) { return (dates._first()._parseDateTime(), dates._second()._parseDateTime(true)); }
            ShiftRanges = ranges.Select(r => toDtPair(r._split(':', true))).ToArray();
        }

        public void ShiftPrefData()
        {
            if (ShiftRanges._notEmpty()) {
                var minDt = posiCumulatives.Keys.Min();
                var maxDt = posiCumulatives.Keys.Max();
                foreach (var pair in ShiftRanges) {
                    var end = pair.Item2._highLimit(maxDt);
                    var dt = pair.Item1._lowLimit(minDt);
                    while (dt <= end) {
                        posiCumulatives[dt.AddDays(-1)] = posiCumulatives[dt];
                        dt = dt.AddDays(1);
                    }
                    if (end == maxDt) posiCumulatives.Remove(end);
                }
            }
        }

        public void AddYAxesMax(double y1Max, double y2Max)
        {
            Y1_Max = y1Max;
            Y2_Max = y2Max;
        }

        public void AddDecayParam(DateTime startDt, int daysToOne, double factor1, double rt1, double rt2, double factor2)
        {
            DecayParam.StartDate = startDt;
            DecayParam.DaysToOne = daysToOne;
            DecayParam.DecayFactor = factor1;
            DecayParam.EasyRt1 = rt1;
            DecayParam.EasyRt2 = rt2;
            DecayParam.DecayFactorNext = factor2;

        }

        public void AddData(DateTime dt, double nPosi, double nTest, string flag)
        {
            if (flag._isEmpty()) {
                posiCumulatives[dt] = nPosi;
                testCumulatives[dt] = nTest;
            } else {
                if (flag._startsWith("O") || !posiCumulatives.ContainsKey(dt)) {    // O: OverWrite
                    posiCumulatives[dt] = posiCumulatives._safeGet(dt.AddDays(-1)) + nPosi;
                }
            }
        }

        private double[] adjustTotal(double[] total)
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

        private double calcY1Max(double[] newlies)
        {
            int newlyMax = (int)(newlies[(newlies.Length - Constants.Y_MAX_CALC_DURATION)._lowLimit(0)..].Max() / 0.9);
            if (newlyMax < 100) {
                return ((newlyMax + 10) / 10) * 10.0;
            } else if (newlyMax < 150) {
                return 150;
            } else if (newlyMax < 1000) {
                return ((newlyMax + 100) / 100) * 100.0;
            } else if (newlyMax < 1500) {
                return 1500;
            } else if (newlyMax < 10000) {
                return ((newlyMax + 1000) / 1000) * 1000.0;
            } else if (newlyMax < 15000) {
                return 15000;
            } else if (newlyMax < 100000) {
                return ((newlyMax + 10000) / 10000) * 10000.0;
            } else if (newlyMax < 150000) {
                return 150000;
            } else {
                return ((newlyMax + 100000) / 100000) * 100000.0;
            }
        }

        private double calcY2Max(double[] rts)
        {
            int nearPt = (rts.Length - 30)._lowLimit(0);
            int longPt = (rts.Length - Constants.Y_MAX_CALC_DURATION)._lowLimit(0);
            if (nearPt > 0 && rts[longPt..nearPt].Count((x) => x > 2.5) > 3)
                return 5.0;
            if (rts.Length > 0 && rts[nearPt..].Max() > 2.5)
                return 5.0;
            if (nearPt > 0 && rts[longPt..nearPt].Count((x) => x > 2.0) > 3)
                return 2.5;
            if (rts.Length > 0 && rts[nearPt..].Max() > 2.0)
                return 2.5;
            return 2.0;
        }

        public InfectData MakeData(int[] extraData = null, SubParams subParams = null)
        {
            var minDt = posiCumulatives.Keys.Min();
            var _dates = ((posiCumulatives.Keys.Max() - minDt).Days + 1)._range().Select(i => minDt.AddDays(i)).ToArray();
            var _total = new double[_dates.Length];
            foreach (int idx in _dates.Length._range()) {
                var val = posiCumulatives._safeGet(_dates[idx], -1);
                _total[idx] = val >= 0 ? val : idx > 0 ? _total[idx - 1] : 0;
            }
            var _testCumu = new double[_dates.Length];
            int _testCumuEnd = 0;
            foreach (int idx in _dates.Length._range()) {
                var val = testCumulatives._safeGet(_dates[idx], -1);
                if (val >= 0) {
                    _testCumuEnd = idx + 1;
                    _testCumu[idx] = val;
                } else {
                    _testCumu[idx] = idx > 0 ? _testCumu[idx - 1] : 0;
                }
            }
            if (_testCumuEnd < _testCumu.Length) {
                _testCumu = _testCumu[0.._testCumuEnd];
            }

            if (extraData._notEmpty()) {
                int len = extraData.Length;
                int n = extraData[0];
                if (len == 1 && n <=0) {
                    n = Math.Min(-n, _dates._length() - 1);
                    if (n > 0) {
                        _dates = _dates[0..^n];
                        _total = _total[0..^n];
                    }
                } else {
                    int orig_len = _dates.Length;
                    _dates = _dates._extend(orig_len + len);
                    _total = _total._extend(orig_len + len);
                    for (int i = 0; i < len; ++i) {
                        int idx = orig_len + i;
                        _dates[idx] = _dates[idx - 1].AddDays(1);
                        _total[idx] = _total[idx - 1] + extraData[i]._lowLimit(0);
                    }
                }
            }

            int predatanum = (_firstDate - minDt).Days;
            double total(int i) => _total._nth(i + predatanum);
            double testCumu(int i) => _testCumu._nth(i + predatanum);
            double newly(int i) => total(i) - total(i - 1);
            DateTime[] dates = _dates[predatanum..];
            double[] newlies = (_total.Length - predatanum)._range().Select(i => newly(i)).ToArray();
            double[] posiRates = new double[_testCumu.Length - predatanum];
            int prevIdx = -7;
            foreach (int i in posiRates.Length._range()) {
                if (testCumu(i) > testCumu(i - 1)) {
                    posiRates[i] = ((total(i) - total(prevIdx)) / (testCumu(i) - testCumu(prevIdx)))._gtZeroOr(0.000001);
                    if (posiRates[i] > 1.0) {
                        logger.Warn($"{Title}: {_dates._nth(i)._toDateString()}: posiRates[{i}]={posiRates[i]:f3}; {i - prevIdx} days total={total(i) - total(prevIdx)}, {i - prevIdx} days tests={testCumu(i) - testCumu(prevIdx)}");
                    }
                } else {
                    posiRates[i] = i > 0 ? posiRates[i-1] : 0;
                }
                if (testCumu(i - 5) > testCumu(i - 6)) prevIdx = i - 6;
            }
            double[] adjustedTotal = adjustTotal(_total);
            double adjTotal(int i) => adjustedTotal._nth(i + predatanum);
            //double weekly(int i) => total(i) - total(i - 7);
            double weekly(int i) => adjTotal(i) - adjTotal(i - 7);
            double average(int i) => weekly(i) / 7;
            double[] averages = (_total.Length - predatanum)._range().Select(i => average(i)).ToArray();
            double rt(int i) { double w7 = weekly(i - 7); return w7 > 0 ? Math.Pow(weekly(i) / w7, 5.0 / 7.0) : 0.0; };
            var rts = (_total.Length - predatanum)._range().Select(i => rt(i)).ToArray();
            var y1_max = Y1_Max > 0 ? Y1_Max : calcY1Max(newlies);
            var y1_step = y1_max / 10;
            var y2_max = Y2_Max > 0 ? Y2_Max : calcY2Max(rts);
            var y2_step = y2_max == 2.5 ? y2_max / 5 : y2_max / 10;
            //以下を有効にしてしまうと、システム既定基準日とシステム既定検出遡及日による基準日との区別ができなくなってしまう。
            //if (DecayParam.StartDate._notValid()) DecayParam.StartDate = dates[0].AddDays(InfectData.FindRecentMaxIndex(rts));
            var infectData = new InfectData {
                Title = Title,
                Events = Events,
                Y1_Max = y1_max,
                Y1_Step = y1_step,
                Y2_Max = y2_max,
                Y2_Step = y2_step,
                Dates = dates,
                Newly = newlies,
                Average = averages,
                PosiRates = posiRates,
                Rt = rts,
                InitialDecayParam = DecayParam,
                PrefData = this,
            };
            if (subParams == null) subParams = infectData.CalcDecaySubParams();
            infectData.InitialSubParams = subParams;
            return infectData;
        }
    } // class PrefInfectData

}
