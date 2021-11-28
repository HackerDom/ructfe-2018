#!/bin/bash

BASE_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null && pwd )"

cd $BASE_DIR

docker-compose run vch /app/vch $1 $2 $3 $4
