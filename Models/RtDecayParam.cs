using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StandardCommon;

namespace ChartBlazorApp.Models
{
    /// <summary>
    /// 予測設定値クラス。このクラスには値型だけを格納すること
    /// </summary>
    public class RtDecayParam
    {
        //public bool UseOnForecast { get; set; }

        public bool Fourstep { get; set; }

        /// <summary>予想基点日(2段階階)</summary>
        public DateTime StartDate { get; set; }

        /// <summary>予想基点日(多段階)</summary>
        public DateTime StartDateFourstep { get; set; }

        /// <summary>予想基点日(2段階または多段階)</summary>
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

        public int DaysToRt5 { get; set; }
        public double Rt5 { get; set; }

        public int DaysToRt6 { get; set; }
        public double Rt6 { get; set; }

        //public bool UsePostDecayRt1 { get; set; } = true;
        public double PostDecayFactorRt2 { get; set; } = 0;

        public RtDecayParam Clone()
        {
            return (RtDecayParam)MemberwiseClone();
        }

        public override string ToString()
        {
            if (Fourstep) {
                return $"4step={Fourstep}, StartDt={StartDate._toDateString()}, Days1={DaysToRt1}, Rt1={Rt1:f3}, Days2={DaysToRt2}, Rt2={Rt2:f3}, Days3={DaysToRt3}, Rt3={Rt3:f3}, " +
                    $"Days4={DaysToRt4}, Rt4={Rt4:f3}, Days5={DaysToRt5}, Rt5={Rt5:f3}, Days6={DaysToRt6}, Rt6={Rt6:f3}";
            } else {
                return $"StartDt={StartDate._toDateString()}, Days1={DaysToOne}, Rt1={EasyRt1:f3}, Factor1={DecayFactor}, Rt2={EasyRt2:f3}, Factor2={DecayFactorNext}";
            }
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
                StartDateFourstep = DateTime.Now.AddDays(-1)._toDate(), //new DateTime(2020, 7, 4),
                DaysToOne = Constants.DAYS_TO_NEXT_RT,
                DecayFactor = 1000,
                DaysToNext = Constants.DAYS_TO_NEXT_RT,
                DecayFactorNext = 50,
                RtMax = 1.2,
                RtMin = 0.8,
                EasyRt1 = 1.0,
                EasyRt2 = 0.8,
                DaysToRt1 = 10,  //45,
                Rt1 = 1,        //0.83,
                DaysToRt2 = 0,  //60,
                Rt2 = 1,
                DaysToRt3 = 0,  //26,
                Rt3 = 1,        //1.4,
                DaysToRt4 = 0,  //30,
                Rt4 = 1,        //0.83,
                DaysToRt5 = 0,
                Rt5 = 1,
                DaysToRt6 = 0,
                Rt6 = 1,
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
                // 多段階モード
                double decayFactor = 10000;
                double rt1 = Rt1;
                int daysToRt1 = DaysToRt1;
                //if (daysToRt1 < 1) daysToRt1 = Constants.DAYS_TO_NEXT_RT;
                double a1 = 0, b1 = 0;
                if (daysToRt1 > 0) {
                    a1 = (rt0 - rt1) * (decayFactor + daysToRt1) * decayFactor / daysToRt1;
                    b1 = rt0 - (a1 / decayFactor);
                } else {
                    daysToRt1 = 0;
                    rt1 = rt0;
                }
                // Rt1に到達してから
                double rt2 = Rt2;
                int daysToRt2 = DaysToRt2;
                //if (daysToRt2 == 0) daysToRt2 = Constants.DAYS_TO_NEXT_RT;
                double a2 = 0, b2 = 0;
                if (daysToRt2 > 0) {
                    a2 = (rt1 - rt2) * (decayFactor + daysToRt2) * decayFactor / daysToRt2;
                    b2 = rt1 - (a2 / decayFactor);
                } else {
                    daysToRt2 = 0;
                    rt2 = rt1;
                }
                // Rt2に到達してから
                double rt3 = Rt3;
                int daysToRt3 = DaysToRt3;
                //if (daysToRt3 == 0) daysToRt3 = Constants.DAYS_TO_NEXT_RT;
                double a3 = 0, b3 = 0;
                if (daysToRt3 > 0) {
                    a3 = (rt2 - rt3) * (decayFactor + daysToRt3) * decayFactor / daysToRt3;
                    b3 = rt2 - (a3 / decayFactor);
                } else {
                    daysToRt3 = 0;
                    rt3 = rt2;
                }
                // Rt3に到達してから
                double rt4 = Rt4;
                int daysToRt4 = DaysToRt4;
                //if (daysToRt4 == 0) daysToRt4 = Constants.DAYS_TO_NEXT_RT;
                double a4 = 0, b4 = 0;
                if (daysToRt4 > 0) {
                    a4 = (rt3 - rt4) * (decayFactor + daysToRt4) * decayFactor / daysToRt4;
                    b4 = rt3 - (a4 / decayFactor);
                } else {
                    daysToRt4 = 0;
                    rt4 = rt3;
                }
                // Rt4に到達してから
                double rt5 = Rt5;
                int daysToRt5 = DaysToRt5;
                //if (daysToRt5 == 0) daysToRt5 = Constants.DAYS_TO_NEXT_RT;
                double a5 = 0, b5 = 0;
                if (daysToRt5 > 0) {
                    a5 = (rt4 - rt5) * (decayFactor + daysToRt5) * decayFactor / daysToRt5;
                    b5 = rt4 - (a5 / decayFactor);
                } else {
                    daysToRt5 = 0;
                    rt5 = rt4;
                }
                // Rt5に到達してから
                double rt6 = Rt6;
                int daysToRt6 = DaysToRt6;
                //if (daysToRt6 == 0) daysToRt6 = Constants.DAYS_TO_NEXT_RT;
                double a6 = 0, b6 = 0;
                if (daysToRt6 > 0) {
                    a6 = (rt5 - rt6) * (decayFactor + daysToRt6) * decayFactor / daysToRt6;
                    b6 = rt5 - (a6 / decayFactor);
                } else {
                    daysToRt6 = 0;
                    rt6 = rt5;
                }

                int copyLen = ((daysToRt1 + daysToRt2 + daysToRt3 + daysToRt4 + daysToRt5 + daysToRt6)._lowLimit(realDays - startIdx) + extensionDays)._highLimit(predRt.Length - startIdx);
                double rt = 0;
                for (int i = 0; i < copyLen; ++i) {
                    if (i == 0) {
                        rt = rt0;
                    } else if (daysToRt1 + daysToRt2 + daysToRt3 + daysToRt4 + daysToRt5 + daysToRt6 == 0) {
                        rt = 0;
                    } else if (i <= daysToRt1) {
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
                    } else if (i <= daysToRt1 + daysToRt2 + daysToRt3 + daysToRt4){
                        rt = a4 / (decayFactor + i - daysToRt1 - daysToRt2 - daysToRt3) + b4;
                        if (rt3 > rt4) {
                            if (rt < rt4) rt = rt4;
                        } else {
                            if (rt > rt4) rt = rt4;
                        }
                    } else if (i <= daysToRt1 + daysToRt2 + daysToRt3 + daysToRt4 + daysToRt5){
                        rt = a5 / (decayFactor + i - daysToRt1 - daysToRt2 - daysToRt3 - daysToRt4) + b5;
                        if (rt4 > rt5) {
                            if (rt < rt5) rt = rt5;
                        } else {
                            if (rt > rt5) rt = rt5;
                        }
                    } else if (i <= daysToRt1 + daysToRt2 + daysToRt3 + daysToRt4 + daysToRt5 + daysToRt6){
                        rt = a6 / (decayFactor + i - daysToRt1 - daysToRt2 - daysToRt3 - daysToRt4 - daysToRt5) + b6;
                        if (rt5 > rt6) {
                            if (rt < rt6) rt = rt6;
                        } else {
                            if (rt > rt6) rt = rt6;
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
                //if (factor1 < 1) factor1 = 50;
                //double a1 = Constants.CalcCoefficientA1(rt0, rt1, factor1, DaysToOne);
                //double b1 = Constants.CalcCoefficientB1(rt0, a1, factor1);
                (double a1, double b1) = Constants.CalcCoefficient1(rt0, rt1, factor1, DaysToOne);

                for (int i = 0; i < toOneLen; ++i) {
                    predRt[startIdx + i] = Constants.CalcRt1(a1, b1, factor1, i);
                }

                // 2nd Stage
                double tgtRt2 = EasyRt2;
                double factor2 = DecayFactorNext;
                (double a2, double b2) = Constants.CalcCoefficients2(rt0, rt1, tgtRt2, factor2, DaysToOne);

                int ph3StartIdx = -1;
                double rt2 = Math.Min(rts._nth(startIdx + DaysToOne), rts[^1]) * 0.9; // 減衰の下限
                double rt3 = 0;
                double lastRt = 1;
                for (int i = toOneLen; i < copyLen; ++i) {
                    int idx = startIdx + i;
                    if (ph3StartIdx < 0) {
                        var rt = Constants.CalcRt2(a2, b2, rt1, tgtRt2, factor2, i, i - toOneLen);
                        predRt[idx] = rt;
                        if (tgtRt2 >= 1 && rt > 1) {
                            if (PostDecayFactorRt2 > 0) {
                                if ((idx + 1 >= realDays && rt == tgtRt2) || (idx >= realDays + 9)) {
                                    ph3StartIdx = i;
                                    rt3 = rt._highLimit(2.0);
                                    PostDecayFactorRt2 = PostDecayFactorRt2._lowLimit((rt3 - 1.0) / 20.0);
                                }
                            }
                        } else if (rt2 < 1 && tgtRt2 < rt2 && rt < rt2) {
                            // Rtが1以下で減少している時は、下限を最後のRtの8掛けとする
                            predRt[idx] = lastRt = rt2;
                            ph3StartIdx = copyLen;
                        }
                    } else if (ph3StartIdx < copyLen) {
                        //var rt = rt3 - ((rt3 - 1) / 30) * (i - ph3StartIdx);
                        var rt = rt3 - (i - ph3StartIdx) * PostDecayFactorRt2;
                        if (rt < 1) {
                            lastRt = rt = 1;
                            ph3StartIdx = copyLen;
                        }
                        predRt[idx] = rt;
                    } else {
                        predRt[idx] = lastRt;
                    }
                }
                return copyLen;
            }
        }
    }

    public class SubParams
    {
        public string StartDate { get; set; }
        public double Rt1 { get; set; }
        public double Rt2 { get; set; }
        public int DaysToOne { get; set; }
        public double DecayFactor { get; set; }
        public double DecayFactorNext { get; set; }
        public DateTime UpdateDt { get; set; } = DateTime.Now;
        public SubParams Clone()
        {
            return (SubParams)MemberwiseClone();
        }

    }

}
