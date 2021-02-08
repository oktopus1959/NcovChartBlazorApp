﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StandardCommon;

namespace ChartBlazorApp.Models
{
    /// <summary>
    /// 利用者用の予測データ
    /// </summary>
    public class UserPredictData
    {
        private static ConsoleLog logger = ConsoleLog.GetLogger();

        /// <summary> 予想実効再生産数 </summary>
        public double[] FullPredRt { get; private set; }
        /// <summary> 推計移動平均 </summary>
        public double[] PredAverage { get; private set; }
        /// <summary> 予想開始日位置 </summary>
        public int PredStartIdx { get; private set; } = 0;
        ///// <summary> 予想Rt </summary>
        //public double[] PredRt { get; private set; }
        /// <summary> 推計陽性者数 </summary>
        public double[] PredNewly { get; private set; }
        /// <summary> 逆算推計移動平均 </summary>
        public double[] RevPredAverage { get; private set; }
        /// <summary> 逆算Rt </summary>
        public double[] RevRt { get; private set; }
        /// <summary> 表示日数 </summary>
        public int DispDays { get; set; } = 1;
        /// <summary> 予測日数 </summary>
        public int PredDays { get; set; }

        /// <summary>
        /// 各種推計値の計算
        /// </summary>
        /// <param name="rtDecayParam">書き換えられてもよいデータである必要がある</param>
        /// <param name="numFullDays">全表示候補日の数(1年間とか)</param>
        /// <param name="predStartDt">予測開始日</param>
        /// <returns>(PredictInfectData, 表示開始から予測終了までの日数)</returns>
        public static UserPredictData PredictValuesEx(InfectData infData, RtDecayParam rtDecayParam, int numFullDays, int extensionDays, DateTime? predStartDt = null)
        {
            return new UserPredictData().predictValuesEx(infData, rtDecayParam, numFullDays, extensionDays, predStartDt);
        }

        /// <summary>各種推計値の計算</summary>
        /// <param name="infData"></param>
        /// <param name="rtDecayParam">not null で、書き換えられてもよいデータである必要がある</param>
        /// <param name="numFullDays">全表示候補日の数(1年間とか)</param>
        /// <param name="extensionDays"></param>
        /// <param name="predStartDt"></param>
        /// <returns></returns>
        public UserPredictData predictValuesEx(InfectData infData, RtDecayParam rtDecayParam, int numFullDays, int extensionDays, DateTime? predStartDt)
        {
            //logger.Trace("ENTER");
            var firstRealDate = infData.Dates.First();
            DateTime realEndDate = predStartDt?.AddDays(-1) ?? DateTime.MaxValue;
            DateTime infEndDate = infData.Dates.Last();
            if (realEndDate > infEndDate) realEndDate = infEndDate;
            int realDays = (realEndDate - firstRealDate).Days + 1;

            if (rtDecayParam.StartDate > realEndDate) rtDecayParam.StartDate = realEndDate;
            if (rtDecayParam.StartDateFourstep > realEndDate) rtDecayParam.StartDateFourstep = realEndDate;
            //logger.Debug($"rtDecayParam.StartDate={rtDecayParam.StartDate.ToLongDateString()}, " +
            //    $"StartDateFourstep={rtDecayParam.StartDateFourstep.ToLongDateString()}, " +
            //    $"EffectiveStartDate={rtDecayParam.EffectiveStartDate.ToLongDateString()}");

            const int ExtraDaysForAverage = Constants.EXTRA_DAYS_FOR_AVERAGE;
            // 逆算移動平均
            double[] revAverage = new double[numFullDays + ExtraDaysForAverage];
            // 予想実効再生産数
            double[] fullPredRt = new double[numFullDays + ExtraDaysForAverage];

            PredStartIdx = (rtDecayParam.EffectiveStartDate - firstRealDate).Days;
            if (PredStartIdx < 0 || PredStartIdx >= infData.Average.Length) {
                logger.Error($"PredStartIdx({PredStartIdx}) is out of range. "
                    + $"rtDecayParam.EffectiveStartDate({rtDecayParam.EffectiveStartDate}) may not be valid. "
                    + $"Use realEndDate={realEndDate} instead");
                PredStartIdx = (realEndDate - firstRealDate).Days;
            }
            Array.Copy(infData.Average, revAverage, PredStartIdx);
            int predRtLen = rtDecayParam.CalcAndCopyPredictRt(infData.Rt, PredStartIdx, fullPredRt, realDays, extensionDays + ExtraDaysForAverage);

            for (int i = 0; i < predRtLen; ++i) {
                int idx = PredStartIdx + i;
                var rt = fullPredRt[idx];
                if (idx >= 7 && idx < revAverage.Length && rt > 0) {
                    revAverage[idx] = Math.Pow(rt, 7.0 / 5.0) * revAverage[idx - 7];
                }
            }
            FullPredRt = fullPredRt.Take(numFullDays).ToArray();

            predRtLen -= ExtraDaysForAverage;
            double[] revAveAverage = new double[numFullDays + ExtraDaysForAverage];   // 逆算移動平均の平均
            for (int i = 0; i < predRtLen; ++i) {
                int idx = PredStartIdx + i;
                int margin = i._highLimit(ExtraDaysForAverage - 1);
                int beg = idx - margin;
                int end = idx + margin + 1;
                if (beg >= 0 && end <= revAverage.Length) {
                    revAveAverage[idx] = revAverage[beg..end].Sum() / (margin * 2 + 1);
                }
            }

            // 推計移動平均
            PredAverage = revAveAverage._extend(numFullDays).Take(numFullDays).ToArray();

            // 推計陽性者数(3週平均)
            double[] predNewlyMean = calcPredictInfectMean(infData.Newly, infData.Average, PredAverage, realDays, PredStartIdx);
            PredNewly = predNewlyMean;

            // 推計陽性者数(前週差分)
            //PredNewly = predictInfect(infData.Newly, predNewlyMean, PrePredAverage, realDays, PredStartIdx);

            // 累積推計の計算
            double predTotal = 0;
            double[] predTotals = PredNewly.Select((n, i) => predTotal += (n > 0 ? n : infData.Newly._nth(i))).ToArray();

            // 逆算移動平均による推計陽性者数の調整
            for (int predChkIdx = PredStartIdx + 1; predChkIdx < PredAverage.Length; predChkIdx += 7) {
                int chkEnd = (predChkIdx + 7)._highLimit(PredAverage.Length) - 1;
                double adjustFact = PredAverage[chkEnd] / ((predTotals[chkEnd] - predTotals[chkEnd - 7]) / 7.0);   // 推計移動平均と逆算移動平均の相違率
                for (int i = predChkIdx; i <= chkEnd; ++i) {
                    PredNewly[i] = PredNewly[i] * adjustFact;
                }
            }

            // 累積推計の再計算
            predTotal = 0;
            predTotals = PredNewly.Select((n, i) => predTotal += (n > 0 ? n : infData.Newly._nth(i))).ToArray();

            // 逆算推計移動平均
            RevPredAverage = predTotals.Select((_, i) => i >= 7 ? (predTotals[i] - predTotals[i - 7]) / 7.0 : 0.0).ToArray();

            // 逆算Rt
            RevRt = new double[numFullDays];
            for (int i = PredStartIdx; i < PredNewly.Length; ++i) {
                RevRt[i] = DailyData.CalcRt(predTotals, i);
            }

            PredDays = PredStartIdx + predRtLen;
            //logger.Trace("LEAVE");
            return this;
        }

        private static double[] predictInfect(double[] newly, double[] predNewlyMean, double[] predAverage, int realDays, int predStartIdx)
        {
            // 推計陽性者数
            double[] predNewly = new double[predAverage.Length];

            for (int n = predStartIdx + 1; n < predAverage.Length; ++n) {
                var ave = predAverage[n];
                var sum = n._range(n - 6).Select(i => i < realDays ? newly[i] : predNewly[i]).Sum();
                var val = ave * 7 - sum;
                var mean = predNewlyMean[n];
                //predNewly[n] = val < ave ? val._lowLimit(mean) : val._highLimit(mean);
                predNewly[n] = (mean * 3 + val) / 4;
            }
            return predNewly;
        }

        private static double[] calcPredictInfectMean(double[] newly, double[] average, double[] predAverage, int realDays, int predStartIdx)
        {
            double newly_offset = 3.0;
            double pred_offset = 10.0;

            // dayRatio: 当日増減率
            double[] dayRatio = new double[predAverage.Length];
            double calcRatio(double n, double? p) => (p > 0) ? Math.Pow((n + newly_offset) / (p.Value + pred_offset), 1.0) : 1.0;
            for (int i = 0; i < realDays && i < dayRatio.Length; ++i) {
                dayRatio[i] = calcRatio(newly._nth(i), (i < predStartIdx) ? average._nth(i) : predAverage._nth(i));
            }

            // dowRatio: 当日、-7日と-14日の平均増減率
            double[] dowRatio = new double[predAverage.Length];
            double calcRatio3(int idx) => Enumerable.Range(0, 3).Select(w => dayRatio._nth(idx - w*7, 1.0)).Sum() / 3;
            for (int i = 0; i < dowRatio.Length; ++i) {
                dowRatio[i] = (i < realDays) ? Math.Max(Math.Min(calcRatio3(i), 2.0), 0.3) : dowRatio._nth(i - 7, 1.0);
            }

            //return pred.Select((p, i) => i < realDays && i <= approxDayPos ? null : p * dowRatio._nth(i - 7)).ToArray();
            // 一週間前の曜日増減率を参照する
            return predAverage.Select((p, i) => (i <= predStartIdx) ? 0 : p * dowRatio._nth(i - 7)).ToArray();
        }

        /// <summary>
        /// 二乗平均誤差の計算
        /// </summary>
        /// <returns></returns>
        public double CalcAverageMSE(double[] average, int start, int end)
        {
            return end._range(start).Select(i => Math.Pow(PredAverage[i] - average[i], 2)).Sum();
        }
    }
}
