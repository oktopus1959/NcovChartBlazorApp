using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChartBlazorApp.Models
{
    public static class Constants
    {
        /***  DailyData.cs ***/
        public const string PREF_FILE_PATH = "Data/csv/prefectures_ex.csv";

        /// <summary> 基準日検出の対象となるRtの最大値(これを超えるRtの日は対象外)</summary>
        public const double RT_THRESHOLD = 3.0;

        public const double RT_THRESHOLD2 = 5.0;

        /// <summary> 基準日検出の対象外となる実数日末尾期間</summary>
        public const int TAIL_GUARD_DURATION = 5;

        /// <summary> 基準日検出のための遡及日数のデフォルト </summary>
        public const int LOCAL_MAX_RT_BACK_DURATION = 12;

        /// <summary> 基準日検出の最大遡及日数</summary>
        public const int MAX_BACK_DAYS = 90;

        /// <summary> 基準日検出の最大遡及日数(OldestExremum検出用)</summary>
        public const int MAX_EXTREMUM_BACK_DAYS = 28;

        /// <summary> 極値検出のための期間(この期間内で最大/最小のものを極値として扱う) </summary>
        public const int EXTREMUM_DETECTION_DURATION = 10;

        /// <summary> システムによる1段階設定において変化日までの最小日数(基準日を0日と数える)</summary>
        public const int STAGE1_MIN_DURATION = 10;

        /// <summary> 2段階設定が可能な最小日数(基準日を0日として、実数日末尾までの日数; 12/01～12/10 なら9日間と数える)</summary>
        //public const int STAGE2_MIN_DURATION = 12;
        public const int STAGE2_MIN_DURATION = 8;

        /// <summary> 中間評価において、移動平均との誤差(ERR)を計算する末尾日数</summary>
        public const int AVERAGE_ERR_DURATION = 7;

        /// <summary> 最終評価において、移動平均との誤差(ERR)を計算する末尾日数</summary>
        public const int TAIL_ERR_DURATION = 3;

        /// <summary> ステージ1において、x日の Rt を計算する (rt = a1 / (factor1 + x) + b1)</summary>
        public static double CalcRt1(double a1, double b1, double factor1, int x)
        {
            return a1 / (factor1 + x) + b1;
        }

        /// <summary> ステージ1において、係数 a1 を計算する (rt = CalcRt1(a1, b1, factor1, x) から逆算)</summary>
        public static double CalcCoefficientA1(double rt0, double rt1, double factor1, int dayIdx1) {
            return (rt0 - rt1) * (factor1 + dayIdx1) * factor1 / dayIdx1; ;
        }

        /// <summary> ステージ1において、係数 b1 を計算する (rt = CalcRt1(a1, b1, factor1, x) から逆算)</summary>
        public static double CalcCoefficientB1(double rt0, double a1, double factor1) {
            return rt0 - (a1 / factor1);
        }

        /// <summary>
        /// ステージ2において、係数 a2, b2 を計算する。
        /// factor2 < 0 なら、線形に増減する。
        /// factor2 > 0 なら、stage1 の増減カーブを延長したような式にする。
        /// 具体的には、rt0 &lt; rt1 &gt; rt2 または rt0 &gt; rt1 &lt; rt2 のように rt0 ～ rt1 ～ rt2 が山型または谷型をしている場合は、rt0 を反転させる。
        /// </summary>
        public static (double, double) CalcCoefficients2(double rt0, double rt1, double rt2, double factor2, int dayIdx2)
        {
            double a2, b2;
            if (factor2 > 0) {
                double rt_ = ((rt0 > rt1 && rt2 > rt1) || (rt0 < rt1 && rt2 < rt1)) ? rt1 * 2 - rt0 : rt0;
                a2 = (rt_ - rt1) * (factor2 + dayIdx2) * factor2 / dayIdx2;
                b2 = rt_ - (a2 / factor2);
            } else {
                double f2 = Math.Max(factor2, -4);
                a2 = (rt2 - rt1) / (dayIdx2 * (1.0 + 0.2 * f2));
                b2 = rt1;
            }
            return (a2, b2);
        }

        public static double CalcRt2(double a2, double b2, double rt1, double tgtRt2, double factor2, int x1, int x2)
        {
            if (tgtRt2 == rt1) {
                return tgtRt2;
            } else {
                double rt = (factor2 > 0) ? a2 / (factor2 + x1) + b2 : a2 * x2 + b2;
                return (tgtRt2 < rt1 && rt < tgtRt2) || (tgtRt2 > rt1 && rt > tgtRt2) ? tgtRt2 : rt;
            }
        }

        /*** ForecastData.cs ***/
        public const string INFECTION_RATE_FILE_PATH = "Data/csv/infect_by_ages.csv";       // 陽性率CSVのパス
        public const string DEATH_RATE_FILE_PATH = "Data/csv/death_rate.csv";               // 死亡率CSVのパス
        public const string SERIOUS_RATE_FILE_PATH = "Data/csv/serious_rate.csv";           // 重症化率CSVのパス
        public const string RECOVER_RATE_FILE_PATH = "Data/csv/recover_rate.csv";           // 改善率CSVのパス
        public const string DEATH_AND_SERIOUS_FILE_PATH = "Data/csv/death_and_serious.csv"; // 死亡者・重症者数CSVのパス

        /// <summary> 予測日数 </summary>
        public const int FORECAST_PREDICTION_DAYS = 28;

        /// <summary> 4段階設定用の予測日数 </summary>
        public const int FORECAST_PREDICTION_DAYS_FOR_DETAIL = 90;

        /// <summary> 7日移動平均の中央までシフトする日数 </summary>
        public const int FORECAST_AVERAGE_SHIFT_DAYS = 3;

        /*** RtDecayParam.cs ***/
        /// <summary> 次のRtに到達するまでの日数 </summary>
        public const int DAYS_TO_NEXT_RT = 15;

        /// <summary> 予測表示期間のデフォルト </summary>
        public const int EXTENSION_DAYS = 25;

        /// <summary> 予測表示期間のデフォルト(4段階設定用) </summary>
        public const int EXTENSION_DAYS_EX = 15;

        /*** RtDecayParam.cs ***/
        /// <summary> 7日移動平均を計算するのに追加で必要となる日数 </summary>
        public const int EXTRA_DAYS_FOR_AVERAGE = 4;

        /*** UserSettings.cs ***/
        public const string SETTINGS_KEY = "net.oktopus59.ncov.settings.v4"; // v1008

        /// <summary> 主要3自治体(全国を含む)の数 (全国、東京、大阪) </summary>
        public const int MAIN_PREF_NUM = 3;

        /// <summary> 自治体選択用ラジオボタンに追加できる自治体の最大数 </summary>
        public const int FAVORITE_PREF_MAX = 10;

        /// <summary> 自治体選択用ラジオボタンの最大数 </summary>
        public const int RADIO_IDX_MAX = MAIN_PREF_NUM + FAVORITE_PREF_MAX;

        /*** EffectiveParams.cs ***/
#if DEBUG
        public const int DEBUG_LEVEL = 1; 
#else
        public const int DEBUG_LEVEL = 0; 
#endif
    }
}
