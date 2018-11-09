import checker
import asyncio
import aiohttp
import sys

import matplotlib.pyplot as plt

class WSHelper:
	def __init__(self, connection, type):
		self.connection = connection
		self.closed = False
		self.type = type
	def start(self):
		asyncio.ensure_future(self.start_internal())
	async def start_internal(self):
		try:
			async with self.connection as ws:
				async for msg in ws:
					if msg.type == self.type:
						checker.log('get data, length: {}'.format(len(msg.data)))
						try:
							await self.process(msg)
						except Exception as ex:
							checker.mumble(error='can\'t process service responce', exception=ex)
					elif msg.type == aiohttp.WSMsgType.CLOSED:
						self.closed = True
						break
					else:
						checker.mumble(error='get message with unexpected type {}\nmessage: {}'.format(msg.type, msg.data))
		except Exception as ex:
			checker.down(error='something down', exception=ex)
	async def close(self):
		self.connection.close()

class WSHelperBinaryHanlder(WSHelper):
	def __init__(self, connection, handle):
		WSHelper.__init__(self, connection, aiohttp.WSMsgType.BINARY)
		self.specs = []
		self.handle = handle
	async def process(self, msg):
		self.handle(msg.data)

class WSHelperSearchJson(WSHelper):
	def __init__(self, connection, fields, required):
		WSHelper.__init__(self, connection, aiohttp.WSMsgType.TEXT)
		self.queue = asyncio.Queue()
		self.wanted = set()
		self.fields = fields
		self.required = required
	async def process(self, msg):
		data = msg.json(loads = lambda s : checker.parse_json(s, self.fields, self.required))
		await self.queue.put(data)
	def want(self, point):
		self.wanted.add(json.dumps(data, sort_keys=True))
	async def finish(self):
		while len(self.wanted) > 0:
			top = await self.queue.get()
			top = json.dumps(top, sort_keys=True)
			if top in self.wanted:
				self.wanted.remove(top)
		self.connection.close()
	async def find(self, id, field):
		while True:
			if self.queue.empty() and self.closed:
				checker.mumble(error='point not found')
			top = await self.queue.get()
			if top[field] == id:
				self.connection.close()
				return top
