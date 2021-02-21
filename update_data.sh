#! /bin/bash

#cd $(dirname $0)
BINDIR=$(dirname $0)

. ${BINDIR}/debug_util.sh

if [[ "$1" == -* ]]; then
    [[ "$1" == -*l* ]] && LOADFLAG=$1
    shift
fi
[ "$1" ] && BLAZOR_REMOTE_HOST2="$1"
[ "$2" ] && BLAZOR_REMOTE_HOST="$2"

WORKDIR=Data/work
CSVDIR=Data/csv

copy_files() {
    if [ "$BLAZOR_REMOTE_HOST" ]; then
        RUN_CMD -m "scp $CSVDIR/*.csv ${BLAZOR_REMOTE_HOST}:dotnet/ChartBlazorApp/$CSVDIR"
    fi
    if [ "$BLAZOR_REMOTE_HOST2" ]; then
        RUN_CMD -m "scp $CSVDIR/*.csv ${BLAZOR_REMOTE_HOST2}:dotnet/ChartBlazorApp/$CSVDIR"
    fi
}

normalizeDate() {
    date -d "$(echo $1 | cut -d, -f1)" '+%Y%m%d'
}

dayBeforeYesterday() {
    date -d "2 days ago" '+%Y%m%d'
}

findLastDate() {
    grep ',$1,' $2 | tail -n 1 | cut -d, -f1
}

findNonZeroTail() {
    local pref="$1"
    local file="$2"
    local lastLine=""
    local lastTotal=""
    local mytotal=""
    for line in $(grep ",${pref}," $file | tail -n 10 | sort -r); do
        mytotal=$(echo $line | cut -d, -f4)
        if [ "$lastTotal" -a "$lastTotal" != "$mytotal" ]; then
            break
        elif [ "$lastTotal" == "" ]; then
            lastTotal=$mytotal
        fi
        lastLine="$line"
    done
    echo "$lastLine"
}

addExtraTotal() {
    paramFile=$1
    outFile=$2
    VAR_PRINT paramFile
    VAR_PRINT outFile
    for pref in $(grep '^20' $paramFile | cut -d, -f3 | sort -u); do
        echo "== $pref =="
        #tailLine=$(findNonZeroTail ${pref} $outFile)
        tailLine=$(grep ",${pref}," $outFile | tail -n 1)
        tailDate=$(normalizeDate "$tailLine")
        tailVal=$(echo $tailLine | cut -d, -f4)
        VAR_PRINT -f -1 tailLine
        VAR_PRINT tailDate
        VAR_PRINT tailVal
        grep -v '^#' $paramFile | grep "${pref}" | while read line; do
            normDate=$(normalizeDate "$line")
            newVal=$(echo $line | cut -d, -f4)
            if [[ $normDate > $tailDate ]]; then
                tailVal=$((tailVal + $newVal))
                addLine="$(echo $line | cut -d, -f1-3),$tailVal"
                VAR_PRINT -f -1 addLine
                echo "$addLine" >> $outFile
            elif [[ "$line" =~ ,USE ]]; then
                [ $normDate -ge $tailDate ] && tailVal=$newVal
                addLine="$(echo $line | cut -d, -f1-4)"
                VAR_PRINT -f -1 addLine
                echo "$addLine" >> $outFile
            fi
        done
    done
}

repairCsv_rb() {
cat <<EOS
    require "date";
    repairBegin = false;
    while line = gets;
        line.strip!;
        if repairBegin || line == "2021/2/10,1691";
            repairBegin = true;
            items = line.split(/,/);
            items[0] = (DateTime.parse(items[0])+1).strftime("%Y/%1m/%1d");
            line = items.join(",");
        end;
        puts line;
    end
EOS
}

downloadCsv() {
    . $BINDIR/debug_util_inc_indent.sh
    local url=$1
    local file=$2
    if [ "$LOADFLAG" ] || [ ! -f ${file} ]; then
        YELLOW_PRINT "download $file"
        local currDt="2020/1/1"
        local dtPos=1
        [[ "$(basename $file)" == tokyo* ]] && dtPos=5
        [ -f ${file} ] && currDt="$(tail -n 1 $file | cut -d, -f$dtPos)"
        currDt=$(date -d "$currDt" '+%s')
        VAR_PRINT currDt
        RUN_CMD -m "curl ${url} -o ${file}.download 2>/dev/null"
        if [[ "$(basename $file)" == pcr_posi* ]]; then
            RUN_CMD -fm "ruby -e '$(repairCsv_rb)' ${file}.download > ${file}.tmp"
        elif [[ "$(basename $file)" == tokyo* ]]; then
            RUN_CMD -fm "grep -Ev '^\"\s*$' ${file}.download > ${file}.tmp"
        else
            RUN_CMD -fm "cp ${file}.download ${file}.tmp"
        fi
        local lastDt="$(tail -n 1 ${file}.tmp | cut -d, -f$dtPos)"
        local diffNum=1
        [ -f ${file} ] && diffNum="$(diff -q ${file}.tmp ${file} | wc -l)"
        VAR_PRINT -f lastDt
        VAR_PRINT -f diffNum
        if [[ "$lastDt" =~ ^[0-9]+[/-][0-9]+[/-][0-9]+$ ]]; then
            lastDt=$(date -d "$lastDt" '+%s')
            VAR_PRINT lastDt
            if [ $lastDt -gt $currDt ] || [ $lastDt -eq $currDt -a "$diffNum" != "0" ]; then
                RUN_CMD -fm "mv ${file}.tmp ${file}"
            fi
        fi
        RUN_CMD -fm "rm -f ${file}.tmp"
    else
        YELLOW_PRINT "${file} exists"
    fi
    . $BINDIR/debug_util_rev_indent.sh
}

makeTokyoPosiCnt_rb() {
cat <<EOS
    nums = {};

    while line = gets;
      items = line.strip.split(",");
      dt = items[4].gsub(/-/, "/");
      if dt =~ /20\d+\/\d+\/\d+/;
        if nums[dt];
          nums[dt] = nums[dt] + 1;
        else;
          nums[dt] = 1;
        end;
      end;
    end;

    total = 0;
    nums.keys.sort.each {|dt| total += nums[dt]; puts "#{dt},東京都,Tokyo,#{total}" }
EOS
}

makePrefCsv_rb() {
cat <<EOS
posi_total = 0;
test_total = 0;
while line = gets;
    items = line.strip.split(/,/);
    posi_total += items[1].to_i;
    test_total += items[2].to_i;
    puts "#{items[0]},全国,All,#{posi_total},#{test_total}";
end
EOS
}

PREF_PARAM_FILE=Data/pref_params.txt
PREF_WORK_FILE=$WORKDIR/prefectures_ex.csv
PREF_TARGET_FILE=$CSVDIR/prefectures_ex.csv
POSI_DAILY_FILE=pcr_positive_daily.csv
POSI_DAILY_WORK=$WORKDIR/$POSI_DAILY_FILE
TEST_DAILY_FILE=pcr_tested_daily.csv
TEST_DAILY_WORK=$WORKDIR/$TEST_DAILY_FILE
ALL_PCR_DAILY_WORK=$WORKDIR/all_pcr_daily.csv
PREF_SRCFILE=Data/covid19/data/prefectures.csv

DEATH_TOTAL_FILE=death_total.csv
DEATH_TOTAL_WORK=$WORKDIR/$DEATH_TOTAL_FILE
SEVERE_DAILY_FILE=severe_daily.csv
SEVERE_DAILY_WORK=$WORKDIR/$SEVERE_DAILY_FILE
DEATH_SERIOUS_PARAM=Data/death_and_serious.txt
DEATH_SERIOUS_TARGET=$CSVDIR/death_and_serious.csv
CHART_SCALES_PARAM=Data/other_chart_scales.txt
MULTI_STEP_EXPECT_PARAM=Data/multi_step_expect_params.txt

INFECT_AGES_FILE=infect_by_ages.txt

# 処理開始
mkdir -p $CSVDIR $WORKDIR

# 先頭日付
FIRST_DATE='2020.0?5.18'

# ヘッダー部
echo '#start_of_header' > $PREF_WORK_FILE
[ -f reload_magic.txt ] && RUN_CMD -fm "cat reload_magic.txt >> $PREF_WORK_FILE"
RUN_CMD -fm "sed -n '1,/#end_of_header/ p' $PREF_PARAM_FILE >> $PREF_WORK_FILE"

# 全国データのダウンロード
RUN_CMD -fm "downloadCsv https://www.mhlw.go.jp/content/$POSI_DAILY_FILE ${POSI_DAILY_WORK}"
RUN_CMD -fm "downloadCsv https://www.mhlw.go.jp/content/$TEST_DAILY_FILE ${TEST_DAILY_WORK}"

# 全国陽性者数
RUN_CMD -fm "sed -nr '/^${FIRST_DATE},/,$ p' $POSI_DAILY_WORK | ruby -ne 'puts \$_.strip' > ${POSI_DAILY_WORK}.tmp"
RUN_CMD -fm "sed -nr '/^${FIRST_DATE},/,$ p' $TEST_DAILY_WORK | ruby -ne 'puts \$_.strip' | cut -d, -f2 > ${TEST_DAILY_WORK}.tmp"
RUN_CMD -fm "paste -d, ${POSI_DAILY_WORK}.tmp ${TEST_DAILY_WORK}.tmp > $ALL_PCR_DAILY_WORK"
RUN_CMD -fm "ruby -e '$(makePrefCsv_rb)' $ALL_PCR_DAILY_WORK >> ${PREF_WORK_FILE}"
#RUN_CMD -m "rm -f ${POSI_DAILY_WORK}.tmp ${TEST_DAILY_WORK}.tmp"

# 都道府県陽性者数と検査数
PREF_WORKDIR=$WORKDIR/pref
mkdir -p $PREF_WORKDIR
PREF_POSI_TEST_WORK=$WORKDIR/pref_posi_test_work.csv
for x in $(ls -1 Data/mhlw_pref/*.txt); do
    pref_work_file=$PREF_WORKDIR/$(basename $x)
    if [[ ! -f $pref_work_file || $x -nt $pref_work_file ]]; then
        RUN_CMD -f -m "$BINDIR/make_pref_data.sh $x > $pref_work_file"
    fi
done
RUN_CMD -f "cat ${PREF_WORKDIR}/*.txt > $PREF_POSI_TEST_WORK"

# 東京都データのダウンロードと集計
TOKYO_POSI_WORK=$WORKDIR/tokyo_covid19_patients.csv
TOKYO_DL_FILE=${TOKYO_POSI_WORK}.download
RUN_CMD -fm "downloadCsv https://stopcovid19.metro.tokyo.lg.jp/data/130001_tokyo_covid19_patients.csv $TOKYO_POSI_WORK"
RUN_CMD -fm "ruby -e '$(makeTokyoPosiCnt_rb)' $TOKYO_POSI_WORK | sed -nr '/^${FIRST_DATE},/,$ p' > ${TOKYO_POSI_WORK}.tmp"
RUN_CMD -fm "paste -d, ${TOKYO_POSI_WORK}.tmp <(grep ',Tokyo,' $PREF_POSI_TEST_WORK | cut -d, -f5) >> ${PREF_WORK_FILE}"

# 東京以外の陽性者数と検査数
RUN_CMD -fm "grep -v ',Tokyo,' $PREF_POSI_TEST_WORK >> ${PREF_WORK_FILE}"
echo "#end_of_mhlw_pref" >> ${PREF_WORK_FILE}

# 追加陽性者数
#RUN_CMD -f -m "addExtraTotal $PREF_PARAM_FILE $PREF_WORK_FILE"
RUN_CMD -fm "sed -n '/#overwrite/,/#append/ p' $PREF_PARAM_FILE | grep -v '^#' | \
               sed 's/$/,OVERWRITE/' >> $PREF_WORK_FILE"
RUN_CMD -fm "sed -n '/#append/,$ p' $PREF_PARAM_FILE | grep -v '^#' | \
               sed 's/$/,APPEND/' >> $PREF_WORK_FILE"
RUN_CMD -fm "sed 's/[府県],/,/' $PREF_WORK_FILE | sed 's/東京都,/東京,/' > $PREF_TARGET_FILE"

# 重症者&死亡者&改善率
RUN_CMD -fm "downloadCsv https://www.mhlw.go.jp/content/$DEATH_TOTAL_FILE ${DEATH_TOTAL_WORK}"
RUN_CMD -fm "downloadCsv https://www.mhlw.go.jp/content/$SEVERE_DAILY_FILE ${SEVERE_DAILY_WORK}"
tailFromDate() {
    RUN_CMD -f "sed -ne '/2020\\/6\\/1/,$ p' $1 | ruby -ne 'puts \$_.strip'"
}
RUN_CMD -fm "tailFromDate $DEATH_TOTAL_WORK > ${DEATH_TOTAL_WORK}.tmp"
RUN_CMD -fm "tailFromDate $SEVERE_DAILY_WORK | cut -d, -f2 > ${SEVERE_DAILY_WORK}.tmp"

cp $DEATH_SERIOUS_PARAM $DEATH_SERIOUS_TARGET
RUN_CMD -fm "paste -d, ${DEATH_TOTAL_WORK}.tmp ${SEVERE_DAILY_WORK}.tmp >> $DEATH_SERIOUS_TARGET"
RUN_CMD -m "rm -f ${DEATH_TOTAL_WORK}.tmp ${SEVERE_DAILY_WORK}.tmp"

for x in Data/*_rate.txt $CHART_SCALES_PARAM $MULTI_STEP_EXPECT_PARAM; do
    RUN_CMD -m "cp -p $x $CSVDIR/$(basename ${x/.txt/.csv})"
done

# 年代別陽性者数
RUN_CMD -fm "cp -p Data/$INFECT_AGES_FILE $CSVDIR/${INFECT_AGES_FILE/.txt/.csv}"

# 作成したファイルをリモートにコピー
RUN_CMD -fm "copy_files"
