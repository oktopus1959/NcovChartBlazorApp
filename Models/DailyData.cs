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

        public static int ReloadMagicNumber { get; private set; } = 9999;

        public DailyData()
        {
            Initialize();
            runReloadTask();
        }

        private void runReloadTask()
        {
            Task.Run(() => {
                while (true) {
                    Task.Delay(60 * 1000).Wait();
                    Initialize();
                }
            });
        }

        private SyncBool m_syncBool = new SyncBool();
        private DateTime _lastInitializedDt;
        private DateTime _lastFileDt;

        public void Initialize(bool bForce = false, DateTime? lastRealDt = null)
        {
            //logger.Debug($"CALLED");
            if (m_syncBool.BusyCheck()) return;
            using (m_syncBool) {
                // OnInitialized は2回呼び出される可能性があるので、30秒以内の再呼び出しの場合は、 DailyData の初期化をスキップする
                var prevDt = _lastInitializedDt;
                _lastInitializedDt = DateTime.Now;
                if (!bForce && _lastInitializedDt < prevDt.AddSeconds(30)) return;

                string filePath = Constants.PREF_FILE_PATH;
                var fileInfo = Helper.GetFileInfo(filePath);
                if (bForce || fileInfo.ModifyDt > _lastFileDt) {
                    // ファイルが更新されていたら再ロードする
                    _lastFileDt = fileInfo.ModifyDt;
                    _infectDataList = loadPrefectureData(readFile(filePath), lastRealDt);
                    logger.Info($"lastInitialized at {prevDt}, Reload:{filePath}");
                }
            }
        }

        private static DateTime _firstDate = "2020/6/1"._parseDateTime();

        public class PrefInfectData
        {
            public string Title { get; set; }
            public double Y1_Max { get; set; }
            public double Y2_Max { get; set; }
            public RtDecayParam DecayParam { get; set; }
            public List<DateTime> Dates { get; set; }
            public List<double> Total { get; set; }
            public int PreDataNum { get; set; }
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
                    if (data.Title._isEmpty()) {
                        data.Title = dispName;
                        data.DecayParam = new RtDecayParam();   // 全て 0 の初期データ
                        data.Dates = new List<DateTime>();
                        data.Total = new List<double>();
                    }
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
                        data.Y1_Max = items._nth(3)._parseDouble(0.0);
                        data.Y2_Max = items._nth(4)._parseDouble(0.0);
                        data.DecayParam.StartDate = items._nth(5)._parseDateTime();
                        data.DecayParam.DaysToOne = items._nth(6)._parseInt(0);
                        data.DecayParam.DecayFactor = Pages.MyChart.GetDecayFactor(items._nth(7)._parseDouble(-9999));
                        data.DecayParam.EasyRt1 = items._nth(8)._parseDouble(0);
                        data.DecayParam.EasyRt2 = items._nth(9)._parseDouble(0);
                        data.DecayParam.DecayFactorNext = Pages.MyChart.GetDecayFactor2(items._nth(10)._parseDouble(-9999));
                    }
                } else if (items[0]._startsWith("20")) {
                    var data = getOrNewData(items, true);
                    if (data != null) {
                        var dt = items._nth(0)._parseDateTime();
                        if (lastRealDt == null || dt <= lastRealDt) {
                            var val = items._nth(3)._parseDouble(0);
                            if (data.Dates._isEmpty() || data.Dates.Last() < dt) {
                                data.Total.Add(val);
                                data.Dates.Add(dt);
                                if (dt < _firstDate) ++data.PreDataNum;
                            } else {
                                for (int i = data.Dates.Count() - 1; i >= 0; --i) {
                                    if (data.Dates[i] == dt) {
                                        data.Total[i] = val;
                                    } else if (data.Dates[i] > dt) {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            double[] adjustTotal(double[] total)
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

            double calcY1Max(double[] newlies)
            {
                int newlyMax = (int)newlies[(newlies.Length - Constants.Y_MAX_CALC_DURATION)._lowLimit(0)..].Max();
                if (newlyMax < 100) {
                    return ((newlyMax + 10) / 10) * 10.0;
                } else if (newlyMax < 1000) {
                    return ((newlyMax + 100) / 100) * 100.0;
                } else if (newlyMax < 10000) {
                    return ((newlyMax + 1000) / 1000) * 1000.0;
                } else if (newlyMax < 100000) {
                    return ((newlyMax + 10000) / 10000) * 10000.0;
                } else {
                    return ((newlyMax + 100000) / 100000) * 100000.0;
                }
            }

            double calcY2Max(double[] rts)
            {
                int nearPt = (rts.Length - 30)._lowLimit(0);
                int longPt = (rts.Length - Constants.Y_MAX_CALC_DURATION)._lowLimit(0);
                if (nearPt > 0 && rts[longPt..nearPt].Count((x) => x > 2.5) > 3)
                    return 5.0;
                if (rts.Length > 0 && rts[nearPt..].Max() > 2.5)
                    return 5.0;
                return 2.5;
            }

            InfectData makeData(PrefInfectData data)
            {
                int predatanum = data.PreDataNum;
                double total(int i) => data.Total._nth(i + predatanum);
                double newly(int i) => total(i) - total(i - 1);
                var dates = data.Dates.Skip(predatanum).ToArray();
                var newlies = (data.Total.Count - predatanum)._range().Select(i => newly(i)).ToArray();
                double[] adjustedTotal = adjustTotal(data.Total.ToArray());
                double adjTotal(int i) => adjustedTotal._nth(i + predatanum);
                //double weekly(int i) => total(i) - total(i - 7);
                double weekly(int i) => adjTotal(i) - adjTotal(i - 7);
                double average(int i) => weekly(i) / 7;
                var averages = (data.Total.Count - predatanum)._range().Select(i => average(i)).ToArray();
                double rt(int i) { double w7 = weekly(i - 7); return w7 > 0 ? Math.Pow(weekly(i) / w7, 5.0 / 7.0) : 0.0; };
                var rts = (data.Total.Count - predatanum)._range().Select(i => rt(i)).ToArray();
                var y1_max = data.Y1_Max > 0 ? data.Y1_Max : calcY1Max(newlies);
                var y1_step = y1_max / 10;
                var y2_max = data.Y2_Max > 0 ? data.Y2_Max : calcY2Max(rts);
                var y2_step = y2_max / 5;
                //以下を有効にしてしまうと、システム既定基準日とシステム既定検出遡及日による基準日との区別ができなくなってしまう。
                //if (data.DecayParam.StartDate._notValid()) data.DecayParam.StartDate = dates[0].AddDays(InfectData.FindRecentMaxIndex(rts));
                var infectData = new InfectData {
                    Title = data.Title,
                    Y1_Max = y1_max,
                    Y1_Step = y1_step,
                    Y2_Max = y2_max,
                    Y2_Step = y2_step,
                    Dates = dates,
                    Newly = newlies,
                    Average = averages,
                    Rt = rts,
                    InitialDecayParam = data.DecayParam,
                };
                infectData.InitialSubParams = infectData.CalcDecaySubParams();
                return infectData;
            }

            var infectList = new List<InfectData>();
            foreach (var pref in prefOrder) {
                var data = prefDataDict._safeGet(pref);
                if (data != null) infectList.Add(makeData(data));
            }
            return infectList;
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

