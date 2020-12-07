#! /bin/bash

cd $(dirname $0)

. ./debug_util.sh

if [[ "$1" == -* ]]; then
    [[ "$1" == -*l* ]] && LOADFLAG=$1
    shift
fi
[ "$1" ] && BLAZOR_REMOTE_HOST="$1"

WORKDIR=Data/work
CSVDIR=Data/csv

copy_files() {
    [ "$BLAZOR_REMOTE_HOST" ] && \
        RUN_CMD -m "scp -p $CSVDIR/*.csv ${BLAZOR_REMOTE_HOST}:dotnet/ChartBlazorApp/$CSVDIR"
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
            if [[ $(normalizeDate "$line") > $tailDate ]]; then
                tailVal=$((tailVal + $(echo $line | cut -d, -f4) ))
                addLine="$(echo $line | cut -d, -f1-3),$tailVal"
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
PREF_TARGET_FILE=$CSVDIR/prefectures_ex.csv
PCR_DAILY_FILE=pcr_positive_daily.csv
PCR_DAILY_WORK=$WORKDIR/$PCR_DAILY_FILE
PREF_SRCFILE=Data/covid19/data/prefectures.csv

DEATH_TOTAL_FILE=death_total.csv
DEATH_TOTAL_WORK=$WORKDIR/$DEATH_TOTAL_FILE
SEVERE_DAILY_FILE=severe_daily.csv
SEVERE_DAILY_WORK=$WORKDIR/$SEVERE_DAILY_FILE
DEATH_SERIOUS_PARAM=Data/death_and_serious.txt
DEATH_SERIOUS_TARGET=$CSVDIR/death_and_serious.csv

INFECT_AGES_FILE=infect_by_ages.txt

mkdir -p $CSVDIR $WORKDIR

# 先頭日付
FIRST_DATE='2020.0?5.18'

# ヘッダー部
grep '^#' $PREF_PARAM_FILE > $PREF_TARGET_FILE

# 全国
RUN_CMD -f "downloadCsv https://www.mhlw.go.jp/content/$PCR_DAILY_FILE ${PCR_DAILY_WORK}"

ruby_script() {
cat <<EOS
total = 0;
while line = gets;
    items = line.strip.split(/,/);
    total += items[1].to_i;
    puts "#{items[0]},全国,All,#{total}";
end
EOS
}

RUN_CMD -f "sed -nr '/^${FIRST_DATE},/,$ p' $PCR_DAILY_WORK| ruby -e '$(ruby_script)' >> ${PREF_TARGET_FILE}"

# 都道府県
[ "$LOADFLAG" ] && RUN_CMD -m "(cd Data/covid19; git pull)"
RUN_CMD -f -m "sed -nr '/^${FIRST_DATE},/,$ p' $PREF_SRCFILE | \
            sed -r 's/^([0-9]+),([0-9]+),([0-9]+),/\1\/\2\/\3,/' | \
            cut -d, -f1-4 >> ${PREF_TARGET_FILE}"

# 追加陽性者数
RUN_CMD -f -m "addExtraTotal $PREF_PARAM_FILE $PREF_TARGET_FILE"

# 重症者&死亡者&改善率
RUN_CMD -f "downloadCsv https://www.mhlw.go.jp/content/$DEATH_TOTAL_FILE ${DEATH_TOTAL_WORK}"
RUN_CMD -f "downloadCsv https://www.mhlw.go.jp/content/$SEVERE_DAILY_FILE ${SEVERE_DAILY_WORK}"
tailFromDate() {
    RUN_CMD -f "sed -ne '/2020\\/6\\/1/,$ p' $1 | ruby -ne 'puts \$_.strip'"
}
RUN_CMD -f "tailFromDate $DEATH_TOTAL_WORK > ${DEATH_TOTAL_WORK}.tmp2"
RUN_CMD -f "tailFromDate $SEVERE_DAILY_WORK | cut -d, -f2 > ${SEVERE_DAILY_WORK}.tmp2"

cp $DEATH_SERIOUS_PARAM $DEATH_SERIOUS_TARGET
RUN_CMD -f -m "paste -d, ${DEATH_TOTAL_WORK}.tmp2 ${SEVERE_DAILY_WORK}.tmp2 >> $DEATH_SERIOUS_TARGET"

for x in Data/*_rate.txt; do
    RUN_CMD -m "cp -p $x $CSVDIR/$(basename ${x/.txt/.csv})"
done

# 年代別陽性者数
RUN_CMD -f -m "cp -p Data/$INFECT_AGES_FILE $CSVDIR/${INFECT_AGES_FILE/.txt/.csv}"

# 作成したファイルをリモートにコピー
RUN_CMD -f "copy_files"
