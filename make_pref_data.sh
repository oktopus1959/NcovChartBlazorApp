#! /bin/bash

if [[ "$1" =~ 202[0-9]/[01]?[0-9/[0-3]?[0-9] ]]; then
    outfile="Data/pref/$(date --date=$1 '+%Y%m%d').txt"
    if [ $? -eq 0 ]; then
        cmd="ruby ./make_pref_data.rb $1 > $outfile"
        echo "$cmd"
        eval "$cmd"
        wc -l $outfile
    fi
else
    echo "Invalid date string: '$1'" >&2
    exit
fi
