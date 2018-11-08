#!/bin/bash

function wait_for_ssh () {
    until ssh -o ConnectTimeout=2 -p2222 root@localhost echo 'Image is up'
        do sleep 1
    done
}

BASE_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null && pwd )"
VBOX="ructfe2018-master"

set -e
set -u
set -x

cd $BASE_DIR

# Start vm
vboxmanage startvm $VBOX --type headless
wait_for_ssh

# Deploy updates
ansible-playbook -i ansible_hosts setup.yml

# Power off VM
ssh -p2222 localhost poweroff
sleep 8

# Export image
time vboxmanage export "ructfe2018-master" -o ructfe2018-tmp.ova
