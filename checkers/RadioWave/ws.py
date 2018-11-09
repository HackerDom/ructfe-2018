import checker
import asyncio
import aiohttp
import sys

import matplotlib.pyplot as plt

class WSHelper:
	def __init__(self, conn, log_info, type):
		self.connection = conn
		self.log_info = log_info
		self.closed = False
		self.type = type
	def start(self):
		asyncio.ensure_future(self.start_internal())
	async def start_internal(self):
		try:
			async for msg in self.connection:
				if msg.type == self.type:
					checker.log(self.log_info + 'get data, length: {}'.format(len(msg.data)))
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
		await self.connection.close()

class WSHelperBinaryHanlder(WSHelper):
	@staticmethod
	async def create(url, state, handle):
		log_info, conn = await state.get_connection(url)
		return WSHelperBinaryHanlder(conn, log_info, handle)
	def __init__(self, conn, log_info, handle):
		WSHelper.__init__(self, conn, log_info, aiohttp.WSMsgType.BINARY)
		self.specs = []
		self.handle = handle
	async def process(self, msg):
		self.handle(msg.data)

class WSHelperSearchText(WSHelper):
	@staticmethod
	async def create(url, state):
		log_info, conn = await state.get_connection(url)
		return WSHelperSearchText(conn, log_info)
	def __init__(self, conn, log_info):
		WSHelper.__init__(self, conn, log_info, aiohttp.WSMsgType.TEXT)
		self.queue = asyncio.Queue()
		self.wanted = set()
	async def process(self, msg):
		await self.queue.put(msg.data)
	def want(self, data):
		self.wanted.add(data)
	async def finish(self):
		while len(self.wanted) > 0:
			top = await self.queue.get()
			if top in self.wanted:
				self.wanted.remove(top)
