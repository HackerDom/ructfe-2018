#!/usr/bin/env python3

import sys
from checker import Checker
import checker
from networking import State
import random
import json
import asyncio
import time

#PORT = 6454
PORT = 8080

def dump(self, msg, fout):
	fout.write(msg.data)

async def handler_check(hostname):
	first = State(hostname, PORT, 'first')
	with open('dump', 'wb') as fout:
		listener = first.get_binary_dumper('/ghslkgfhsyfth/data', fout)
		await listener.start_internal()
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
