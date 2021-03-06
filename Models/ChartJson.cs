using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChartJs.Blazor.ChartJS.Common.Properties;
using Newtonsoft.Json;
using StandardCommon;

namespace ChartBlazorApp.Models
{

    /// <summary>
    /// Chart.js の Chart() 関数に渡す JSONデータを表すクラス
    /// </summary>
    public class ChartJson
    {
        public string type { get; set; }
        public Data data { get; set; }
        public Options options { get; set; }

        public ChartJson RoundData()
        {
            if (data != null) data.RoundData();
            return this;
        }
    }
    public class Data
    {
        public string[] labels { get; set; }
        public Dataset[] datasets { get; set; }

        public void RoundData()
        {
            if (datasets != null) {
                foreach (var dataset in datasets) {
                    dataset.RoundData();
                }
            }
        }
    }

    public class Dataset
    {
        public string label { get; set; }
        /// <summary> double に解釈されるべきデータ </summary>
        public double?[] data { get; set; }
        public string backgroundColor { get; set; }
        public string borderColor { get; set; }
        public string hoverBackgroundColor { get; set; }
        public string hoverBorderColor { get; set; }
        public int order { get; set; } = 1;
        public int dispOrder { get; set; } = 1;
        public string type { get; set; } = "bar";
        public bool fill { get; set; } = false;
        public bool spanGaps { get; set; } = false;
        public double pointRadius { get; set; } = 2.5;
        public double pointHoverRadius { get; set; } = 4.5;
        public double? borderWidth { get; set; } = null;
        public double[] borderDash { get; set; }
        public string xAxisID { get; set; } = null;
        public string yAxisID { get; set; } = "y-1";

        public static Dataset CreateBar(string label, double?[] data, string color, string yAxis = "y-1")
        {
            return new Dataset { label = label, data = data, borderColor = color, backgroundColor = color, order = 5, yAxisID = yAxis };
        }

        public static Dataset CreateBar2(string label, double?[] data, string color)
        {
            return CreateBar(label, data, color, "y-2");
        }

        public static Dataset CreateLine(string label, double?[] data, string bdColor, string bgColor, string yAxis = "y-1")
        {
            return new Dataset { label = label, data = data, borderColor = bdColor, backgroundColor = bgColor, order = 1, type = "line", yAxisID = yAxis };
        }

        public static Dataset CreateLine2(string label, double?[] data, string bdColor, string bgColor)
        {
            return CreateLine(label, data, bdColor, bgColor, "y-2");
        }

        private static double[] _dashParam = new double[] { 10, 3 };
        private static double[] _dotParam = new double[] { 3, 3 };

        public static Dataset CreateDashLine(string label, double?[] data, string color, string yAxis = "y-1", double[] dashParam = null)
        {
            return new Dataset { label = label, data = data, borderColor = color, backgroundColor = color, order = 3, type = "line", pointRadius = 0, borderDash = dashParam ?? _dashParam, yAxisID = yAxis };
        }

        public static Dataset CreateDashLine2(string label, double?[] data, string color, double[] dashParam = null)
        {
            return CreateDashLine(label, data, color, "y-2", dashParam);
        }

        public static Dataset CreateDotLine(string label, double?[] data, string color, string yAxis = "y-1")
        {
            return CreateDashLine(label, data, color, yAxis, _dotParam);
        }

        public static Dataset CreateDotLine2(string label, double?[] data, string color)
        {
            return CreateDashLine(label, data, color, "y-2", _dotParam);
        }

        public static Dataset CreateDataset(string type, string label, double?[] data, string color, string bgColor)
        {
            return new Dataset { type = type, label = label, data = data, borderColor = color, backgroundColor = bgColor, order = 2 };
        }

        public Dataset SetOrders(int order, int dispOrder)
        {
            this.order = order;
            this.dispOrder = dispOrder;
            return this;
        }

        public Dataset SetBorderWidth(double width)
        {
            borderWidth = width;
            return this;
        }

        public Dataset SetPointRadius(double radius)
        {
            pointRadius = radius;
            return this;
        }

        public Dataset SetDispOrder(int order)
        {
            dispOrder = order;
            return this;
        }

        public Dataset SetOrder(int order)
        {
            this.order = order;
            return this;
        }

        public Dataset SetHoverColors(string color)
        {
            hoverBackgroundColor = color;
            hoverBorderColor = color;
            return this;
        }

        public Dataset SetStackedAxisId()
        {
            return SetAxisIds("x-stacked", "y-stacked");
        }

        public Dataset SetAxisIds(string xAxisId, string yAxisId)
        {
            xAxisID = xAxisId;
            yAxisID = yAxisId;
            return this;
        }

        public Dataset RoundData()
        {
            if (data != null) {
                data = data.Select(x => x._round(3)).ToArray();
            }
            return this;
        }

        public static double?[] CalcDummyData(int length, double?[][] stackedValues, double?[][] y1Values, double?[][] y2Values, double y1_max, double y2_max = 0)
        {
            if (y2_max <= 0) y2_max = y1_max;
            double y2toy1(double val) { return val * y1_max / y2_max; }

            double? calcDummyValue(int idx)
            {
                double?[] barVals = stackedValues.Select(a => a._nth(idx)).ToArray();
                double?[] y1Vals = y1Values._notEmpty() ? y1Values.Select(a => a._nth(idx)).ToArray() : null;
                double?[] y2Vals = y2Values._notEmpty() ? y2Values.Select(a => a._nth(idx)).ToArray() : null;
                double sum = 0;
                int num = 0;
                int factor = 1; // 自身の分
                foreach (var v in barVals) {
                    if (v.HasValue) {
                        ++num;
                        if (v >= 0) ++factor;
                    }
                }
                foreach (var v in barVals) {
                    if (v >= 0) sum += v.Value * factor--;
                }
                if (y1Vals._notEmpty()) {
                    foreach (var v in y1Vals) {
                        if (v.HasValue) {
                            ++num;
                            if (v >= 0) sum += v.Value;
                        }
                    }
                }
                if (y2Vals._notEmpty()) {
                    foreach (var v in y2Vals) {
                        if (v.HasValue) {
                            ++num;
                            if (v >= 0) sum += y2toy1(v.Value);
                        }
                    }
                }
                double dispPostion = num < 5 ? 0.8 : 0.75;
                return Math.Max(y1_max * dispPostion * (num + 1) - sum, 0);
            }

            return length._range().Select(i => calcDummyValue(i)).ToArray();
        }

    }

    public class Options
    {
        public int AnimationDuration {
            get { return animation.duration; }
            set { animation.duration = value; }
        }

        public bool maintainAspectRatio { get; set; } = false;

        public Animation animation { get; set; } = new Animation();

        public class Hover { public int animationDuration; }
        public Hover hover { get; set; } = new Hover() { animationDuration = 0 };

        public int responsiveAnimationDuration { get; set; } = 0;

        public Scales scales { get; set; }

        public class Legend {
            public bool display { get; set; } = true;
            public string position { get; set; } = "top";
            public string align { get; set; } = "center";
            public bool reverse { get; set; } = false;

            public void SetAlignStart() { align = "start"; }
            public void SetAlignEnd() { align = "end"; }
        }

        public Legend legend { get; set; } = new Legend();

        public Tooltips tooltips { get; set; } = new Tooltips();

        public Annotation annotation { get; set; } = new Annotation();

        public string[] events { get; set; }

        public void SetOnlyClickEvent(bool flag)
        {
            if (flag) SetEvents("click");
        }

        public void SetEvents(params string[] events)
        {
            this.events = events;
        }

        public static Options Plain(Ticks ticks = null)
        {
            return new Options {
                scales = new Scales {
                    yAxes = new[] {
                        new Yaxis{
                            ticks = ticks ?? new Ticks()
                        }
                    }
                }
            };
        }

        public static Options CreateTwoAxes(Ticks leftTicks = null, Ticks rightTicks = null)
        {
            return new Options {
                scales = new Scales {
                    yAxes = new[] {
                            new Yaxis{ id = "y-1", position = "left", ticks = leftTicks ?? new Ticks() },
                            new Yaxis{ id = "y-2", position = "right", ticks = rightTicks ?? new Ticks() },
                    }
                }
            };
        }

        public Options AddStackedAxis(int yAxisTgt = 0)
        {
            return AddStackedAxis("x-stacked", "y-stacked", yAxisTgt);
        }

        public Options AddStackedAxis(string xAxisId, string yAxisId, int yAxisTgt = 0)
        {
            scales.xAxes = scales.xAxes._extend(scales.xAxes._safeCount() + 1);
            scales.yAxes = scales.yAxes._extend(scales.yAxes._safeCount() + 1);
            scales.xAxes[^1] = new Xaxis() { id = xAxisId, stacked = true, display = false };
            scales.yAxes[^1] = new Yaxis() { id = yAxisId, stacked = true, display = false, ticks = scales.yAxes[yAxisTgt].ticks };
            return this;
        }

        public void AddAnnotation(string value, string label = null, string color = null, string axisId = null, string mode = null)
        {
            var annotations = annotation.annotations;
            if (annotations == null) {
                annotations = new Annotation.Annotation_[1];
            } else {
                annotations = annotations._extend(annotations.Length + 1);
            }
            annotation.annotations = annotations;
            var anno = annotations[^1] = new Annotation.Annotation_();
            if (mode._notEmpty()) anno.mode = mode;
            if (axisId._isEmpty()) axisId = scales.xAxes[0].id;
            if (axisId._notEmpty()) anno.scaleID = axisId;
            if (color._notEmpty()) anno.borderColor = color;
            anno.value = value;
            if (label._notEmpty()) {
                anno.label = new Annotation.Annotation_.Label() { content = label };
            }
        }
    }

    public class Animation
    {
        public int duration { get; set; } = 500;
    }

    public class Annotation
    {
        public class Annotation_
        {
            public string type { get; set; } = "line";
            public string mode { get; set; } = "vertical";
            public string scaleID { get; set; } = "x-axis-0";
            public string value { get; set; }
            public string borderColor { get; set; } = "blue";

            public class Label {
                public string content { get; set; }
                public bool enabled { get; set; } = true;
                public string position { get; set; } = "top";
            }
            public Label label { get; set; } = null;
        }

        public Annotation_[] annotations = null;
    }

    public class Tooltips
    {
        public string mode { get; set; } = "index";
        public bool intersect { get; set; } = true;

        public string position { get; set; } = "average";

        public int _startIdx { get; set; } = 0;

        public int _endIdx { get; set; } = 0;

        public void SetCustomFixed()
        {
            position = "customFixed";
        }

        public void SetCustomHighest()
        {
            position = "customHighest";
        }

        /// <summary> startIdx から length 個の要素の平均。length <= 0 なら elements[elements.length+length-1]要素まで。</summary>
        public void SetCustomAverage(int startIdx, int endIdx)
        {
            position = "customAverage";
            _startIdx = startIdx;
            _endIdx = endIdx;
        }
    }

    public class Scales
    {
        public Xaxis[] xAxes { get; set; } = new Xaxis[] { new Xaxis() };
        public Yaxis[] yAxes { get; set; }
    }

    public class Xaxis
    {
        public string id { get; set; } = "x-1";
        public string position { get; set; } = "bottom";
        public bool display { get; set; } = true;
        public bool stacked { get; set; } = false;
        public bool offset { get; set; } = true;
    }

    public class Yaxis
    {
        public string id { get; set; } = "y-1";
        public string position { get; set; } = "left";
        public Ticks ticks { get; set; }
        public bool display { get; set; } = true;
        public bool stacked { get; set; } = false;
    }

    public class Ticks
    {
        public bool beginAtZero { get; set; } = true;
        public double? min { get; set; }
        public double? max { get; set; }
        public double? stepSize { get; set; }

        public Ticks(double? maxVal = null, double? stepVal = null, double? minVal = null)
        {
            if (maxVal.HasValue) {
                max = maxVal.Value;
                min = minVal.HasValue ? minVal.Value : 0;
                stepSize = stepVal.HasValue ? stepVal.Value : maxVal.Value / 10;
            }
        }
    }

    /// <summary>
    /// ヘルパー拡張メソッド
    /// </summary>
    public static class Extensions
    {
        public static double? _round(this double? d, int digits = 0)
        {
            return d.HasValue ? Math.Round(d.Value, digits) : d;
        }

        public static string _toString(this ChartJson json)
        {
            return json == null ? "" : JsonConvert.SerializeObject(json.RoundData(), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        }
    }
}
