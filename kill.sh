#! /bin/bash

if [ ! -e run/pid ]; then
    echo "Progrma notrunning" >&2
    exit 1
fi

pid=$(cat run/pid)
kill $pid
rm run/pid

echo "PID=${pid} killed" >&2
