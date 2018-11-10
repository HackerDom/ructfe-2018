#!/usr/bin/env python3

import sys
from checker import Checker
import checker
from networking import State
import random
import json
import asyncio
import time
import TextGenerator
import base64

from MorseParser import MorseParser
from SoundFinder import SoundFinder
from ws import WSHelperSearchText, WSHelperBinaryHanlder

PORT = 7777

def get_message(text=None, dpm=None, freq=None, is_private=None, need_base32=None, password=None):
	if text is None:
		text = TextGenerator.get_text().upper() 
	password = checker.get_value_or_rand_string(password, 30)
	if need_base32 is None:
		need_base32 = not bool(random.getrandbits(2))
	if is_private is None:
		is_private = bool(random.getrandbits(1))
	if dpm is None:
		dpm = random.randint(35 * 60 // 4, 35 * 60 // 3)
	if freq is None:
		freq = random.randint(50, 2000)
	data = {
		'text': text,
		'dpm': dpm,
		'frequency': freq,
		'is_private': is_private,
		'need_base32': need_base32,
	}
	if password:
		data['password'] = password
	return data

def get_second_message(freq, **kwargs):
	f = random.randint(50, 2000)
	if freq is not None:
		while 0.5 <= f / freq <= 2:
			f = random.randint(50, 2000)
	return get_message(freq=f, **kwargs)

def base32(s):
	return base64.b32encode(s.encode('ascii')).decode('ascii').rstrip('=')

def get_or_None(d, f):
	if f in d:
		return d[f]
	return None

def equal(expected, actual, fields):
	for f in fields:
		exp = get_or_None(expected, f)
		act = get_or_None(actual, f)
		if exp != act:
			return False
	return True

def has_equal(messages, message, fields):
	for m in messages:
		if equal(message, m, fields):
			return True
	return False

FIELDS = ['text', 'frequency', 'dpm', 'need_base32', 'is_private']
ALL_FIELDS = FIELDS + ['password']

async def check_news(hostname):
	first = State(hostname, PORT, 'first')
	second = State(hostname, PORT, 'second')
	listener = await WSHelperSearchText.create('/news', second)
	listener.start()

	message = get_message(is_private=False)
	channel = checker.get_rand_string(30)
	response = await first.post('/db/{}'.format(channel), message)
	messages = checker.parse_json_list(response, FIELDS)
	if message['need_base32']:
		message['text'] = base32(message['text'])

	if not has_equal(messages, message, ALL_FIELDS):
		checker.mumble(message="Can't find message. May be you delete my password?")

	listener.want(channel)
	await listener.finish()
	await listener.close()

async def handler_check(hostname):
	await check_news(hostname)
	checker.ok()

async def handler_put_pass(hostname, id, flag):
	checker.log('put pass')
	message = get_message(password=flag, is_private=True)
	state = State(hostname, PORT)
	await state.post('/db/{}'.format(id), message)
	checker.ok(message=id)

async def handler_get_pass(hostname, id, flag):
	checker.log('get pass')
	message = get_message(password=flag, is_private=True)
	state = State(hostname, PORT)
	res = await state.post('/db/{}'.format(id), message)
	messages = checker.parse_json_list(res)
	if len(messages) <= 1:
		checker.corrupt(error='no old messages')
	if 'password' not in messages[0]:
		checker.corrupt(error='first message has not password')
	if messages[0]['password'] != flag:
		checker.corrupt(error='first message has not right password: bad "{}"'.format(messages[0]['password']))
	checker.ok()

async def handler_put_channel(hostname, id, flag):
	checker.log('put channel')
	message = get_message(is_private=True)
	state = State(hostname, PORT)
	await state.post('/db/{}'.format(flag), message)
	checker.ok(message=id)

async def handler_get_channel(hostname, id, flag):
	checker.log('get channel')
	state = State(hostname, PORT)
	finder = SoundFinder()
	listener = await WSHelperBinaryHanlder.create('/radio/{}'.format(flag), state, finder.get_new_data)
	listener.start()
	await asyncio.sleep(10)
	await listener.close()

	if not finder.result:
		checker.corrupt(error='no signal for 10 seconds')

	checker.ok()

async def handler_put_morse(hostname, id, flag):
	checker.log('put morse')
	message = get_message(is_private=True, text=flag, need_base32=True)
	state = State(hostname, PORT)
	await state.post('/db/{}'.format(id), message)
	checker.ok(message=json.dumps({'channel': id, 'freq': message['frequency']}))
	
async def handler_put_morse_2(hostname, id, flag):
	checker.log('put morse 2')
	message = get_message(is_private=True, text=flag, need_base32=True)
	state = State(hostname, PORT, 'first')
	await state.post('/db/{}'.format(id), message)
	state2 = State(hostname, PORT, 'second')
	message2 = get_second_message(message['frequency'], password=message['password'])
	await state2.post('/db/{}'.format(id), message2)
	checker.ok(message=json.dumps({'channel': id, 'freq': message['frequency']}))

async def handler_get_morse(hostname, id, flag):
	checker.log('get morse')
	id = json.loads(id)
	state = State(hostname, PORT)
	parser = MorseParser(id['freq'])
	listener = await WSHelperBinaryHanlder.create('/radio/{}'.format(id['channel']), state, parser.save)
	listener.start()

	flag = base32(flag)

	await asyncio.sleep(10)
	await listener.close()

	text = parser.process()
	if text not in flag:
		checker.corrupt(error='{} not in {}'.format(text, flag))

	checker.ok(message=text)

def main(argv):
	checker = Checker(handler_check, [(handler_put_pass, handler_get_pass), (handler_put_channel, handler_get_channel, 5), (handler_put_morse, handler_get_morse, 2), (handler_put_morse_2, handler_get_morse, 2)])
	checker.process(argv)

if __name__ == "__main__":
	main(sys.argv)
