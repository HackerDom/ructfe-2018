#! /bin/bash

nick=${1:-@team`hostname -I | grep -Po '\d+\.\d+\.\d+\.\K\d+'`}
master=10.10.10.100:16770

./partychat-node $master 6666 $nick
