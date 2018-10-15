#!/usr/bin/python3
from base64 import b64encode
from hashlib import sha1
import json
import pickle
from sys import argv, stderr
import socket
import traceback

import requests
import websocket

from generators import generate_headers, generate_login, generate_password, generate_label

REGISTER_URL = "http://{hostport}/register?login={login}&password={password}"
LOGIN_URL = "http://{hostport}/login?login={login}&password={password}"
WS_URL = "ws://{}:{}/cmdexec"
OK, CORRUPT, MUMBLE, DOWN, CHECKER_ERROR = 101, 102, 103, 104, 110
PORT = 8080


def get_hash(obj):
    return b64encode(sha1(pickle.dumps(obj)).digest()).decode()


def signup(hostport, login, password):
    register_url = REGISTER_URL.format(
        hostport=hostport,
        login=login,
        password=password
    )
    r = requests.get(
        url=register_url,
        headers=generate_headers(),
        timeout=10
    )
    r.raise_for_status()
    return r.cookies


def signin(hostport, login, password):
    login_url = LOGIN_URL.format(
        hostport=hostport,
        login=login,
        password=password
    )
    r = requests.get(
        url=login_url,
        headers=generate_headers(),
        timeout=10
    )
    r.raise_for_status()
    return r.cookies


def create_command_request(command, data):
    return json.dumps({
        "Command": command,
        "Data": json.dumps(data)
    })


def get_raw_cookies(cookies):
    return "; ".join([str(k) + "=" + str(v) for k, v in cookies.items()])


def info():
    print("vulns: 1")
    exit(OK)


def check(hostname):
    exit(OK)


def not_found(*args):
    print("Unsupported command %s" % argv[1], file=stderr)
    return CHECKER_ERROR


def create_label(ws, cookies, text, font, size):
    ws.send(create_command_request("create", {
        "RawCookies": get_raw_cookies(cookies),
        "Text": text,
        "Font": font,
        "Size": size,
    }))
    return ws.recv()


def list_labels(ws, cookies):
    ws.send(create_command_request("list", {
        "RawCookies": get_raw_cookies(cookies),
        "Offset": 0
    }))
    return json.loads(ws.recv().encode())


def put(hostname, flag_id, flag, vuln):
    login = generate_login()
    password = generate_password()
    exit_code = OK
    try:
        cookies = signup("{}:{}".format(hostname, PORT), login, password)
        label_font, label_size = generate_label()
        ws = websocket.create_connection(
            WS_URL.format(hostname, PORT),
            timeout=10
        )
        if create_label(ws, cookies, flag, label_font, label_size) != "true":
            exit(MUMBLE)
        ws.send(create_command_request("list", {
            "RawCookies": get_raw_cookies(cookies),
            "Offset": 0
        }))
        labels = list_labels(ws, cookies)
        if len(labels) != 1:
            exit(MUMBLE)
        label = labels[0]
        text = label.get("Text", None)
        font = label.get("Font", None)
        size = label.get("Size", None)
        if text is None or font is None or size is None:
            exit(MUMBLE)
        print("{},{},{}".format(
            login,
            password,
            get_hash((text, font, size))
        ))
        ws.close()
    except (requests.exceptions.ConnectTimeout, socket.timeout, requests.exceptions.ConnectionError):
        exit_code = DOWN
    except (
            requests.exceptions.HTTPError, UnicodeDecodeError, json.decoder.JSONDecodeError,
            TypeError, websocket._exceptions.WebSocketBadStatusException,
            websocket._exceptions.WebSocketConnectionClosedException,
    ):
        exit_code = MUMBLE
    exit(exit_code)


def get(hostname, flag_id, flag, _):
    login, password, label_hash = flag_id.split(',')
    exit_code = OK
    try:
        cookies = signin("{}:{}".format(hostname, PORT), login, password)
        ws = websocket.create_connection(
            WS_URL.format(hostname, PORT),
            timeout=10
        )
        labels = list_labels(ws, cookies)
        if len(labels) != 1:
            exit(CORRUPT)
        label = labels[0]
        text = label.get("Text", None)
        font = label.get("Font", None)
        size = label.get("Size", None)
        if text is None or font is None or size is None:
            exit(MUMBLE)
        if get_hash((text, font, size)) != label_hash:
            exit(CORRUPT)
    except (requests.exceptions.ConnectTimeout, socket.timeout, requests.exceptions.ConnectionError):
        exit_code = DOWN
    except (
            requests.exceptions.HTTPError, UnicodeDecodeError, json.decoder.JSONDecodeError,
            TypeError, websocket._exceptions.WebSocketBadStatusException,
            websocket._exceptions.WebSocketConnectionClosedException,
    ):
        exit_code = MUMBLE
    exit(exit_code)


COMMANDS = {'check': check, 'put': put, 'get': get, 'info': info}


def main():
    try:
        COMMANDS.get(argv[1], not_found)(*argv[2:])
    except Exception:
        traceback.print_exc()
        exit(CHECKER_ERROR)


if __name__ == '__main__':
    main()
