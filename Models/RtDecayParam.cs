using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChartBlazorApp.Models
{
    /// <summary>
    /// 予測設定値クラス。このクラスには値型だけを格納すること
    /// </summary>
    public class RtDecayParam
    {
        //public bool UseOnForecast { get; set; }

        public bool Fourstep { get; set; }

        /// <summary>予測開始日</summary>
        public DateTime StartDate { get; set; }

        public DateTime StartDateFourstep { get; set; }

        public DateTime EffectiveStartDate { get { return Fourstep ? StartDateFourstep : StartDate; } }

        public int DaysToOne { get; set; }

        public double DecayFactor { get; set; }

        public int DaysToNext { get; set; }

        public double DecayFactorNext { get; set; }

        public double RtMax { get; set; }

        public double RtMin { get; set; }

        public double EasyRt1 { get; set; }

        public double EasyRt2 { get; set; }

        public int DaysToRt1 { get; set; }
        public double Rt1 { get; set; }

        public int DaysToRt2 { get; set; }
        public double Rt2 { get; set; }

        public int DaysToRt3 { get; set; }
        public double Rt3 { get; set; }

        public int DaysToRt4 { get; set; }
        public double Rt4 { get; set; }

        public RtDecayParam Clone()
        {
            return (RtDecayParam)MemberwiseClone();
        }

        /// <summary>
        /// 最も基底となる予測設定値を作成する(Layer-1)
        /// </summary>
        /// <returns></returns>
        private static RtDecayParam CreateDefaultParam()
        {
            return new RtDecayParam() {
                Fourstep = false,
                StartDate = new DateTime(2020, 12, 1),
                StartDateFourstep = new DateTime(2020, 7, 4),
                DaysToOne = Constants.DAYS_TO_NEXT_RT,
                DecayFactor = 1000,
                DaysToNext = Constants.DAYS_TO_NEXT_RT,
                DecayFactorNext = 50,
                RtMax = 1.2,
                RtMin = 0.8,
                EasyRt1 = 1.0,
                EasyRt2 = 0.8,
                DaysToRt1 = 45,
                Rt1 = 0.83,
                DaysToRt2 = 60,
                Rt2 = 1,
                DaysToRt3 = 26,
                Rt3 = 1.4,
                DaysToRt4 = 30,
                Rt4 = 0.83,
            };
        }

        /// <summary> 最も基底となる予測設定値(Layer-1) </summary>
        public static RtDecayParam DefaultParam { get; } = CreateDefaultParam();

        /// <summary> y = (a / x) + b の形の関数として Rt の減衰を計算する </summary>
        /// <returns>計算された予測Rtの日数を返す</returns>
        public int CalcAndCopyPredictRt(double[] rts, int startIdx, double[] predRt, int realDays, int extensionDays)
        {
            if (extensionDays == 0) extensionDays = Constants.EXTENSION_DAYS;
            double rt0 = rts[startIdx];
            if (Fourstep) {
                // 4段階モード
                double decayFactor = 10000;
                double rt1 = Rt1;
                int daysToRt1 = DaysToRt1;
                if (daysToRt1 < 1) daysToRt1 = Constants.DAYS_TO_NEXT_RT;
                double a1 = (rt0 - rt1) * (decayFactor + daysToRt1) * decayFactor / daysToRt1;
                double b1 = rt0 - (a1 / decayFactor);
                // Rt1に到達してから
                double rt2 = Rt2;
                int daysToRt2 = DaysToRt2;
                if (daysToRt2 < 1) daysToRt2 = Constants.DAYS_TO_NEXT_RT;
                double a2 = (rt1 - rt2) * (decayFactor + daysToRt2) * decayFactor / daysToRt2;
                double b2 = rt1 - (a2 / decayFactor);
                // Rt2に到達してから
                double rt3 = Rt3;
                int daysToRt3 = DaysToRt3;
                if (daysToRt3 < 1) daysToRt3 = Constants.DAYS_TO_NEXT_RT;
                double a3 = (rt2 - rt3) * (decayFactor + daysToRt3) * decayFactor / daysToRt3;
                double b3 = rt2 - (a3 / decayFactor);
                // Rt3に到達してから
                double rt4 = Rt4;
                int daysToRt4 = DaysToRt4;
                if (daysToRt4 < 1) daysToRt4 = Constants.DAYS_TO_NEXT_RT;
                double a4 = (rt3 - rt4) * (decayFactor + daysToRt4) * decayFactor / daysToRt4;
                double b4 = rt3 - (a4 / decayFactor);

                int copyLen = Math.Min(Math.Max(daysToRt1 + daysToRt2 + daysToRt3 + daysToRt4 + Constants.EXTENSION_DAYS_EX, realDays - startIdx + extensionDays), predRt.Length - startIdx);
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
                // 2段階モード
                // ここの extensionDays には、移動平均のための余分な4日分がすでに追加されているので、これを max としてよい
                int copyLen = Math.Min(realDays - startIdx + extensionDays, predRt.Length - startIdx);
                int toOneLen = Math.Min(DaysToOne, copyLen);

                // 1st Stage (rt = a1 * / (factor1 + x) + b1)
                double rt1 = EasyRt1;
                double factor1 = DecayFactor;
                if (factor1 < 1) factor1 = 50;
                double a1 = Constants.CalcCoefficientA1(rt0, rt1, factor1, DaysToOne);
                double b1 = Constants.CalcCoefficientB1(rt0, a1, factor1);

                for (int i = 0; i < toOneLen; ++i) {
                    predRt[startIdx + i] = Constants.CalcRt1(a1, b1, factor1, i);
                }

                // 2nd Stage
                double tgtRt2 = EasyRt2;
                double factor2 = DecayFactorNext;
                (double a2, double b2) = Constants.CalcCoefficients2(rt0, rt1, tgtRt2, factor2, DaysToOne);

                for (int i = toOneLen; i < copyLen; ++i) {
                    predRt[startIdx + i] = Constants.CalcRt2(a2, b2, rt1, tgtRt2, factor2, i, i - toOneLen);
                }
                return copyLen;
            }
        }
    }

    public struct SubParams
    {
        public string StartDate { get; set; }
        public double Rt1 { get; set; }
        public double Rt2 { get; set; }
        public int DaysToOne { get; set; }
        public double DecayFactor { get; set; }
        public double DecayFactorNext { get; set; }
    }

}
