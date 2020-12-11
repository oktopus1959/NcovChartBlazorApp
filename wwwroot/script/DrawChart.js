function insertStaticDescription(html) {
    document.getElementById("static-description").innerHTML = html;
}

function insertStaticDescription2(html) {
    document.getElementById("static-description2").innerHTML = html;
}

function insertAboutDescription(html) {
    document.getElementById("about-description").innerHTML = html;
}

function insertOthersDescription(html) {
    document.getElementById("others-description").innerHTML = html;
}

function showOrHideDiv(descDivId, targetId) {
    document.getElementById(targetId).style.display = descDivId == targetId ? "block" : "none";
}

function selectDescription(descDivId, paramId, paramVal) {
    showOrHideDiv(descDivId, "home-page");
    showOrHideDiv(descDivId, "forecast-page");
    showOrHideDiv(descDivId, "about-page");
    if (paramId && paramVal) {
        document.getElementById(paramId).innerHTML = paramVal;
    }
}

const readAsTextReader = file => {
    const reader = new FileReader();

    return new Promise((resolve, reject) => {
        reader.onerror = () => {
            reader.abort();
            reject("error");
        };

        reader.onload = () => {
            console.log("onload");
            resolve(reader.result);
        };

        reader.readAsText(file);
    });
};

function uploadFile(event) {
    const files = event.target.files;
    if (files.length == 0) return;

    const file = files[0];
    console.log("uploadFile: " + file);

    readAsTextReader(file)
        .then(value => {
            console.log(value);
            return value;
        })
        .catch(reason => {
            alert(reason);
        });
}

function getLocalStorage(key) {
    return localStorage.getItem(key);
}

function setLocalStorage(key, value) {
    localStorage.setItem(key, value);
}

function ChartDrawer(wrapperId) {
    //console.log('ENTER: ChartDrawer(' + wrapperId + ')');

    // グラフ描画用 canvas
    this.cvsChart = null;
    this.ctxChart = null;

    // Y軸コピー用 canvas
    this.cvsYAxis1 = null;
    this.ctxYAxis1 = null;
    this.cvsYAxis2 = null;
    this.ctxYAxis2 = null;

    // X軸の1データ当たりの幅
    this.xAxisStepSize = 12;

    // X軸の1データ当たりの幅
    this.xAxisWidthRate = 2;

    // 棒の幅率
    this.currentBarWidth = -999;

    // グラフ全体の幅を計算
    this.chartWidth = 0; //data.length * this.xAxisStepSize;

    // グラフ用 canvas の高さ
    this.chartHeight = 400;

    // 初期スクロール量
    this.initialScrollRate = 0;

    // 二重実行防止用フラグ
    this.copyYAxisCalled = false;

    // Y軸イメージのコピー関数
    this.copyYAxisImage = (chart) => {
        //console.log("ENTER: copyYAxisImage Called="+this.copyYAxisCalled);

        if (this.copyYAxisCalled) return;

        this.copyYAxisCalled = true;

        // グラフ描画後は、canvas.width(height):canvas.style.width(height) 比は、下記 scale の値になっている
        var scale = window.devicePixelRatio;

        //console.log("scale=" + scale);
        //console.log("this.currentBarWidth=" + this.currentBarWidth);
        //console.log("this.chartWidth=" + this.chartWidth);

        // グラフ描画用canvasのstyle.width(すなわち全データを描画するのに必要なサイズ)に上記の幅を設定
        //this.cvsChart.style.width = this.chartWidth + "px";
        // グラフ用 canvas サイズの変更
        //this.cvsChart.width = this.chartWidth * window.devicePixelRatio;

        // Y軸のスケール情報
        var yAxScale1 = chart.scales['y-1'];
        var yAxScale2 = chart.scales['y-2'];

        // Y軸部分としてグラフからコピーすべき幅 (TODO: 良く分かっていない)
        var yAxisStyleWidth0_1 = yAxScale1.width - 10;
        var yAxisStyleWidth0_2 = yAxScale2.width - 10;

        // canvas におけるコピー幅(yAxisStyleWidth0を直接使うと微妙にずれるので、整数値に切り上げる)
        var copyWidth1 = Math.ceil(yAxisStyleWidth0_1 * scale);
        var copyWidth2 = Math.ceil(yAxisStyleWidth0_2 * scale);
        // Y軸canvas の幅(右側に少し空白部を残す)
        var yAxisCvsWidth1 = copyWidth1 + 4;
        var yAxisCvsWidth2 = copyWidth2 + 4;
        // 実際の描画幅(styleに設定する)
        var yAxisStyleWidth1 = yAxisCvsWidth1 / scale;
        var yAxisStyleWidth2 = yAxisCvsWidth2 / scale;

        // Y軸部分としてグラフからコピーすべき高さ (TODO: 良く分かっていない) ⇒これを実際の描画高とする(styleに設定)
        var yAxisStyleHeight1 = yAxScale1.height + yAxScale1.top + 10;
        var yAxisStyleHeight2 = yAxScale2.height + yAxScale2.top + 10;
        // canvas におけるコピー高
        var copyHeight1 = yAxisStyleHeight1 * scale;
        var copyHeight2 = yAxisStyleHeight2 * scale;
        // Y軸canvas の高さ
        var yAxisCvsHeight1 = copyHeight1;
        var yAxisCvsHeight2 = copyHeight2;

        //console.log("this.cvsChart.style.width=" + this.cvsChart.style.width);
        //console.log("this.cvsChart.width=" + this.cvsChart.width);
        //console.log('--------------------------');
        //console.log("copyWidth1=" + copyWidth1);
        //console.log("copyWidth2=" + copyWidth2);
        //console.log("yAxisCvsWidth1=" + yAxisCvsWidth1);
        //console.log("yAxisCvsWidth2=" + yAxisCvsWidth2);
        //console.log("yAxisStyleWidth0_1=" + yAxisStyleWidth0_1);
        //console.log("yAxisStyleWidth0_2=" + yAxisStyleWidth0_2);
        //console.log("yAxisStyleWidth1=" + yAxisStyleWidth1);
        //console.log("yAxisStyleWidth2=" + yAxisStyleWidth2);
        //console.log("this.cvsChart.style.height=" + this.cvsChart.style.height);
        //console.log("this.cvsChart.height=" + this.cvsChart.height);
        //console.log("copyHeight1=" + copyHeight1);
        ////console.log("copyHeight2=" + copyHeight2);
        //console.log("yAxisCvsHeight1=" + yAxisCvsHeight1);
        ////console.log("yAxisCvsHeight2=" + yAxisCvsHeight2);
        //console.log("yAxisStyleHeight1=" + yAxisStyleHeight1);
        ////console.log("yAxisStyleHeight2=" + yAxisStyleHeight2);

        // 下記はやってもやらなくても結果が変わらないっぽい
        //this.ctxYAxis1.scale(scale, scale);
        //this.ctxYAxis2.scale(scale, scale);

        // Y軸canvas の幅と高さを設定
        this.cvsYAxis1.width = yAxisCvsWidth1;
        this.cvsYAxis1.height = yAxisCvsHeight1;
        this.cvsYAxis2.width = yAxisCvsWidth2;
        this.cvsYAxis2.height = yAxisCvsHeight2;

        // Y軸canvas.style(実際に描画される大きさ)の幅と高さを設定
        this.cvsYAxis1.style.width = yAxisStyleWidth1 + "px";
        this.cvsYAxis1.style.height = yAxisStyleHeight1 + "px";
        this.cvsYAxis2.style.width = yAxisStyleWidth2 + "px";
        this.cvsYAxis2.style.height = yAxisStyleHeight1 + "px";

        // グラフcanvasからY軸部分のイメージをコピーする
        this.ctxYAxis1.drawImage(this.cvsChart, 0, 0, copyWidth1, copyHeight1, 0, 0, copyWidth1, copyHeight1);
        this.ctxYAxis2.drawImage(this.cvsChart, this.cvsChart.width - copyWidth2, 0, copyWidth2, copyHeight2, this.cvsYAxis2.width - copyWidth2, 0, copyWidth2, copyHeight2);

        // 軸ラベルのフォント色を透明に変更して、以降、再表示されても見えないようにする
        chart.options.scales.yAxes[0].ticks.fontColor = 'rgba(0,0,0,0)';
        chart.options.scales.yAxes[1].ticks.fontColor = 'rgba(0,0,0,0)';
        chart.update();
        // 最初に描画されたグラフのY軸ラベル部分をクリアする
        //this.ctxChart.clearRect(0, 0, yAxisStyleWidth1, yAxisStyleHeight1);
        this.cvsChart.getContext('2d').clearRect(0, 0, yAxisStyleWidth1, yAxisStyleHeight1);
        this.cvsChart.getContext('2d').clearRect(this.cvsChart.style.Width - yAxisStyleWidth2, 0, yAxisStyleWidth2, yAxisStyleHeight2);

        // スクロールする
        if (this.initialScrollRate > 0) {
            var wrapper = document.getElementById(this.wrapperId).getElementsByTagName("div")[0];
            var cliWidth = wrapper.clientWidth;     // 描画時にdivがdisplay:noneになっているとこの値は0
            var scrWidth = wrapper.scrollWidth;     // 描画時にdivがdisplay:noneになっているとこの値は0
            //console.log("clientWidth=" + cliWidth);
            //console.log("scrollWidth=" + scrWidth);
            //console.log("scrollRate=" + this.initialScrollRate);
            // 描画時にdivがdisplay:noneになっていると、そもそもスクロールが効かない
            wrapper.scrollLeft = cliWidth == 0 ? 100 : Math.min(Math.max(scrWidth * this.initialScrollRate - cliWidth * 0.667, 0.0), scrWidth - cliWidth);
            //console.log("wrapper.scrollLeft=" + wrapper.scrollLeft);
            this.initialScrollRate = 0;
        }

        //console.log('LEAVE: copyYAxisImage')
    };

    this.wrapperId = wrapperId;
    this.myChart = null;
    //this.eventListenerAdded = false;

    this.render = (barWidth, scrollRate, chartJson) => {
        //console.log('ENTER: render(' + barWidth + ',' + scrollRate + ', ...)');
        if (this.myChart) {
            //console.log('destroy myChart');
            this.myChart.destroy();
            this.myChart = null;
        }
        //console.log("json=" + chartJson);
        var chartData = JSON.parse(chartJson);

        this.currentBarWidth = barWidth;
        this.initialScrollRate = scrollRate;
        this.chartWidth = chartData.data.labels.length * (this.xAxisStepSize + this.currentBarWidth * this.xAxisWidthRate);

        // グラフ描画用 canvas
        var canvases = document.getElementById(this.wrapperId).getElementsByTagName("canvas");
        //console.log(canvases);
        this.cvsChart = canvases[0];
        this.ctxChart = this.cvsChart.getContext('2d');
        //console.log(this.ctxChart);

        // グラフ描画用canvasのstyle.width(すなわち全データを描画するのに必要なサイズ)に上記の幅を設定
        this.cvsChart.style.width = this.chartWidth + "px";
        this.cvsChart.style.height = this.chartHeight + "px";
        // グラフ用 canvas サイズの変更（style.* が変更になったら、 * も同じ値に変えておく必要があるらしい）
        this.cvsChart.width = this.chartWidth; // * window.devicePixelRatio;
        this.cvsChart.height = this.chartHeight; // * window.devicePixelRatio;

        //console.log('this.cvsChart.style.width=' + this.cvsChart.style.width);
        //console.log('this.cvsChart.style.height=' + this.cvsChart.style.height);
        //console.log('this.cvsChart.width=' + this.cvsChart.width);
        //console.log('this.cvsChart.height=' + this.cvsChart.height);
        //console.log('--------------------------');

        // Y軸コピー用 canvas
        this.cvsYAxis1 = canvases[1];
        this.ctxYAxis1 = this.cvsYAxis1.getContext('2d');
        this.cvsYAxis1.style.width = 0;
        this.cvsYAxis2 = canvases[2];
        this.ctxYAxis2 = this.cvsYAxis2.getContext('2d');
        this.cvsYAxis2.style.width = 0;

        chartData.options.responsive = false;
        //chartData.options.scales.yAxes[1].ticks.callback = function (value, index, values) { return Number(value).toFixed(2); };
        //chartData.plugins = [{ afterRender: this.copyYAxisImage }]
        chartData.plugins = [{ afterDraw: this.copyYAxisImage }]
        this.copyYAxisCalled = false;

        function nameFilter(name) {
            //console.log('filter: ' + name);
            return name && name != "" && name != 'RtBaseline';
        }

        // RtBaseline および名前無しを凡例に表示しない
        chartData.options.legend.labels = {
            filter: function (item) { return nameFilter(item.text); }
        }

        // RtBaseline および名前無しをツールチップに表示しない
        chartData.options.tooltips.filter = function (item, data) {
            var dataset = data.datasets[item.datasetIndex];
            return dataset && nameFilter(dataset.label) && dataset.data[item.index] != undefined;
        }

        // ツールチップの表示順
        chartData.options.tooltips.itemSort = function (item1, item2, data) {
            var order1 = data.datasets[item1.datasetIndex].dispOrder;
            var order2 = data.datasets[item2.datasetIndex].dispOrder;
            return (order1 < order2) ? -1 : (order1 > order2) ? 1 : 0;
        }

        // ツールチップラベルの編集
        //chartData.options.tooltips.callbacks = {
        //    label: function (item, data) {
        //        var dataset = data.datasets[item.datasetIndex];
        //        return dataset ? dataset.label.trim() : "";
        //    }
        //};

        // ツールチップが表示されているか
        //var tooltipShown = false;
        //chartData.options.tooltips.custom = function (tooltip) {
        //    console.log(tooltip.opacity);
        //    tooltipShown = tooltip.opacity > 0;
        //};

        var chartWidth = this.chartWidth;
        var chartHeight = this.chartHeight;
        function _customFixedOrHighest(elements, eventPosition, isHighest) {
            // 上から120pxの高さのところを返す。
            // core.tooltip.jsを参照。
            if (!elements.length) {
                return false;
            }
            // X軸ラベル部分ならツールチップを表示しない
            if (eventPosition.y >= 360) {
                return false;
            }

            //console.log(tooltip);
            //console.log(elements);

            var i, len;
            var count = 0;
            var xpos = 0;
            var ymax = 10000;
            for (i = 0, len = elements.length; i < len; ++i) {
                var label = elements[i]._chart.config.data.datasets[elements[i]._datasetIndex].label;
                if (label && label.trim()) {
                    var el = elements[i];
                    if (el && el.hasValue()) {
                        //console.log(label);
                        var pos = el.tooltipPosition();
                        if (pos.x) xpos = pos.x;
                        if (pos.y && pos.y > 0) {
                            ++count;
                            if (pos.y < ymax) ymax = pos.y;
                        }
                    }
                }
            }
            if (xpos > 0) {
                var ypos = (count + 1) * 14 + 16;
                //console.log("ChartWidth:" + chartWidth + ", xpos:" + xpos + ", ypos:" + ypos + ", count:" + count);
                return {
                    x: Math.round(xpos - (xpos >= chartWidth / 2 ? 8 : -8)),
                    y: Math.round(isHighest && ymax > ypos ? ymax : ypos)
                };
            } else {
                return false;
            }
        }

        // ツールチップの表示位置のカスタム関数を登録
        Chart.Tooltip.positioners.customFixed = function (elements, eventPosition) {
            return _customFixedOrHighest(elements, eventPosition, false);
        }

        // ツールチップの表示位置のカスタム関数を登録
        Chart.Tooltip.positioners.customHighest = function (elements, eventPosition) {
            return _customFixedOrHighest(elements, eventPosition, true);
        }

        function _customAverage(elements, startIdx, length) {
            // startIdx から length 個の要素の平均。length <= 0 なら elements[elements.length+length-1]要素まで。
            // core.tooltip.jsを参照。
            if (!elements.length || startIdx < 0 || startIdx >= elements.length) {
                console.log("elements.length: " + elements.length + ", startIdx: " + startIdx);
                return false;
            }
            var endIdx = startIdx + (length > 0 ? length : elements.length + length);
            if (endIdx < 0 || endIdx >= elements.length) {
                console.log("elements.length: " + elements.length + ", startIdx: " + startIdx + ", length: " + length);
                return false;
            }

            var i;
            var count = 0;
            var xpos = 0;
            var ypos = 0;
            for (i = startIdx; i < endIdx; ++i) {
                var el = elements[i];
                if (el && el.hasValue()) {
                    var pos = el.tooltipPosition();
                    if (pos.y && pos.y > 0) {
                        xpos += pos.x;
                        ypos += pos.y;
                        ++count;
                    }
                }
            }
            if (xpos > 0) {
                //console.log("ChartWidth:" + chartWidth + ", xpos:" + xpos + ", ypos:" + ypos);
                xpos /= count;
                ypos /= count;
                return {
                    x: Math.round(xpos - (xpos >= chartWidth / 2 ? 8 : -8)),
                    y: Math.round(ypos)
                };
            } else {
                return false;
            }
        }

        // ツールチップの表示位置のカスタム関数を登録
        Chart.Tooltip.positioners.customAverage = function (elements, eventPosition) {
            var _options = this._options;
            //console.log(this);
            //console.log("customAverage: _startIdx=" + _options._startIdx + ", _endIdx=" + _options._endIdx);
            return _customAverage(elements, _options._startIdx, _options._endIdx);
        }

        //console.log('new Chart');

        // クリック時のみ、反応する
        //chartData.options.events = ["click"];

        this.myChart = new Chart(this.ctxChart, chartData);

        //console.log(window.innerWidth);
        //console.log(window.innerHeight);
        //console.log(chartData.options._tooltipsHide);

        //var myChart = this.myChart;
        //var cvsChart = this.cvsChart;
        //cvsChart._tooltipsHide = myChart.options._tooltipsHide;
        //function mousemoveListener(e) {
        //    //console.log(this._tooltipsHide);
        //    if (this._tooltipsHide && window.innerWidth > 640 && window.innerHeight > 640) {
        //        var ptrCvsY = e.clientY - cvsChart.getBoundingClientRect().top;
        //        //console.log(ptrCvsY);
        //        myChart.options.tooltips.enabled = (ptrCvsY < 360);
        //    }
        //}
        //function mouseclickListener(e) {
        //    if (this._tooltipsHide) {
        //        //console.log(cvsChart);
        //        //console.log(cvsChart.getBoundingClientRect().top);
        //        //console.log(e);
        //        var ptrCvsY = e.clientY - cvsChart.getBoundingClientRect().top;
        //        //console.log(ptrCvsY);
        //        if (ptrCvsY >= 360) {
        //            //myChart.options.tooltips.enabled = false;
        //            myChart.update();
        //        }
        //    }
        //}
        //if (!this.eventListenerAdded) {
        //    this.eventListenerAdded = true;
        //    cvsChart.addEventListener("mousemove", mousemoveListener);
        //    cvsChart.addEventListener("click", mouseclickListener);
        //    console.log("eventListener added");
        //}
    };

    //console.log('LEAVE: ChartDrawer()')
    //return this;
}

var chartDrawers = {};

//This function is not currently in use.
//It wat left as a sample to call a server-side instance method from JavaScript.
//see: https://docs.microsoft.com/ja-jp/aspnet/core/blazor/call-dotnet-from-javascript?view=aspnetcore-3.1
window.renderChart0 = (dotnetHelper, dataId, predDayPos, realStopDate, endDate, bManualPred, bAnimation) => {
    //console.log(pos);
    if (window.myChart) {
        //console.log('destroy myChart');
        window.myChart.destroy();
    }
    dotnetHelper.invokeMethodAsync('GetChartData', dataId, predDayPos, realStopDate, endDate, bManualPred, bAnimation).then((json) => {
        //console.log("json="+json);
        var ctx = document.getElementById('myChart_0');
        window.myChart = new Chart(ctx, JSON.parse(json));
    });
    dotnetHelper.dispose();
};

/**
 * render chart function.
 * @@param {String} chartJson JSON string that is passed to Chart() function after being parsed
 */
window.renderChart2 = (wrapperId, barWidth, scrollRate, chartJson) => {
    //console.log('==========================');
    //console.log('wrapperId=' + wrapperId);
    drawer = window.chartDrawers[wrapperId];
    if (!drawer) {
        drawer = new ChartDrawer(wrapperId);
        window.chartDrawers[wrapperId] = drawer;
    }
    drawer.render(barWidth, scrollRate, chartJson);
};