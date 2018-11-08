#!/bin/bash

BASE_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null && pwd )"

set -e
set -u
set -x

vboxmanage snapshot "ructfe2018-master" restore "base_image"

$BASE_DIR/update_image.sh
