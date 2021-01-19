#! /bin/bash

BINDIR=$(dirname $0)

txtfile=$1
dt=$(basename ${txtfile%.*})
if [[ "$dt" =~ ^202[0-9][01][0-9][0-3][0-9]$ ]]; then
    dtstr=$(date --date=$dt '+%Y/%m/%d')
    if [ $? -eq 0 ]; then
        cmd="cat $txtfile | ruby $BINDIR/make_pref_data.rb $dtstr"
        #echo "$cmd" >&2
        eval "$cmd"
        #wc -l $outfile
    fi
else
    echo "Invalid date string: '$dtstr'" >&2
    exit
fi
