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

downloadCsv() {
    local url=$1
    local file=$2
    if [ "$LOADFLAG" ]; then
        local currDt="2020/1/1"
        [ -f ${file} ] && currDt="$(tail -n 1 ${file} | cut -d, -f1)"
        currDt=$(date -d "$currDt" '+%s')
        VAR_PRINT currDt
        RUN_CMD -m "curl ${url} -o ${file}.tmp"
        local lastDt="$(tail -n 1 ${file}.tmp | cut -d, -f1)"
        if [[ "$lastDt" =~ ^[0-9]+/[0-9]+/[0-9]+$ ]]; then
            lastDt=$(date -d "$lastDt" '+%s')
            VAR_PRINT lastDt
            if [ $lastDt -gt $currDt ]; then
                RUN_CMD -m "mv ${file}.tmp ${file}"
            fi
        fi
    fi
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
FOURSTEP_EXPECT_PARAM=Data/4step_expect_params.txt

INFECT_AGES_FILE=infect_by_ages.txt

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

ruby_script() {
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

# 全国陽性者数
RUN_CMD -fm "sed -nr '/^${FIRST_DATE},/,$ s/\r//p' $POSI_DAILY_WORK > ${POSI_DAILY_WORK}.tmp"
RUN_CMD -fm "sed -nr '/^${FIRST_DATE},/,$ s/\r//p' $TEST_DAILY_WORK | cut -d, -f2 > ${TEST_DAILY_WORK}.tmp"
RUN_CMD -fm "paste -d, ${POSI_DAILY_WORK}.tmp ${TEST_DAILY_WORK}.tmp > $ALL_PCR_DAILY_WORK"
RUN_CMD -fm "ruby -e '$(ruby_script)' $ALL_PCR_DAILY_WORK >> ${PREF_WORK_FILE}"

# 都道府県陽性者数
PREF_WORKDIR=$WORKDIR/pref
mkdir -p $PREF_WORKDIR
for x in $(ls -1 Data/mhlw_pref/*.txt); do
    pref_work_file=$PREF_WORKDIR/$(basename $x)
    if [[ ! -f $pref_work_file || $x -nt $pref_work_file ]]; then
        RUN_CMD -f -m "$BINDIR/make_pref_data.sh $x > $pref_work_file"
    fi
done
RUN_CMD "cat ${PREF_WORKDIR}/*.txt >> ${PREF_WORK_FILE}"
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
RUN_CMD -fm "tailFromDate $DEATH_TOTAL_WORK > ${DEATH_TOTAL_WORK}.tmp2"
RUN_CMD -fm "tailFromDate $SEVERE_DAILY_WORK | cut -d, -f2 > ${SEVERE_DAILY_WORK}.tmp2"

cp $DEATH_SERIOUS_PARAM $DEATH_SERIOUS_TARGET
RUN_CMD -fm "paste -d, ${DEATH_TOTAL_WORK}.tmp2 ${SEVERE_DAILY_WORK}.tmp2 >> $DEATH_SERIOUS_TARGET"

for x in Data/*_rate.txt $CHART_SCALES_PARAM $FOURSTEP_EXPECT_PARAM; do
    RUN_CMD -m "cp -p $x $CSVDIR/$(basename ${x/.txt/.csv})"
done

# 年代別陽性者数
RUN_CMD -fm "cp -p Data/$INFECT_AGES_FILE $CSVDIR/${INFECT_AGES_FILE/.txt/.csv}"

# 作成したファイルをリモートにコピー
RUN_CMD -fm "copy_files"
