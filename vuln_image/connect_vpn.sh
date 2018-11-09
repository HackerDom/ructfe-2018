#!/bin/bash

BASE_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null && pwd )"

set -e
set -u
set -x

team_id=$1

cd $BASE_DIR/../ansible/roles/vpn/gen/client_for_developers/
sudo openvpn ${team_id}.conf
