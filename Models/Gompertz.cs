using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChartBlazorApp.Models
{
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
            Console.WriteLine($"{title}: StartDate={StartDate.ToLongDateString()}, PeakDays={PeakDays}, H={H:f1}, w={w:f5}");
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

    }
}
