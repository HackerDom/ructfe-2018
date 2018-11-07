#!/bin/bash

BASE_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null && pwd )"
VBOX="ructfe2018-master"

set -e
set -u
set -x

cd $BASE_DIR
vboxmanage startvm $VBOX --type headless
ansible-playbook -i ansible_hosts setup.yml
ssh -p2222 localhost poweroff
sleep 5
time vboxmanage export "ructfe2018-master" -o ructfe2018-tmp.ova
