#! /bin/bash

mkdir -p log run
if [ -e run/pid ]; then
    echo "Progrma already executed: $(cat run/pid)" >&2
    exit 1
fi
CMD='bin/Release/netcoreapp3.1/ChartBlazorApp --urls "http://0.0.0.0:5000;https://0.0.0.0:5001" > log/ChartBlazorApp.log 2>&1 &'
echo "$CMD"
eval "$CMD"
pid="$!"
echo $pid > run/pid
echo "PID=$pid" >&2
