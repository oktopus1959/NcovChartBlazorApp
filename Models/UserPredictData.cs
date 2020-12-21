using System;
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
            var firstRealDate = infData.Dates.First();
            DateTime realEndDate = predStartDt?.AddDays(-1) ?? DateTime.MaxValue;
            if (realEndDate > infData.Dates.Last()) realEndDate = infData.Dates.Last();
            int realDays = (realEndDate - firstRealDate).Days + 1;

            if (rtDecayParam.StartDate > realEndDate) rtDecayParam.StartDate = realEndDate;
            if (rtDecayParam.StartDateFourstep > realEndDate) rtDecayParam.StartDateFourstep = realEndDate;
#if DEBUG
            //Console.WriteLine($"rtDecayParam.StartDate={rtDecayParam.StartDate.ToLongDateString()}, " +
            //    $"StartDateFourstep={rtDecayParam.StartDateFourstep.ToLongDateString()}, " +
            //    $"EffectiveStartDate={rtDecayParam.EffectiveStartDate.ToLongDateString()}");
#endif
            RevAverage = new double[numFullDays];
            FullPredRt = new double[numFullDays];

            const int ExtraDaysForAverage = 4;
            PredStartIdx = (rtDecayParam.EffectiveStartDate - firstRealDate).Days;
            Array.Copy(infData.Average, RevAverage, PredStartIdx);
            int predRtLen = rtDecayParam.CalcAndCopyPredictRt(infData.Rt, PredStartIdx, FullPredRt, realDays, extensionDays + ExtraDaysForAverage);

            for (int i = 0; i < predRtLen; ++i) {
                int idx = PredStartIdx + i;
                var rt = FullPredRt[idx];
                if (idx >= 7 && idx < RevAverage.Length && rt > 0) {
                    RevAverage[idx] = Math.Pow(rt, 7.0 / 5.0) * RevAverage[idx - 7];
                }
            }

            predRtLen -= ExtraDaysForAverage;
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
            PredNewly = predictInfect(infData.Newly, infData.Average, PredAverage, realDays, PredStartIdx);
            double total = 0;
            double[] totals = PredNewly.Select((n, i) => total += (n > 0 ? n : infData.Newly._nth(i))).ToArray();
            RevRt = new double[numFullDays];
            for (int i = PredStartIdx; i < PredNewly.Length; ++i) {
                RevRt[i] = DailyData.CalcRt(totals, i);
            }

            PredDays = PredStartIdx + predRtLen;
            return this;
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
}
