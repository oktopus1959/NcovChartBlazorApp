# NcovChartBlazorApp
新型コロナウイルスの陽性者数の推移をグラフ表示するWebアプリを、 ASP.NET Core Blazor と
Chart.js を使って実装したものです。もともと、Web アプリの勉強用に開発を始めたものなので、
いろいろと不備な点もありますが、Blazor や Chart.js の使い方のサンプルにもなるかと思い、
公開することにしました。

## Web サイト
このアプリは、下記サイトにて実際に稼動しています。

https://ncov.oktopus59.net/

陽性者数の他、 日本における COVID-19 重症者や死亡者数の予測も下記ページで行っています。

https://ncov.oktopus59.net/forecast/

## データソース
- 日本全国の陽性者数、COVID-19 重症者数・死亡者数については、[厚生労働省のオープンデータ](https://www.mhlw.go.jp/stf/covid-19/open-data.html)
- 各都道府県の陽性者数については、厚生労働省の「[発生状況](https://www.mhlw.go.jp/stf/seisakunitsuite/bunya/0000121431_00086.html)」ページからたどれる
【別紙１】「各都道府県の検査陽性者の状況（空港・海港検疫、チャーター便案件を除く国内事例）」PDF資料（[例](https://www.mhlw.go.jp/content/10906000/000747891.pdf)）。

## ビルド方法など
ビルドには .NET Core 3.1 が必要になります。

.NET Core のインストールやアプリのビルド方法などについては、下記 Qiita ページを参照してください。

- [ASP.NET Core Blazor と Chart.js で入門する Web アプリ作成 － 導入編：サンプルを動かす](https://qiita.com/okatako/items/171f05dfc36d6b27769d)

なお Visual Studio 2019 用の Solution ファイルも入れてあります。

## データ作成方法

### 陽性者数データ
プロジェクトフォルダで update_data.sh を実行します。

```sh
$ ./update_data.sh --load
```

`--load` オプションを付けると、厚労省のオープンデータ（全国陽性者数）をダウンロードしてからデータの作成を行います。

スクリプトの実行には ruby が必要です。

### 都道府県別データ
新しい日付の都道府県別データを作成するには、以下の手順を実行します。
1. 厚労省サイトで「国内における都道府県別のPCR検査陽性者数」PDFをダウンロードする
2. 上記PDFに記述されている日付を確認し、その日付から yyyymmdd.txt というファイル名を生成
3. shell 上で `python3 mhlw_pref_pdf_to_text.py PDF FILE-NAME > Data/mhlw_pref/yyyymmdd.txt` を実行

実行には camelot という python モジュールが必要となります。
作成されたデータは、都道府県別のPCR検査数、陽性率を抽出するために利用することも可能です。
抽出方法については `make_pref_data.rb` を参考にしてください。

### 年齢階級別陽性者数データ
重症者数・死亡者数の予測には、 Data/infect_by_ages.txt が必要です。
これは、毎週木曜日あたりに厚労省から発表される「年齢階級別陽性者数」を見て手作業で更新しています。

## 実行方法
簡単に実行したい場合は、 dotnet run を使ってください。

```sh
$ dotnet run --urls "http://0.0.0.0:5000;https://0.0.0.0:5001"
```

上記のように `--urls "http://0.0.0.0:5000;https://0.0.0.0:5001"` オプションを付けて起動すると
localhost 以外からもアクセスできるようになります。

## ライセンスなど
プログラムのソースコードについては完全フリーとします。商用含め、いかなる用途に使用していただいても構いません。
ただし、使用した結果について、作者は一切の責を免れるものとします。

Data/ 配下の

- death_rate.txt
- recover_rate.txt
- serious_rate.txt

については、公開する著作物の一部としてこれらを利用される際は
[Qiitaアカウント(@okatako)](https://qiita.com/okatako)
または [Twitterアカウント(@oktopus59)](https://twitter.com/oktopus59) までご一報ください。

以上

