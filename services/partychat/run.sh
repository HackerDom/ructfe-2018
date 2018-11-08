#! /bin/bash

ip=`hostname -I | grep -Po '\d+\.\d+\.\d+\.\d+'`
set -f
parts=(${ip//./ })
team_num=`expr \( \( ${parts[1]} - 60 \) * 256 \) % 1024 + ${parts[2]}`

nick=${1:-@team$team_num}
master=10.10.10.100:16770

./partychat-node $master 6666 $nick
