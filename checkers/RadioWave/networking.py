#!/usr/bin/env python3

import checker
import aiohttp
import random
import asyncio
import string
from ws import WSHelper, WSHelperBinaryHanlder

import UserAgents
import json

def get_cookie_string(cookies):
	return '; '.join([str(cookie.key) + '=' + str(cookie.value) for cookie in cookies])

async def check_status(response, log_info):
	if response.status >= 500:
		checker.down(error='{}\n\tstatus code is {}. Content: {}\n'.format(log_info, response.status, await response.text()))
	if response.status != 200:
		checker.mumble(error='{}\n\tstatus code is {}. Content: {}\n'.format(log_info, response.status, await response.text()))

def get_log_info(name, url):
	return '[{:05}] {}: {}'.format(random.randint(0, 99999), name, url)

class State:
	def __init__(self, hostname, port=None, name=''):
		self.hostname = hostname
		self.name = name
		self.port = '' if port is None else ':' + str(port)
		cookie_jar = aiohttp.CookieJar(unsafe=True)
		self.session = aiohttp.ClientSession(
			cookie_jar=cookie_jar,
			headers={
				'Referer': self.get_url(''),
				'User-Agent': UserAgents.get(),
			})
	def __del__(self):
		self.session.close()
	def get_url(self, path='', proto='http'):
		return '{}://{}{}/{}'.format(proto, self.hostname, self.port, path.lstrip('/'))

	async def get(self, url):
		url = self.get_url(url)
		log_info = get_log_info(self.name, url)
		try:
			async with self.session.get(url) as response:
				await check_status(response, log_info)
				text = await response.text()
				checker.log(log_info + ' responsed')
				return text
		except Exception as ex:
			checker.down(error=log_info, exception=ex)

	async def post(self, url, data={}, need_check_status=True):
		url = self.get_url(url)
		log_info = get_log_info(self.name, url)
		try:
			checker.log(log_info + ' ' + json.dumps(data))
			async with self.session.post(url, json=data) as response:
				if need_check_status:
					await check_status(response, log_info)
					text = await response.text()
					checker.log(log_info + ' responsed')
					return text
				else:
					return response.status, await response.text()
		except Exception as ex:
			checker.down(error='{}\n{}'.format(log_info, data), exception=ex)

	def get_binary_dumper(self, url, process):
		url = self.get_url(url, proto='ws')
		log_info = get_log_info(self.name, url)
		try:
			connection = self.session.ws_connect(url, origin=self.get_url(''))
			checker.log(log_info + ' connected')
		except Exception as ex:
			checker.down(error=log_info, exception=ex)
		helper = WSHelperBinaryHanlder(connection, process)
		return helper
