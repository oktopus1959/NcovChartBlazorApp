using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using StandardCommon;

namespace ChartBlazorApp.Models
{
    /// <summary>
    /// シングルトンデータクラス
    /// </summary>
    public class DailyData
    {
        private static ConsoleLog logger = ConsoleLog.GetLogger();

        public List<InfectData> InfectDataList {
            get {
                if (_infectDataList == null) Initialize();
                return _infectDataList;
            }
        }

        public int InfectDataCount { get { return _infectDataList?.Count ?? 0; } }

        private List<InfectData> _infectDataList = null;

        public DateTime LastUpdateDt { get; private set; }

        public static int ReloadMagicNumber { get; private set; } = 9999;

        public DailyData()
        {
            Initialize();
            runReloadTask();
        }

        private void runReloadTask()
        {
            Task.Run(() => {
                int period = (ConsoleLog.DEBUG_FLAG ? 10 : 60) * 1000;
                while (true) {
                    Task.Delay(period).Wait();
                    Initialize();
                }
            });
        }

        private SyncBool m_syncBool = new SyncBool();
        private DateTime _lastInitializedDt;
        private DateTime _lastFileDt;

        public void Initialize(bool bForce = false, DateTime? lastRealDt = null)
        {
            logger.Trace($"CALLED");
            if (m_syncBool.BusyCheck()) return;
            using (m_syncBool) {
                // OnInitialized は2回呼び出される可能性があるので、30秒以内の再呼び出しの場合は、 DailyData の初期化をスキップする
                var prevDt = _lastInitializedDt;
                _lastInitializedDt = DateTime.Now;
                int skipSec = ConsoleLog.DEBUG_FLAG ? 5 : 30;
                if (!bForce && _lastInitializedDt < prevDt.AddSeconds(skipSec)) {
                    logger.Info($"SKIPPED");
                    return;
                }

                try {
                    string filePath = Constants.PREF_FILE_PATH;
                    var fileInfo = Helper.GetFileInfo(filePath);
                    if (bForce || fileInfo.ModifyDt > _lastFileDt) {
                        // ファイルが更新されていたら再ロードする
                        _lastFileDt = fileInfo.ModifyDt;
                        _infectDataList = loadPrefectureData(readFile(filePath), lastRealDt);
                        loadFourstepHopeParams();
                        LastUpdateDt = _lastInitializedDt;
                        logger.Info($"prev Initialized at {prevDt}, Reload:{filePath}");
                    }
                } catch (Exception e) {
                    logger.Error(e.ToString());
                }
            }
        }

        public static double CalcRt(double[] total, int idx)
        {
            double weekly(int i) => total._nth(i) - total._nth(i - 7);
            double w7 = weekly(idx - 7);
            return w7 > 0 ? Math.Pow(weekly(idx) / w7, 5.0 / 7.0) : 0.0;
        }

        private List<InfectData> loadPrefectureData(IEnumerable<string> lines, DateTime? lastRealDt = null)
        {
            List<string> prefOrder = new List<string>();
            var prefDataDict = new Dictionary<string, PrefInfectData>();
            PrefInfectData getOrNewData(string[] items, bool bAddOrder)
            {
                PrefInfectData data = null;
                var dispName = items[1];
                var keyName = items[2];
                if (keyName._notEmpty()) {
                    data = prefDataDict._safeGetOrNewInsert(keyName);
                    data.InitializeIfNecessary(dispName);
                    if (bAddOrder && !prefOrder.Contains(keyName)) prefOrder.Add(keyName);
                }
                return data;
            }

            foreach (var line in lines) {
                var items = line.Trim().Split(',');
                if (items._isEmpty()) continue;

                if (items[0]._startsWith("#order")) {
                    prefOrder.AddRange(items[1..]);
                } else if (items[0]._startsWith("#reload")) {
                    ReloadMagicNumber = items[1]._parseInt(9999);
                    logger.Info($"ReloadMagicNumber: {ReloadMagicNumber}");
                } else if (items[0]._startsWith("#params")) {
                    var data = getOrNewData(items, false);
                    if (data != null) {
                        data.AddYAxesMax(items._nth(3)._parseDouble(0.0), items._nth(4)._parseDouble(0.0));
                        data.AddDecayParam(
                            items._nth(5)._parseDateTime(),
                            items._nth(6)._parseInt(0),
                            Pages.MyChart.GetDecayFactor(items._nth(7)._parseDouble(-9999)),
                            items._nth(8)._parseDouble(0),
                            items._nth(9)._parseDouble(0),
                            Pages.MyChart.GetDecayFactor2(items._nth(10)._parseDouble(-9999)));
                    }
                } else if (items[0]._startsWith("#events")) {
                    if (items.Length >= 4) {
                        var data = getOrNewData(items, false);
                        data.AddEvents(items[3..]);
                    }
                } else if (items[0]._startsWith("#shifts")) {
                    if (items.Length >= 4) {
                        var data = getOrNewData(items, false);
                        data.AddShiftRanges(items[3..]);
                    }
                } else if (items[0]._startsWith("#end_of_mhlw")) {
                    foreach (var data in prefDataDict.Values) {
                        data.ShiftPrefData();
                    }
                } else if (items[0]._startsWith("20")) {
                    var data = getOrNewData(items, true);
                    if (data != null) {
                        var dt = items._nth(0)._parseDateTime();
                        if (dt._isValid()) {
                            var val = items._nth(3)._parseDouble(0);
                            var flag = items._nth(4);
                            data.AddData(dt, val, flag);
                        }
                    }
                }
            }

            var infectList = new List<InfectData>();
            foreach (var pref in prefOrder) {
                var data = prefDataDict._safeGet(pref);
                if (data != null) infectList.Add(data.MakeData());
            }
            return infectList;
        }

        public class ExpectdFourstepParam
        {
            public string ButtonText;
            public string StartDate;
            public int DaysToRt1;
            public double Rt1;
            public int DaysToRt2;
            public double Rt2;
            public int DaysToRt3;
            public double Rt3;
            public int DaysToRt4;
            public double Rt4;
        }

        public static ExpectdFourstepParam[] ExpectedFourstepParams { get; private set; } = null;

        /// <summary>
        /// 4段階設定の希望設定をロードする
        /// </summary>
        private void loadFourstepHopeParams()
        {
            ExpectedFourstepParams = readFile(Constants.EXPECT_FILE_PATH).
                Select(line => line.Trim()._reReplace(" *", "")._reReplace(",#.*", "").Split(',')).
                Where(items => items._length() >= 3 && !items[0]._startsWith("#")).
                Select(items => new ExpectdFourstepParam() {
                    ButtonText = items[0],
                    StartDate = items[1],
                    DaysToRt1 = items._nth(2)._parseInt(0),
                    Rt1 = items._nth(3)._parseDouble(1),
                    DaysToRt2 = items._nth(4)._parseInt(0),
                    Rt2 = items._nth(5)._parseDouble(1),
                    DaysToRt3 = items._nth(6)._parseInt(0),
                    Rt3 = items._nth(7)._parseDouble(1),
                    DaysToRt4 = items._nth(8)._parseInt(0),
                    Rt4 = items._nth(9)._parseDouble(1),
                }).
                ToArray();
        }

        // [JSInvokable] 属性を付加すると JavaScript から呼び出せるようになる。(今回は使用していないが参考のため残してある)
        // 呼び出し方法については _Host.cshtml の renderChart0 関数、および GompertzInterop クラスを参照のこと。
        // 参照: https://docs.microsoft.com/ja-jp/aspnet/core/blazor/call-dotnet-from-javascript?view=aspnetcore-3.1
        [JSInvokable]
        public string GetChartData(InfectData infData, int yAxisMax, string endDate, bool estimatedBar, bool onlyOnClick, bool bAnimation)
        {
            //(var jsonStr, var dispDays) = MakeJsonData(infData, yAxisMax, endDate._parseDateTime(), null, 0, estimatedBar, onlyOnClick, bAnimation);
            return "";
        }

        private static List<string> getFiles(string dirPath, string type)
        {
            try {
                return System.IO.Directory.GetFiles(dirPath, $"*.{type}.csv", System.IO.SearchOption.TopDirectoryOnly).OrderBy(x => x).ToList();
            } catch (Exception) {
                return new List<string>();
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

        public string[] GetDecayStartDates(int duration)
        {
            if (_infectDataList._isEmpty()) return new string[0];

            return _infectDataList.Select(data => (data == null) ? "" : data.GetDecayStartDateStr(duration)).ToArray();
        }

    } // DailyData

}

