#! /bin/bash

ip=`hostname -I | grep -Po '10\.\d+\.\d+\.\d+'`
set -f
parts=(${ip//./ })
team_num=`expr \( \( ${parts[1]} - 60 \) * 256 \) % 1024 + ${parts[2]}`

if [[ -z "$team_num" ]]; then
	echo Service is note reday to start yet..
	exit 1
fi

nick=${1:-@team$team_num}
master=10.10.10.100:16770

./partychat-node $master 6666 $nick
