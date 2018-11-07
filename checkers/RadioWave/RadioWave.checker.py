#!/usr/bin/env python3

import sys
from checker import Checker
import checker
from networking import State
import random
import json
import asyncio

PORT = 6454

async def handler_check(hostname):
	checker.ok()

async def handler_get(hostname, id, flag):
	checker.ok()

async def handler_put(hostname, id, flag):
	checker.ok(message=json.dumps(id))

def main(argv):
	checker = Checker(handler_check, [(handler_put, handler_get)])
	checker.process(argv)

if __name__ == "__main__":
	main(sys.argv)
