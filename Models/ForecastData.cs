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

        public DatedDataSeriesList NewAdd(DatedDataSeries series)
        {
            return new DatedDataSeriesList() {
                DataSeriesList = new List<DatedDataSeries>(this.DataSeriesList)._safeAdd(series),
            };
        }

        public DatedDataSeries FindDataSeries(DateTime date)
        {
            return DataSeriesList[findDataSeriesIdx(date)];
        }

        public double[] CalcDailyData(double[][] infectsByAges, DateTime firstDate, DateTime chartStartDate, int numDays)
        {
            int preambleDays = (chartStartDate - firstDate).Days;
            double[] daily = new double[numDays];
            double[] result = new double[numDays - preambleDays];
            int day = 0;
            for (int i = findDataSeriesIdx(firstDate); i < DataSeriesList.Count; ++i) {
                var dataSeries = DataSeriesList[i].DataSeries;
                int offset = DataSeriesList[i].OffsetDays;
                var end = i < DataSeriesList.Count - 1 ? Math.Min((DataSeriesList[i + 1].Date - firstDate).Days, numDays) : numDays;
                while (day < end) {
                    daily[day] = infectsByAges[day]._dotProduct(dataSeries);
                    if (day >= preambleDays) result[day - preambleDays] = daily[day - offset];
                    ++day;
                }
                if (day >= numDays) break;
            }
            return result;
        }

        public double[] CalcTotalData(double prevTotal, double[][] infectsByAges, DateTime firstDate, DateTime chartStartDate, int numDays)
        {
            double total = prevTotal;
            return CalcDailyData(infectsByAges, firstDate, chartStartDate, numDays).Select(x => total += x).ToArray();
        }

        public int findDataSeriesIdx(DateTime date)
        {
            for (int i = 1; i < DataSeriesList.Count; ++i) {
                if (date < DataSeriesList[i].Date) return i - 1;
            }
            return DataSeriesList.Count - 1;
        }
    }

    /// <summary>
    /// 予測データ (Singleton)
    /// </summary>
    public class ForecastData
    {
        private static ConsoleLog logger = ConsoleLog.GetLogger();

        /// <summary> グラフ表示開始日 </summary>
        public DateTime ChartStartDate { get; set; }

        /// <summary> 実データ部分も含めた予測表示開始日 </summary>
        public DateTime PredDispStartDate { get; set; }

        /// <summary> 予測に利用する実データの終了日(の翌日) </summary>
        public DateTime PredictStartDate { get; set; }

        public int ChartRealDays { get { return (PredictStartDate - ChartStartDate).Days; } }

        public double StartSeriousTotal { get; set; }

        public double StartRecoverTotal { get; set; }

        public string PredictStartDateStr { get { return PredictStartDate.ToString("M月d日"); } }

        public double YAxisMaxDeath { get; set; }

        public double YAxisMinDeath { get; set; }

        public double YAxisMaxSerious { get; set; }

        public double YAxisMinSerious { get; set; }

        //public DailyData dailyData { get; set; }

        public DatedDataSeriesList InfectRatesByAges { get; private set; } = null;

        public DatedDataSeries RealDeathSeries = null;

        public DatedDataSeries RealSeriousSeries = null;

        public DatedDataSeriesList DeathRatesByAges { get; private set; }

        public DatedDataSeriesList SeriousRatesByAges { get; private set; }

        public DatedDataSeriesList RecoverRates { get; private set; }

        public int RateAveWeeks { get; set; } = 3;

        /// <summary>
        /// 想定死亡率、重症化率CSVのロード
        /// </summary>
        private DatedDataSeriesList loadRatesFile(string csvfile, int num)
        {
            var list = new DatedDataSeriesList() { DataSeriesList = new List<DatedDataSeries>() };
            foreach (var items in readFile(csvfile).Select(line => line.Trim().Split(','))) {
                if (items[0]._startsWith("#")) {
                    if (items[0]._startsWith("#modify")) {
                        list.UpdateDate = items[1]._parseDateTime();
                    }
                } else {
                    int end = Math.Min(items.Length, num);
                    list.DataSeriesList.Add(new DatedDataSeries() {
                        Date = items[0]._parseDateTime(),
                        DataSeries = items[1..end].Select(item => item._parseDouble()).ToArray(),
                        OffsetDays = items[end]._parseInt(0),
                    });
                }
            }
            return list;
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

        public void Initialize(bool bForce = false)
        {
            logger.Info($"CALLED");
            if (m_syncBool.BusyCheck()) return;
            using (m_syncBool) {
                // OnInitialized は2回呼び出される可能性があるので、30秒以内の再呼び出しの場合は、 DailyData の初期化をスキップする
                var prevDt = _lastInitializedDt;
                _lastInitializedDt = DateTime.Now;
                if (!bForce && _lastInitializedDt < prevDt.AddSeconds(30)) {
                    logger.Info($"SKIPPED");
                    return;
                }

                InfectRatesByAges = loadInfectByAgesData();
                loadDeathAndSerious();
                DeathRatesByAges = loadRatesFile(Constants.DEATH_RATE_FILE_PATH, 10);        // 死亡率CSVのロード
                SeriousRatesByAges = loadRatesFile(Constants.SERIOUS_RATE_FILE_PATH, 10);    // 重症化率CSVのロード
                RecoverRates = loadRatesFile(Constants.RECOVER_RATE_FILE_PATH, 2);           // 改善率CSVのロード
                logger.Info($"lastInitialized at {prevDt}, Files reloaded");
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
            List<double[]> infectCountsList = new List<double[]>();
            infectCountsList.Add(new double[9]);
            //double[] prevCounts = new double[9];
            List<double> totalList = new List<double>();
            totalList.Add(0);
            //double prevTotal = 0;
            PredictStartDate = new DateTime(2020, 5, 21);   // とりあえずの初期値
            foreach (var items in readFile(Constants.INFECTION_RATE_FILE_PATH).Select(line => line.Trim().Split(','))) {
                if (items[0]._startsWith("#")) {
                    if (items[0]._startsWith("#aveWeek")) {
                        RateAveWeeks = items._nth(1)._parseInt(3);
                    } else if (items[0]._startsWith("#start")) {
                        var dt = items[1]._parseDateTime();
                        if (dt._isValid()) PredictStartDate = dt.AddDays(1);
                    }
                    continue;
                }
                if (items._safeCount() < 10) continue;

                var prevCounts = infectCountsList.Last();
                var prevTotal = totalList.Last();
                var counts = items[1..10].Select(n => n._parseDouble(0)).ToArray();
                var total = counts.Sum();
                list.Add(new DatedDataSeries() {
                    Date = PredictStartDate,
                    DataSeries = counts.Select((n, i) => (n - prevCounts[i]) / (total - prevTotal)).ToArray()
                });
                PredictStartDate = items[0]._parseDateTime().AddDays(1);
                infectCountsList.Add(counts);
                totalList.Add(total);
            }

            {
                var prevCounts = infectCountsList[^(RateAveWeeks + 1)];
                var prevTotal = totalList[^(RateAveWeeks + 1)];
                var counts = infectCountsList[^1];
                var total = totalList[^1];
                list.Add(new DatedDataSeries() {
                    Date = PredictStartDate,
                    DataSeries = counts.Select((n, i) => (n - prevCounts[i]) / (total - prevTotal)).ToArray()
                });
            }
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
        private void loadDeathAndSerious()
        {
            var items = readFile(Constants.DEATH_AND_SERIOUS_FILE_PATH).Select(line => line.Trim().Split(',')).ToArray();
            ChartStartDate = items[0][0]._parseDateTime();
            PredDispStartDate = items[1][0]._parseDateTime();
            StartSeriousTotal = items[2][0]._parseDouble();
            StartRecoverTotal = items[2][1]._parseDouble();
            YAxisMaxDeath = items[3][0]._parseDouble();
            YAxisMaxSerious = items[3][1]._parseDouble();
            YAxisMinDeath = items[3]._nth(2)._parseDouble(0);
            YAxisMinSerious = items[3]._nth(3)._parseDouble(0);
            var dataStart = items[4][0]._parseDateTime();
            RealDeathSeries = new DatedDataSeries() {
                Date = dataStart,
                DataSeries = items[4..].Select(item => item[1]._parseDouble()).ToArray(),
            };
            RealSeriousSeries = new DatedDataSeries() {
                Date = dataStart,
                DataSeries = items[4..].Select(item => item[2]._parseDouble()).ToArray(),
            };
        }

        /// <summary>
        /// 死亡者数予測グラフ用データの作成
        /// </summary>
        /// <returns></returns>
        public string MakeDeathJsonData(UserForecastData userData, UserForecastData userDataByUser, bool onlyOnClick, bool bAnimation)
        {
            double maxPredDeath = userData.LastPredictDeath._lowLimit(userDataByUser?.LastPredictDeath ?? 0);
            double y1_max = roundYAxis(maxPredDeath, YAxisMaxDeath);
            double y1_min = maxPredDeath > YAxisMaxDeath ? 0 : YAxisMinDeath;
            double y1_step = (y1_max - y1_min) / 10;
            Options options = Options.CreateTwoAxes(new Ticks(y1_max, y1_step, y1_min), new Ticks(y1_max, y1_step, y1_min));
            options.AnimationDuration = bAnimation ? 500 : 0;
            //options.tooltips.intersect = false;
            options.tooltips.SetCustomAverage(0, -1);
            var predDate = PredictStartDate._toShortDateString();
            options.AddAnnotation(predDate, predDate);
            options.legend.SetAlignEnd();
            options.legend.reverse = true;
            options.AddStackedAxis();
            options.SetOnlyClickEvent(onlyOnClick);

            var dataSets = new List<Dataset>();
            dataSets.Add(Dataset.CreateLine("  ", new double?[userData.FullPredictDeath.Length], "rgba(0,0,0,0)", "rgba(0,0,0,0)")); // 凡例の右端マージン用ダミー
            if (userDataByUser != null) {
                double?[] predLineByUser = userDataByUser.FullPredictDeath._toNullableArray(0)._fill(0, ChartRealDays, null);
                dataSets.Add(Dataset.CreateLine("by利用者設定", predLineByUser, "royalblue", "royalblue").SetHoverColors("royalblue").SetOrders(2, 3).SetBorderWidth(0.8).SetPointRadius(0));
            }
            double?[] predLine = userData.FullPredictDeath._toNullableArray(0);
            dataSets.Add(Dataset.CreateDotLine("予測死亡者数", predLine, "indianred").SetHoverColors("firebrick").SetDispOrder(2));
            //dataSets.Add(Dataset.CreateDotLine("予測死亡者数(日次)", predDailyDeath._toNullableArray(0), "darkgreen"));
            double?[] realBar = userData.RealDeath._toNullableArray(0);
            dataSets.Add(Dataset.CreateBar("死亡者実数", realBar, "dimgray").SetHoverColors("black").SetStackedAxisId());
            double?[] dummyBars1 = userData.RealDeath.Select(v => y1_max - v).ToArray()._extend(userData.FullPredictDeath.Length, y1_max)._toNullableArray(0, 0);
            //double?[] dummyBars2 = new double[0]._extend(userData.FullPredictDeath.Length, -1)._toNullableArray(0, -1);
            //double?[] dummyBars3 = Dataset.CalcDummyData(userData.FullPredictDeath.Length, new double?[][] { realBar, dummyBars1, dummyBars2 }, new double?[][] { predLine }, null, y1_max);
            dataSets.Add(Dataset.CreateBar("", dummyBars1, "rgba(0,0,0,0)").SetStackedAxisId().SetHoverColors("rgba(10,10,10,0.1)"));
            //dataSets.Add(Dataset.CreateBar("", dummyBars2, "rgba(0,0,0,0)").SetStackedAxisId());
            //dataSets.Add(Dataset.CreateBar("", dummyBars3, "rgba(0,0,0,0)").SetStackedAxisId());

            return new ChartJson {
                type = "bar",
                data = new Data {
                    labels = userData.LabelDates.ToArray(),
                    datasets = dataSets.ToArray(),
                },
                options = options,
            }._toString();
        }

        /// <summary>
        /// 重症者数予測グラフ用データの作成
        /// </summary>
        /// <returns></returns>
        public string MakeSeriousJsonData(UserForecastData userData, UserForecastData userDataByUser, bool onlyOnClick, bool bAnimation)
        {
            double maxPredSerious = userData.MaxPredictSerious._lowLimit(userDataByUser?.MaxPredictSerious ?? 0);
            double y1_max = roundYAxis(maxPredSerious, YAxisMaxSerious);
            double y1_min = maxPredSerious > YAxisMaxSerious ? 0 : YAxisMinSerious;
            //double y1_max = 3000;
            double y1_step = (y1_max - y1_min) / 10;
            Options options = Options.CreateTwoAxes(new Ticks(y1_max, y1_step, y1_min), new Ticks(y1_max, y1_step, y1_min));
            options.AnimationDuration = bAnimation ? 500 : 0;
            //options.tooltips.intersect = false;
            options.tooltips.SetCustomAverage(0, -1);
            var predDate = PredictStartDate._toShortDateString();
            options.AddAnnotation(predDate, predDate);
            options.legend.SetAlignEnd();
            options.legend.reverse = true;
            options.AddStackedAxis();
            options.SetOnlyClickEvent(onlyOnClick);

            var dataSets = new List<Dataset>();
            dataSets.Add(Dataset.CreateLine("  ", new double?[userData.FullPredictSerious.Length], "rgba(0,0,0,0)", "rgba(0,0,0,0)")); // 凡例の右端マージン用ダミー
            if (userDataByUser != null) {
                double?[] predLineByUser = userDataByUser.FullPredictSerious._toNullableArray(0)._fill(0, ChartRealDays, null);
                dataSets.Add(Dataset.CreateLine("by利用者設定", predLineByUser, "royalblue", "royalblue").SetHoverColors("royalblue").SetOrders(2, 3).SetBorderWidth(0.8).SetPointRadius(0));
            }
            double?[] predLine = userData.FullPredictSerious._toNullableArray(0);
            dataSets.Add(Dataset.CreateDotLine("予測重症者数", predLine, "darkorange").SetHoverColors("firebrick").SetDispOrder(2));
            //dataSets.Add(Dataset.CreateDotLine("予測重症者数(累積)", fullPredictSeriousTotal._toNullableArray(0), "darkgreen"));
            //dataSets.Add(Dataset.CreateDotLine("予測改善者数(累積)", fullPredictRecoverTotal._toNullableArray(0), "darkblue"));
            double?[] realBar = userData.RealSerious._toNullableArray(0);
            dataSets.Add(Dataset.CreateBar("重症者実数", realBar, "steelblue").SetHoverColors("mediumblue").SetStackedAxisId());
            double?[] dummyBars1 = userData.RealSerious.Select(v => y1_max - v).ToArray()._extend(userData.FullPredictSerious.Length, y1_max)._toNullableArray(0, 0);
            //double?[] dummyBars2 = new double[0]._extend(userData.FullPredictSerious.Length, -1)._toNullableArray(0, -1);
            //double?[] dummyBars3 = Dataset.CalcDummyData(userData.FullPredictSerious.Length, new double?[][] { realBar, dummyBars1, dummyBars2 }, new double?[][] { predLine }, null, y1_max);
            dataSets.Add(Dataset.CreateBar("", dummyBars1, "rgba(0,0,0,0)").SetStackedAxisId().SetHoverColors("rgba(10,10,10,0.1)"));
            //dataSets.Add(Dataset.CreateBar("", dummyBars2, "rgba(0,0,0,0)").SetStackedAxisId());
            //dataSets.Add(Dataset.CreateBar("", dummyBars3, "rgba(0,0,0,0)").SetStackedAxisId());

            return new ChartJson {
                type = "bar",
                data = new Data {
                    labels = userData.LabelDates.ToArray(),
                    datasets = dataSets.ToArray(),
                },
                options = options,
            }._toString();
        }

        private double roundYAxis(double maxValue, double yAxis)
        {
            if (maxValue <= yAxis) return yAxis;
            double calcAxis(double val) => Math.Round((double)(val + 499) / 1000.0, 0) * 1000;
            if (maxValue <= 3000) return calcAxis(maxValue * 2) / 2;
            return calcAxis(maxValue);
        }

        /// <summary>
        /// 日別死亡者数グラフ用データの作成
        /// </summary>
        /// <returns></returns>
        public string MakeDailyDeathJonData(UserForecastData userData, UserForecastData userDataByUser, bool onlyOnClick)
        {
            double maxDeath = 0;
            var dataSets = new List<Dataset>();
            dataSets.Add(Dataset.CreateLine("  ", new double?[userData.DailyPredictDeath.Length], "rgba(0,0,0,0)", "rgba(0,0,0,0)")); // 凡例の右端マージン用ダミー
            if (userDataByUser != null) {
                maxDeath = userDataByUser.DailyPredictDeath.Max();
                double?[] predLineByUser = userDataByUser.DailyPredictDeath._toNullableArray(0)._fill(0, ChartRealDays, null);
                dataSets.Add(Dataset.CreateLine("by利用者設定", predLineByUser, "royalblue", "royalblue").SetHoverColors("royalblue").SetOrders(2, 3).SetBorderWidth(0.8).SetPointRadius(0));
            }
            double[] dailyReal = userData.DailyRealDeath._extend(userData.DailyPredictDeath.Length);
            double?[] dailyPredNullable = userData.DailyPredictDeath._toNullableArray(0);
            //dataSets.Add(Dataset.CreateBar("日別死亡者予測数", dailyPredNullable, "silver").SetHoverColors("black").SetStackedAxisId());
            dataSets.Add(Dataset.CreateDotLine("日別死亡者予測数", dailyPredNullable, "indianred").SetHoverColors("firebrick").SetDispOrder(2));
            double?[] realBar = dailyReal._toNullableArray(0);
            dataSets.Add(Dataset.CreateBar("日別死亡者実数", realBar, "dimgray").SetHoverColors("black").SetStackedAxisId());

            maxDeath = Math.Max(userData.DailyRealDeath.Max(), userData.DailyPredictDeath.Max())._lowLimit(maxDeath);
            double y1_max = (((int)maxDeath + 99) / 100) * 100.0;

            double?[] dummyBars1 = dailyReal.Select((v, i) => y1_max - (i < userData.DailyRealDeath.Length ? userData.DailyRealDeath[i] : v)).ToArray()._toNullableArray(0, 0);
            dataSets.Add(Dataset.CreateBar("", dummyBars1, "rgba(0,0,0,0)").SetStackedAxisId().SetHoverColors("rgba(10,10,10,0.1)"));

            double y1_min = 0;
            double y1_step = (y1_max - y1_min) / 10;
            Options options = Options.CreateTwoAxes(new Ticks(y1_max, y1_step, y1_min), new Ticks(y1_max, y1_step, y1_min));
            options.AnimationDuration = 0;
            //options.tooltips.intersect = false;
            options.tooltips.SetCustomAverage(0, -1);
            var predDate = PredictStartDate._toShortDateString();
            //options.AddAnnotation(predDate, predDate);
            options.legend.SetAlignEnd();
            options.legend.reverse = true;
            options.AddStackedAxis();
            options.SetOnlyClickEvent(onlyOnClick);

            return new ChartJson {
                type = "bar",
                data = new Data {
                    labels = userData.LabelDates.Take(dailyReal.Length).ToArray(),
                    datasets = dataSets.ToArray(),
                },
                options = options,
            }._toString();
        }

        /// <summary>
        /// 重症者数 実数／予測差分グラフ用データの作成
        /// </summary>
        /// <returns></returns>
        public string MakeSeriousDiffJsonData(UserForecastData userData, bool onlyOnClick)
        {
            double y1_max = 50;
            double y1_min = -50;
            double y1_step = (y1_max - y1_min) / 10;
            Options options = Options.CreateTwoAxes(new Ticks(y1_max, y1_step, y1_min), new Ticks(y1_max, y1_step, y1_min));
            options.AnimationDuration = 0;
            options.tooltips.intersect = false;
            //options.tooltips.SetCustomAverage(0, -1);
            var predDate = PredictStartDate._toShortDateString();
            //options.AddAnnotation(predDate, predDate);
            options.legend.SetAlignEnd();
            options.legend.reverse = true;
            options.AddStackedAxis();
            options.SetOnlyClickEvent(onlyOnClick);

            var dataSets = new List<Dataset>();
            dataSets.Add(Dataset.CreateLine("  ", new double?[userData.SeriousRealPredictDiff.Length], "rgba(0,0,0,0)", "rgba(0,0,0,0)")); // 凡例の右端マージン用ダミー
            double?[] plusBar = userData.SeriousRealPredictDiff.Select(x => x >= 0 ? (double?)x : null).ToArray();
            double?[] minusBar = userData.SeriousRealPredictDiff.Select(x => x < 0 ? (double?)x : null).ToArray();
            dataSets.Add(Dataset.CreateBar("実数≧予測", plusBar, "indianred").SetHoverColors("darkred").SetStackedAxisId());
            dataSets.Add(Dataset.CreateBar("実数＜予測", minusBar, "seagreen").SetHoverColors("darkgreen").SetStackedAxisId());

            return new ChartJson {
                type = "bar",
                data = new Data {
                    labels = userData.LabelDates.Take(plusBar.Length).ToArray(),
                    datasets = dataSets.ToArray(),
                },
                options = options,
            }._toString();
        }

        /// <summary>
        /// 死亡者数 実数／予測差分グラフ用データの作成
        /// </summary>
        /// <returns></returns>
        public string MakeDeathDiffJsonData(UserForecastData userData, bool onlyOnClick)
        {
            //double maxPredBothDiff = userData.DeathRealPredictDiff?.Last() ?? 100;
            double y1_max = 60;
            double y1_min = -40;
            double y1_step = (y1_max - y1_min) / 10;
            Options options = Options.CreateTwoAxes(new Ticks(y1_max, y1_step, y1_min), new Ticks(y1_max, y1_step, y1_min));
            options.AnimationDuration = 0;
            options.tooltips.intersect = false;
            //options.tooltips.SetCustomAverage(0, -1);
            var predDate = PredictStartDate._toShortDateString();
            //options.AddAnnotation(predDate, predDate);
            options.legend.SetAlignEnd();
            options.legend.reverse = true;
            options.AddStackedAxis();
            options.SetOnlyClickEvent(onlyOnClick);

            var dataSets = new List<Dataset>();
            dataSets.Add(Dataset.CreateLine("  ", new double?[userData.DeathRealPredictDiff.Length], "rgba(0,0,0,0)", "rgba(0,0,0,0)")); // 凡例の右端マージン用ダミー
            double?[] plusBar = userData.DeathRealPredictDiff.Select(x => x >= 0 ? (double?)x : null).ToArray();
            double?[] minusBar = userData.DeathRealPredictDiff.Select(x => x < 0 ? (double?)x : null).ToArray();
            dataSets.Add(Dataset.CreateBar("実数≧予測", plusBar, "indianred").SetHoverColors("darkred").SetStackedAxisId());
            dataSets.Add(Dataset.CreateBar("実数＜予測", minusBar, "seagreen").SetHoverColors("darkgreen").SetStackedAxisId());

            return new ChartJson {
                type = "bar",
                data = new Data {
                    labels = userData.LabelDates.Take(plusBar.Length).ToArray(),
                    datasets = dataSets.ToArray(),
                },
                options = options,
            }._toString();
        }

        /// <summary>
        /// 重症＋死亡者数 実数／予測差分グラフ用データの作成
        /// </summary>
        /// <returns></returns>
        public string MakeBothDiffJsonData(UserForecastData userData, bool onlyOnClick)
        {
            //double maxPredBothDiff = userData.BothSumRealPredictDiff?.Last() ?? 100;
            double y1_max = 50;
            double y1_min = -50;
            double y1_step = (y1_max - y1_min) / 10;
            Options options = Options.CreateTwoAxes(new Ticks(y1_max, y1_step, y1_min), new Ticks(y1_max, y1_step, y1_min));
            options.AnimationDuration = 0;
            options.tooltips.intersect = false;
            //options.tooltips.SetCustomAverage(0, -1);
            var predDate = PredictStartDate._toShortDateString();
            //options.AddAnnotation(predDate, predDate);
            options.legend.SetAlignEnd();
            options.legend.reverse = true;
            options.AddStackedAxis();
            options.SetOnlyClickEvent(onlyOnClick);

            var dataSets = new List<Dataset>();
            dataSets.Add(Dataset.CreateLine("  ", new double?[userData.BothSumRealPredictDiff.Length], "rgba(0,0,0,0)", "rgba(0,0,0,0)")); // 凡例の右端マージン用ダミー
            double?[] plusBar = userData.BothSumRealPredictDiff.Select(x => x >= 0 ? (double?)x : null).ToArray();
            double?[] minusBar = userData.BothSumRealPredictDiff.Select(x => x < 0 ? (double?)x : null).ToArray();
            dataSets.Add(Dataset.CreateBar("実数≧予測", plusBar, "indianred").SetHoverColors("darkred").SetStackedAxisId());
            dataSets.Add(Dataset.CreateBar("実数＜予測", minusBar, "seagreen").SetHoverColors("darkgreen").SetStackedAxisId());

            return new ChartJson {
                type = "bar",
                data = new Data {
                    labels = userData.LabelDates.Take(plusBar.Length).ToArray(),
                    datasets = dataSets.ToArray(),
                },
                options = options,
            }._toString();
        }

        public double[][] calcInfectsByAges(DateTime firstDate, double[] infectPred)
        {
            var infectsByAges = infectPred.Select((n, i) => InfectRatesByAges.FindDataSeries(firstDate.AddDays(i)).DataSeries.Select(r => n * r).ToArray()).ToArray();
            var totals = new double[infectsByAges[0].Length];
            var totalByAges = infectsByAges.Select((array, i) => array.Select((n, j) => totals[j] += n).ToArray()).ToArray();
            const int shiftDays = Constants.FORECAST_AVERAGE_SHIFT_DAYS;
            return totalByAges[shiftDays..].Select((a, i) => {
                int from = Math.Max(i - shiftDays - 1, 0);
                int to = i + shiftDays;
                int days = shiftDays * 2 + 1;
                return a.Select((_, j) => (totalByAges[to][j] - totalByAges[from][j]) / days).ToArray();
            }).ToArray();
        }

        public double[] CalcFullPredictDeath(double[][] infectsByAges, DateTime firstDate, int numDays)
        {
            return CalcFullPredictDeath(DeathRatesByAges, infectsByAges, firstDate, numDays);
        }


        public double[] CalcFullPredictDeath(DatedDataSeriesList ratesByAges, double[][] infectsByAges, DateTime firstDate, int numDays)
        {
            double predDeathTotal = RealDeathSeries.DataOn(ChartStartDate.AddDays(-1));
            return ratesByAges.CalcTotalData(predDeathTotal, infectsByAges, firstDate, ChartStartDate, numDays);
        }

        public double[] CalcFullPredictSerious(double[][] infectsByAges, DateTime firstDate, int numDays, double[] fullPredictDeath)
        {
            return CalcFullPredictSerious(SeriousRatesByAges, infectsByAges, firstDate, numDays, fullPredictDeath);
        }

        public double[] CalcFullPredictSerious(DatedDataSeriesList ratesByAges, double[][] infectsByAges, DateTime firstDate, int numDays, double[] fullPredictDeath)
        {
            double predSeriousTotal = StartSeriousTotal;
            var fullPredictSeriousTotal = ratesByAges.CalcTotalData(predSeriousTotal, infectsByAges, firstDate, ChartStartDate, numDays);

            double predRecoverTotal = StartRecoverTotal;

            double[] preambleSerious = RealSeriousSeries.GetSubSeriesTo(ChartStartDate);
            double[] fullPredictSerious = preambleSerious._extend(preambleSerious.Length + fullPredictSeriousTotal.Length);
            double[] fullPredictRecoverTotal = new double[fullPredictSeriousTotal.Length];
            for (int i = preambleSerious.Length; i < fullPredictSerious.Length; ++i) {
                int j = i - preambleSerious.Length;
                predRecoverTotal = calcPredTotalRecovered(predRecoverTotal, fullPredictSerious, i);
                fullPredictRecoverTotal[j] = predRecoverTotal;
                fullPredictSerious[i] = fullPredictSeriousTotal[j] - fullPredictDeath[j] - predRecoverTotal;
            }

            return fullPredictSerious.Skip(preambleSerious.Length).Select((x, i) => Math.Round(x)).ToArray();
        }

        public double calcPredTotalRecovered(double prevTotal, double[] predSerious, int n)
        {
            var data = RecoverRates.FindDataSeries(RealSeriousSeries.Date.AddDays(n));
            int range = 14;
            int begin = n - data.OffsetDays - (range / 2);
            int end = begin + range;
            return prevTotal + (predSerious[begin..end].Sum() / range) * data.DataSeries[0];
        }

    }

    public class UserForecastData
    {
        private static ConsoleLog logger = ConsoleLog.GetLogger();

        public UserForecastData(bool bExtendDispDays = false)
        {
            ExtendDispDays = bExtendDispDays;
        }

        public bool ExtendDispDays { get; set; } = false;

        public bool UseFourStep { get; set; } = false;

        private int PredictDays { get { return UseFourStep ? Constants.FORECAST_PREDICTION_DAYS_FOR_DETAIL : Constants.FORECAST_PREDICTION_DAYS * (ExtendDispDays ? 2 : 1); } }

        public DateTime LastPredictDate { get { return chartPredStartDate.AddDays(PredictDays - 1); } }

        public string LastPredictDateStr { get { return LastPredictDate.ToString("M月d日"); } }

        public int LastPredictDeath { get; set; }

        public int LastPredictSerious { get; set; }

        public int MaxPredictSerious { get; set; }

        public DateTime MaxPredictSeriousDate { get; set; }

        public string MaxPredictSeriousDateStr { get { return MaxPredictSeriousDate.ToString("M月d日"); } }

        public int LastAccumPositive { get; set; }

        public string[] LabelDates { get; set; } = null;

        public double[] RealDeath { get; set; } = null;
        public double[] FullPredictDeath { get; set; } = null;

        public double[] RealSerious { get; set; } = null;
        public double[] FullPredictSerious = null;

        public double[] DailyRealDeath { get; set; } = null;
        public double[] DailyPredictDeath { get; set; } = null;

        public double[] DeathRealPredictDiff { get; set; } = null;
        public double DeathDiffMSE { get; set; } = 0;

        public double[] SeriousRealPredictDiff { get; set; } = null;
        public double SeriousDiffMSE { get; set; } = 0;

        public double[] BothSumRealPredictDiff { get; set; } = null;
        public double BothDiffMSE { get; set; } = 0;

        private DateTime chartPredStartDate;

        /// <summary>
        /// 予測に必要なデータの準備
        /// </summary>
        /// <param name="infectData"></param>
        public UserForecastData MakeData(ForecastData forecastData, InfectData infectData, RtDecayParam rtParam, bool byUser = false)
        {
            UseFourStep = rtParam?.Fourstep ?? false;
            chartPredStartDate = forecastData.PredictStartDate;

            DateTime firstDate = infectData.Dates._first();
            DateTime chartStartDate = forecastData.ChartStartDate;

            int preambleDays = Math.Max((chartStartDate - firstDate).Days, 0);
            int predDays = PredictDays;

            DateTime calcPredStartDt = (rtParam != null && chartPredStartDate <= rtParam.EffectiveStartDate) ? rtParam.EffectiveStartDate.AddDays(1) : chartPredStartDate ;
            int calcFullRealDays = Math.Max((calcPredStartDt - firstDate).Days, 0);
            //int calcFullPredDays = calcFullRealDays + predDays;
            int calcRealDays = calcFullRealDays - preambleDays;

            int chartFullRealDays = Math.Max((chartPredStartDate - firstDate).Days, 0);
            int chartFullPredDays = chartFullRealDays + predDays;
            int chartFullDays = chartFullPredDays + Constants.FORECAST_AVERAGE_SHIFT_DAYS + 7;
            int chartLabelDays = chartFullPredDays + 1 - preambleDays;

            logger.Debug($"CALL UserPredictData.PredictValuesEx({rtParam}, fullDays={chartFullDays}, extDays={predDays + 7}, predStartDt={calcPredStartDt._toDateString()})");
            var predData = UserPredictData.PredictValuesEx(infectData, rtParam, chartFullDays, predDays + 7, calcPredStartDt);

            double[] dailyInfect = infectData.Newly.Take(calcFullRealDays).ToArray();
            double[] dailyPredInfect = predData.PredNewly.Take(chartFullDays).Select((x, i) => (i < calcFullRealDays && dailyInfect._nth(i) > 0) ? dailyInfect[i] : x).ToArray();
            LabelDates = chartLabelDays._range().Select(n => forecastData.ChartStartDate.AddDays(n)._toShortDateString()).ToArray();

            // 予測期間における新規陽性者数累計
            LastAccumPositive = (int)Math.Round(predData.PredNewly.Skip(calcFullRealDays).Take(predDays).Sum(), 0);
            //foreach (var p in predData.PredNewly.Skip(realDays).Take(predDays)) logger.Debug(p.ToString("f1"));
            logger.Info($"LastAccumPositive={LastAccumPositive}, ByUser={byUser}, {rtParam}");

            // 年代ごとの日別陽性者数データ
            double[][] infectsByAges = forecastData.calcInfectsByAges(firstDate, dailyPredInfect);

            // death予想
            RealDeath = forecastData.RealDeathSeries.GetSubSeriesFrom(forecastData.ChartStartDate);
            double[] fullPredictDeath = forecastData.CalcFullPredictDeath(infectsByAges, firstDate, chartFullPredDays);
            FullPredictDeath = fullPredictDeath.Select((x, i) => Math.Round(x)).ToArray();
            LastPredictDeath = (int)FullPredictDeath.Last();

            // serious予想
            RealSerious = forecastData.RealSeriousSeries.GetSubSeriesFrom(forecastData.ChartStartDate);
            FullPredictSerious = forecastData.CalcFullPredictSerious(infectsByAges, firstDate, chartFullPredDays, fullPredictDeath);

            (var idx, var val) = FullPredictSerious.Skip(calcRealDays)._top1();
            MaxPredictSerious = (int)Math.Round(val);
            MaxPredictSeriousDate = chartPredStartDate.AddDays(idx);

            LastPredictSerious = (int)FullPredictSerious.Last();

            // 日別死亡者数
            double firstVal = forecastData.RealDeathSeries.DataOn(forecastData.ChartStartDate) - forecastData.RealDeathSeries.DataOn(forecastData.ChartStartDate.AddDays(-1));
            DailyRealDeath = RealDeath.Length._range().Select(i => i == 0 ? firstVal : RealDeath[i] - RealDeath[i - 1]).ToArray();
            DailyPredictDeath = new double[FullPredictDeath.Length];
            //for (int i = DailyRealDeath.Length; i < DailyPredictDeath.Length; ++i) DailyPredictDeath[i] = FullPredictDeath[i] - FullPredictDeath[i - 1];
            for (int i = 1; i < DailyPredictDeath.Length; ++i) DailyPredictDeath[i] = FullPredictDeath[i] - FullPredictDeath[i - 1];

            // death の差分
            DeathRealPredictDiff = RealDeath.Length._range().Select(i => RealDeath[i] - FullPredictDeath[i]).ToArray();
            DeathDiffMSE = calcMSE(DeathRealPredictDiff);
            logger.Debug($"Death MSE={DeathDiffMSE:f1}");

            // serious の差分
            SeriousRealPredictDiff = RealSerious.Length._range().Select(i => RealSerious[i] - FullPredictSerious[i]).ToArray();
            SeriousDiffMSE = calcMSE(SeriousRealPredictDiff);
            logger.Debug($"Serious MSE={SeriousDiffMSE:f1}");

            // death + serious の差分
            BothSumRealPredictDiff = RealDeath.Length._range().Select(i => (RealDeath[i] + RealSerious[i]) - (FullPredictDeath[i] + FullPredictSerious[i])).ToArray();
            BothDiffMSE = calcMSE(BothSumRealPredictDiff);
            logger.Debug($"BothSum MSE={BothDiffMSE:f1}");

            return this;
        }

        private double calcMSE(double[] array)
        {
            return array._isEmpty() ? 0 : Math.Round(array.Select(x => x * x).Sum() / array.Length, 1);
        }
    }
    
}
