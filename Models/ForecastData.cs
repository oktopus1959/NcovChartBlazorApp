using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StandardCommon;

namespace ChartBlazorApp.Models
{
    /// <summary>
    /// 開始日付きデータ系列
    /// </summary>
    public class DatedDataSeries
    {
        /// <summary>開始日</summary>
        public DateTime Date { get; set; }

        /// <summary>データ系列</summary>
        public double[] DataSeries { get; set; }

        /// <summary>遡及日数</summary>
        public int OffsetDays { get; set; }

        public static DatedDataSeries Create(DatedDataSeries baseSeries, string date, double factor, int offset = 0)
        {
            return new DatedDataSeries() {
                Date = date._parseDateTime(),
                DataSeries = baseSeries.DataSeries.Select(r => r * factor).ToArray(),
                OffsetDays = offset,
            };
        }

        /// <summary>指定日のデータを取得</summary>
        public double DataOn(DateTime dt)
        {
            return DataSeries._nth((dt - Date).Days);
        }

        /// <summary>指定日以降の部分データ系列を取得</summary>
        public double[] GetSubSeriesFrom(DateTime fromDt)
        {
            int days = Math.Max((fromDt - Date).Days, 0);
            return days < DataSeries.Length ? DataSeries[days..] : new double[0];
        }

        /// <summary>指定日の前日までの部分データ系列を取得</summary>
        public double[] GetSubSeriesTo(DateTime toDt)
        {
            int days = Math.Min(Math.Max((toDt - Date).Days, 0), DataSeries.Length);
            return DataSeries[0..days];
        }
    }

    /// <summary>
    /// 開始日付きデータ系列のリスト
    /// </summary>
    public class DatedDataSeriesList
    {
        public DateTime UpdateDate { get; set; }

        public List<DatedDataSeries> DataSeriesList { get; set; }

        public DatedDataSeries FindDataSeries(DateTime date)
        {
            for (int i = 1; i < DataSeriesList.Count; ++i) {
                if (date < DataSeriesList[i].Date) return DataSeriesList[i - 1];
            }
            return DataSeriesList.Last();
        }

    }

    /// <summary>
    /// 予測データ
    /// </summary>
    public class ForecastData
    {
        /// <summary> 7日移動平均の中央までシフトする日数 </summary>
        public const int AverageShiftDays = 3;

        /// <summary> 予測日数 </summary>
        public const int PredictDays = 28;

        /// <summary> グラフ表示開始日 </summary>
        public DateTime ChartStartDate { get; set; }

        /// <summary> 実データ部分も含めた予測表示開始日 </summary>
        public DateTime PredDispStartDate { get; set; }

        /// <summary> 予測に利用する実データの終了日(の翌日) </summary>
        public DateTime PredictStartDate { get; set; }

        public string PredictStartDateStr { get { return PredictStartDate.ToString("M月d日"); } }

        public DateTime LastPredictDate { get { return PredictStartDate.AddDays(PredictDays - 1); } }

        public string LastPredictDateStr { get { return LastPredictDate.ToString("M月d日"); } }

        public int LastPredictDeath { get; set; }

        public int LastPredictSerious { get; set; }

        public int MaxPredictSerious { get; set; }

        public DateTime MaxPredictSeriousDate { get; set; }

        public string MaxPredictSeriousDateStr { get { return MaxPredictSeriousDate.ToString("M月d日"); } }

        public int LastAccumPositive { get; set; }

        public double StartSeriousTotal { get; set; }

        public double StartRecoverTotal { get; set; }

        public int FullDays(DateTime firstDate) { return Math.Max((PredictStartDate - firstDate).Days, 0) + PredictDays + AverageShiftDays + 7; }

        public double MaxDeath { get; set; }

        public double MinDeath { get; set; }

        public double MaxSerious { get; set; }

        public double MinSerious { get; set; }

        public DailyData dailyData { get; set; }

        public DatedDataSeriesList InfectRatesByAges { get; private set; } = null;

        private DatedDataSeries _realDeathSeries = null;

        private DatedDataSeries _realSeriousSeries = null;

        public DatedDataSeriesList DeathRatesByAges { get; private set; }

        public DatedDataSeriesList SeriousRatesByAges { get; private set; }

        /// <summary>
        /// 想定死亡率、重症化率CSVのロード
        /// </summary>
        private DatedDataSeriesList loadDeathRate(string csvfile)
        {
            var list = new DatedDataSeriesList() { DataSeriesList = new List<DatedDataSeries>() };
            foreach (var items in readFile(csvfile).Select(line => line.Trim().Split(','))) {
                if (items[0]._startsWith("#")) {
                    list.UpdateDate = items[0].Trim('#', ' ')._parseDateTime();
                } else {
                    list.DataSeriesList.Add(new DatedDataSeries() {
                        Date = items[0]._parseDateTime(),
                        DataSeries = items[1..10].Select(item => item._parseDouble()).ToArray(),
                        OffsetDays = items[10]._parseInt(0),
                    });
                }
            }
            return list;
        }

        public DatedDataSeriesList RecoverRates { get; private set; }

        /// <summary>
        /// 改善率CSVのロード
        /// </summary>
        private void loadRecoverRate()
        {
            RecoverRates = new DatedDataSeriesList() { DataSeriesList = new List<DatedDataSeries>() };
            foreach (var items in readFile("Data/csv/recover_rate.csv").Select(line => line.Trim().Split(','))) {
                if (items[0]._startsWith("#")) {
                    RecoverRates.UpdateDate = items[0].Trim('#', ' ')._parseDateTime();
                } else {
                    RecoverRates.DataSeriesList.Add(new DatedDataSeries() {
                        Date = items[0]._parseDateTime(),
                        DataSeries = items[1]._parseDouble()._toArray1(),
                        OffsetDays = items[2]._parseInt(0),
                    });
                }
            }
        }

        public ForecastData()
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

                InfectRatesByAges = loadInfectByAgesData();
                loadDeathAndSeriousByAges();
                DeathRatesByAges = loadDeathRate("Data/csv/death_rate.csv");
                SeriousRatesByAges = loadDeathRate("Data/csv/serious_rate.csv");
                loadRecoverRate();
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
        /// 年代別陽性者数CSVのロード⇒年代別陽性者割合に変換
        /// </summary>
        /// <returns></returns>
        private DatedDataSeriesList loadInfectByAgesData()
        {
            List<DatedDataSeries> list = new List<DatedDataSeries>();
            double[] prevCounts = new double[9];
            double prevTotal = 0;
            foreach (var line in readFile("Data/csv/infect_by_ages.csv")) {
                var items = line.Trim().Split(',');
                if (items._safeCount() < 10) continue;

                var counts = items[1..10].Select(n => n._parseDouble(0)).ToArray();
                var total = counts.Sum();
                list.Add(new DatedDataSeries() {
                    Date = items[0]._parseDateTime().AddDays(-6),
                    DataSeries = counts.Select((n, i) => (n - prevCounts[i]) / (total - prevTotal)).ToArray()
                });
                prevCounts = counts;
                prevTotal = total;
            }

            var prevData1 = list[list.Count - 1];
            var prevData2 = list[list.Count - 2];
            var prevData3 = list[list.Count - 3];
            PredictStartDate = prevData1.Date.AddDays(7);
            list.Add(new DatedDataSeries() {
                Date = PredictStartDate,
                DataSeries = prevCounts.Select((_, i) => (prevData1.DataSeries[i] + prevData2.DataSeries[i] + prevData3.DataSeries[i]) / 3).ToArray()
            });
            return new DatedDataSeriesList { DataSeriesList = list };
        }

        /// <summary>
        /// 死亡者数と重症化者数CSVのロード
        /// <para>
        /// 0: StartDate ,
        /// 1: PredDispStartDate ,
        /// 2: StartSeriousTotal,StartRecoveredTotal ,
        /// 3: MaxDeath,MaxSerious[,MinDeath,MinSerious]
        /// 4: 2020/6/1,death,serious ,
        /// 5: ...</para>
        /// </summary>
        private void loadDeathAndSeriousByAges()
        {
            var items = readFile("Data/csv/death_and_serious.csv").Select(line => line.Trim().Split(',')).ToArray();
            ChartStartDate = items[0][0]._parseDateTime();
            PredDispStartDate = items[1][0]._parseDateTime();
            StartSeriousTotal = items[2][0]._parseDouble();
            StartRecoverTotal = items[2][1]._parseDouble();
            MaxDeath = items[3][0]._parseDouble();
            MaxSerious = items[3][1]._parseDouble();
            MinDeath = items[3]._nth(2)._parseDouble(0);
            MinSerious = items[3]._nth(3)._parseDouble(0);
            var dataStart = items[4][0]._parseDateTime();
            _realDeathSeries = new DatedDataSeries() {
                Date = dataStart,
                DataSeries = items[4..].Select(item => item[1]._parseDouble()).ToArray(),
            };
            _realSeriousSeries = new DatedDataSeries() {
                Date = dataStart,
                DataSeries = items[4..].Select(item => item[2]._parseDouble()).ToArray(),
            };
        }

        private string[] LabelDates = null;

        private double[] RealDeath = null;
        private double[] FullPredictDeath = null;

        private double[] RealSerious = null;
        private double[] FullPredictSerious = null;
        private double[] fullPredictSeriousTotal = null;
        private double[] fullPredictRecoverTotal = null;

        /// <summary>
        /// 予測に必要なデータの準備
        /// </summary>
        /// <param name="infectData"></param>
        public void MakePreliminaryData(InfectData infectData)
        {
            var firstDate = infectData.Dates._first();
            int realDays = Math.Max((PredictStartDate - firstDate).Days, 0);
            double[] dailyInfect = infectData.Newly.Take(realDays).ToArray();
            double[] dailyInPred = infectData.PredNewly.Take(FullDays(firstDate)).Select((x, i) => (i < realDays && dailyInfect[i] > 0) ? dailyInfect[i] : x).ToArray();
            int preambleDays = (ChartStartDate - firstDate).Days;
            LabelDates = (realDays + PredictDays + 1 - preambleDays)._range().Select(n => ChartStartDate.AddDays(n)._toShortDateString()).ToArray();

            // 予測期間における新規陽性者数累計
            LastAccumPositive = (int)Math.Round(infectData.PredNewly.Skip(realDays).Take(PredictDays).Sum(), 0);
#if DEBUG
            //foreach (var p in infectData.PredNewly.Skip(realDays).Take(PredictDays)) Console.WriteLine(p.ToString("f1"));
            Console.WriteLine($"LastAccumPositive={LastAccumPositive}");
#endif

            // death予想
            RealDeath = _realDeathSeries.GetSubSeriesFrom(ChartStartDate);
            double predDeathTotal = _realDeathSeries.DataOn(ChartStartDate.AddDays(-1));
            double[] predDailyDeath = calcPredDailyDeath(firstDate, dailyInPred, realDays + PredictDays);
            double[] fullPredictDeath = predDailyDeath.Select((x, i) => predDeathTotal += x).ToArray();
            FullPredictDeath = fullPredictDeath.Select((x, i) => Math.Round(x)).ToArray();
            LastPredictDeath = (int)FullPredictDeath.Last();

            // serious予想
            RealSerious = _realSeriousSeries.GetSubSeriesFrom(ChartStartDate);
            double predSeriousTotal = StartSeriousTotal;
            double predRecoverTotal = StartRecoverTotal;
            double[] predDailySerious = calcPredDailySerious(firstDate, dailyInPred, realDays + PredictDays);
            fullPredictSeriousTotal = predDailySerious.Select((x, i) => predSeriousTotal += x).ToArray();
            double[] preambleSerious = _realSeriousSeries.GetSubSeriesTo(ChartStartDate);
            double[] fullPredictSerious = preambleSerious._extend(preambleSerious.Length + fullPredictSeriousTotal.Length);
            fullPredictRecoverTotal = new double[fullPredictSeriousTotal.Length];
            for (int i = preambleSerious.Length; i < fullPredictSerious.Length; ++i) {
                int j = i - preambleSerious.Length;
                predRecoverTotal = calcPredTotalRecovered(predRecoverTotal, fullPredictSerious, i);
                fullPredictRecoverTotal[j] = predRecoverTotal;
                fullPredictSerious[i] = fullPredictSeriousTotal[j] - fullPredictDeath[j] - predRecoverTotal;
            }

            (var idx, var val) = fullPredictSerious.Skip(realDays)._top1();
            MaxPredictSerious = (int)Math.Round(val);
            MaxPredictSeriousDate = firstDate.AddDays(realDays + idx);

            FullPredictSerious = fullPredictSerious.Skip(preambleSerious.Length).Select((x, i) => Math.Round(x)).ToArray();
            LastPredictSerious = (int)FullPredictSerious.Last();
        }

        private double[] calcPredDailyDeath(DateTime firstDate, double[] infectPred, int numDays)
        {
            var infectsByAges = infectPred.Select((n, i) => InfectRatesByAges.FindDataSeries(firstDate.AddDays(i)).DataSeries.Select(r => n * r).ToArray()).ToArray();
            var totals = new double[infectsByAges[0].Length];
            var totalByAges = infectsByAges.Select((array, i) => array.Select((n, j) => totals[j] += n).ToArray()).ToArray();
            var averageByAges = totalByAges[AverageShiftDays..].Select((a, i) => {
                int from = Math.Max(i - AverageShiftDays - 1, 0);
                int to = i + AverageShiftDays;
                int days = AverageShiftDays * 2 + 1;
                return a.Select((_, j) => (totalByAges[to][j] - totalByAges[from][j]) / days).ToArray();
            }).ToArray();

            var dailyPred = averageByAges.Select((a, i) => a._dotProduct(DeathRatesByAges.FindDataSeries(firstDate.AddDays(i)).DataSeries)).ToArray();

            int preambleDays = (ChartStartDate - firstDate).Days;
            return (numDays - preambleDays)._range().Select(n => dailyPred[preambleDays + n - DeathRatesByAges.FindDataSeries(ChartStartDate.AddDays(n)).OffsetDays]).ToArray();
        }

        private double[] calcPredDailySerious(DateTime firstDate, double[] infectPred, int numDays)
        {
            var infectsByAges = infectPred.Select((n, i) => InfectRatesByAges.FindDataSeries(firstDate.AddDays(i)).DataSeries.Select(r => n * r).ToArray()).ToArray();
            var totals = new double[infectsByAges[0].Length];
            var totalByAges = infectsByAges.Select((array, i) => array.Select((n, j) => totals[j] += n).ToArray()).ToArray();
            var averageByAges = totalByAges[AverageShiftDays..].Select((a, i) => {
                int from = Math.Max(i - AverageShiftDays - 1, 0);
                int to = i + AverageShiftDays;
                int days = AverageShiftDays * 2 + 1;
                return a.Select((_, j) => (totalByAges[to][j] - totalByAges[from][j]) / days).ToArray();
            }).ToArray();

            var dailyPred = averageByAges.Select((a, i) => a._dotProduct(SeriousRatesByAges.FindDataSeries(firstDate.AddDays(i)).DataSeries)).ToArray();

            int preambleDays = (ChartStartDate - firstDate).Days;
            return (numDays - preambleDays)._range().Select(n => dailyPred[preambleDays + n - SeriousRatesByAges.FindDataSeries(ChartStartDate.AddDays(n)).OffsetDays]).ToArray();
        }

        private double calcPredTotalRecovered(double prevTotal, double[] predSerious, int n)
        {
            var data = RecoverRates.FindDataSeries(_realSeriousSeries.Date.AddDays(n));
            int range = 14;
            int begin = n - data.OffsetDays - (range / 2);
            int end = begin + range;
            return prevTotal + (predSerious[begin..end].Sum() / range) * data.DataSeries[0];
        }

        /// <summary>
        /// 死亡者数予測グラフ用データの作成
        /// </summary>
        /// <returns></returns>
        public JsonData MakeDeathJsonData()
        {
            double y1_max = MaxDeath;
            double y1_min = MinDeath;
            double y1_step = (y1_max - y1_min) / 10;
            Options options = Options.CreateTwoAxes(new Ticks(y1_max, y1_step, y1_min), new Ticks(y1_max, y1_step, y1_min));
            options.AnimationDuration = 500;
            //options.tooltips.intersect = false;
            options.tooltips.SetCustomAverage(0, -1);
            var predDate = PredictStartDate._toShortDateString();
            options.AddAnnotation(predDate, predDate);
            options.legend.SetAlignEnd();
            options.legend.reverse = true;
            options.AddStackedAxis();

            var dataSets = new List<Dataset>();
            dataSets.Add(Dataset.CreateLine("  ", new double?[FullPredictDeath.Length], "rgba(0,0,0,0)", "rgba(0,0,0,0)"));
            double?[] predLine = FullPredictDeath._toNullableArray(0);
            dataSets.Add(Dataset.CreateDotLine("予測死亡者数", predLine, "indianred").SetHoverColors("firebrick").SetDispOrder(2));
            //dataSets.Add(Dataset.CreateDotLine("予測死亡者数(日次)", predDailyDeath._toNullableArray(0), "darkgreen"));
            double?[] realBar = RealDeath._toNullableArray(0);
            dataSets.Add(Dataset.CreateBar("死亡者実数", realBar, "dimgray").SetHoverColors("black").SetStackedAxisId());
            double?[] dummyBars1 = RealDeath.Select(v => y1_max - v).ToArray()._extend(FullPredictDeath.Length, y1_max)._toNullableArray(0, 0);
            //double?[] dummyBars2 = new double[0]._extend(FullPredictDeath.Length, -1)._toNullableArray(0, -1);
            //double?[] dummyBars3 = Dataset.CalcDummyData(FullPredictDeath.Length, new double?[][] { realBar, dummyBars1, dummyBars2 }, new double?[][] { predLine }, null, y1_max);
            dataSets.Add(Dataset.CreateBar("", dummyBars1, "rgba(0,0,0,0)").SetStackedAxisId().SetHoverColors("rgba(10,10,10,0.1)"));
            //dataSets.Add(Dataset.CreateBar("", dummyBars2, "rgba(0,0,0,0)").SetStackedAxisId());
            //dataSets.Add(Dataset.CreateBar("", dummyBars3, "rgba(0,0,0,0)").SetStackedAxisId());

            return new JsonData() {
                chartData = new ChartJson {
                    type = "bar",
                    data = new Data {
                        labels = LabelDates.ToArray(),
                        datasets = dataSets.ToArray(),
                    },
                    options = options,
                }
            };
        }

        /// <summary>
        /// 重症者数予測グラフ用データの作成
        /// </summary>
        /// <returns></returns>
        public JsonData MakeSeriousJsonData()
        {
            double y1_max = MaxSerious;
            double y1_min = MinSerious;
            //double y1_max = 3000;
            double y1_step = (y1_max - y1_min) / 10;
            Options options = Options.CreateTwoAxes(new Ticks(y1_max, y1_step, y1_min), new Ticks(y1_max, y1_step, y1_min));
            options.AnimationDuration = 500;
            //options.tooltips.intersect = false;
            options.tooltips.SetCustomAverage(0, -1);
            var predDate = PredictStartDate._toShortDateString();
            options.AddAnnotation(predDate, predDate);
            options.legend.SetAlignEnd();
            options.legend.reverse = true;
            options.AddStackedAxis();

            var dataSets = new List<Dataset>();
            dataSets.Add(Dataset.CreateLine("  ", new double?[FullPredictSerious.Length], "rgba(0,0,0,0)", "rgba(0,0,0,0)"));
            double?[] predLine = FullPredictSerious._toNullableArray(0);
            dataSets.Add(Dataset.CreateDotLine("予測重症者数", predLine, "darkorange").SetHoverColors("firebrick").SetDispOrder(2));
            //dataSets.Add(Dataset.CreateDotLine("予測重症者数(累積)", fullPredictSeriousTotal._toNullableArray(0), "darkgreen"));
            //dataSets.Add(Dataset.CreateDotLine("予測改善者数(累積)", fullPredictRecoverTotal._toNullableArray(0), "darkblue"));
            double?[] realBar = RealSerious._toNullableArray(0);
            dataSets.Add(Dataset.CreateBar("重症者実数", realBar, "steelblue").SetHoverColors("mediumblue").SetStackedAxisId());
            double?[] dummyBars1 = RealSerious.Select(v => y1_max - v).ToArray()._extend(FullPredictSerious.Length, y1_max)._toNullableArray(0, 0);
            //double?[] dummyBars2 = new double[0]._extend(FullPredictSerious.Length, -1)._toNullableArray(0, -1);
            //double?[] dummyBars3 = Dataset.CalcDummyData(FullPredictSerious.Length, new double?[][] { realBar, dummyBars1, dummyBars2 }, new double?[][] { predLine }, null, y1_max);
            dataSets.Add(Dataset.CreateBar("", dummyBars1, "rgba(0,0,0,0)").SetStackedAxisId().SetHoverColors("rgba(10,10,10,0.1)"));
            //dataSets.Add(Dataset.CreateBar("", dummyBars2, "rgba(0,0,0,0)").SetStackedAxisId());
            //dataSets.Add(Dataset.CreateBar("", dummyBars3, "rgba(0,0,0,0)").SetStackedAxisId());

            return new JsonData() {
                chartData = new ChartJson {
                    type = "bar",
                    data = new Data {
                        labels = LabelDates.ToArray(),
                        datasets = dataSets.ToArray(),
                    },
                    options = options,
                }
            };
        }

    }

}
