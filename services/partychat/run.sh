#! /bin/bash

nick=@team`hostname -I | grep -Po '\d+\.\d+\.\d+\.\K\d+'`
master=localhost:16770

./partychat-node $master 6666 $nick