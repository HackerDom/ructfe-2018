#!/usr/bin/env python3

import sys
from checker import Checker
import checker
from networking import State
import json

import string
import random

from ws4py.client import WebSocketBaseClient
from ws4py.manager import WebSocketManager

PORT = 8888
# WS_BROADCASTER_ADDR = 'ws://{}/news'
# DB_ADDR = 'http://{}/db/{}'

WS_BROADCASTER_ADDR = 'ws://{}:7777/'
DB_ADDR = 'http://{}/{}'

m = WebSocketManager()


class EchoClient(WebSocketBaseClient):
    def handshake_ok(self):
        m.add(self)

    def add_writer(self, link):
        self.writer = link

    def received_message(self, msg):
        self.writer.append(str(msg))


def get_rnd_string(n):
    return ''.join(random.choices(string.ascii_uppercase + string.digits, k=n))


async def handler_check(hostname):
    messages = []

    m.start()
    client = EchoClient(WS_BROADCASTER_ADDR.format(hostname))
    client.add_writer(messages)
    client.connect()

    first = State(hostname, PORT, 'first')

    key = get_rnd_string(random.randint(5, 20))
    msg = {
        "text": get_rnd_string(random.randint(5, 20)),
        "frequency": 1500,
        "dpm": 500,
        "need_base32": False,
        "password": "123",
        "is_private": False,
        'datetime': None,
    }

    status, text = await first.post(key, msg, False)

    if status != 200:
        print('Не удалось положить в бд')
        checker.log()  # Не удалось положить в бд

    if msg not in json.loads(text):
        print('В ответе нет нашего сообщения')
        checker.log()  # В ответе нет нашего сообщения

    text = await first.get(key)

    if msg not in json.loads(text):
        print('В ответе нет нашего сообщения')
        checker.log()  # В ответе нет нашего сообщения

    m.close_all()
    m.stop()
    m.join()

    if key not in messages:
        print('Не пришли новости о новой станции')
        checker.log()  # Не пришли новости о новой станции

    checker.ok()


async def handler_get(hostname, id, flag):
    first = State(hostname, PORT, 'first')
    text = await first.get(id)

    if flag not in text:
        print('NO FLAG')
        checker.corrupt()

    checker.ok()


async def handler_put(hostname, id, flag):
    first = State(hostname, PORT, 'first')

    key = get_rnd_string(random.randint(10, 20))
    msg = {
        "text": flag,
        "frequency": random.randint(1, 4000),
        "dpm": random.randint(1, 4000),
        "need_base32": False,
        "password": get_rnd_string(random.randint(15, 30)),
        "is_private": True,
        'datetime': None,
    }

    status, _ = await first.post(key, msg, False)

    if status != 200:
        print('CANT PUT')
        checker.mumble()
    
    print(key)
    checker.ok()


def main(argv):
    checker = Checker(handler_check, [(handler_put, handler_get)])
    checker.process(argv)


if __name__ == "__main__":
    main(sys.argv)
