#!/usr/bin/python3
from base64 import b64encode
from hashlib import sha1
from sys import argv, stderr
import json
import pickle
import re
import socket
import traceback
from time import sleep

import requests
import websocket

from generators import generate_headers, generate_login, generate_password, generate_label, generate_phrase

REGISTER_URL = "http://{hostport}/register?login={login}&password={password}&phrase={phrase}"
LOGIN_URL = "http://{hostport}/login?login={login}&password={password}"
PHRASE_URL = "http://{hostport}/phrase"
WS_URL = "ws://{}:{}/cmdexec"
OK, CORRUPT, MUMBLE, DOWN, CHECKER_ERROR = 101, 102, 103, 104, 110
PORT = 8080
PHRASE_PATTERN = re.compile("<h1>([a-zA-Z0-9!@#$%&*()_+=/., ]{1,100})</h1>")


def print_to_stderr(*objs):
    print(*objs, file=stderr)


def get_hash(obj):
    return b64encode(sha1(pickle.dumps(obj)).digest()).decode()


def signup(hostport, login, password, phrase):
    register_url = REGISTER_URL.format(
        hostport=hostport,
        login=login,
        password=password,
        phrase=b64encode(phrase.encode()).decode()
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
    print("vulns: 1:7")
    exit(OK)


def check(hostname):
    exit(OK)


def not_found(*args):
    print_to_stderr("Unsupported command %s" % argv[1])
    return CHECKER_ERROR


def create_label(hostname, cookies, text, font, size):
    ws = websocket.create_connection(
        WS_URL.format(hostname, PORT),
        timeout=10
    )
    ws.send(create_command_request("create", {
        "RawCookies": get_raw_cookies(cookies),
        "Text": text,
        "Font": font,
        "Size": size,
    }))
    result = ws.recv()
    ws.close()
    return result


def list_labels(hostname, cookies):
    ws = websocket.create_connection(
        WS_URL.format(hostname, PORT),
        timeout=10
    )
    ws.send(create_command_request("list", {
        "RawCookies": get_raw_cookies(cookies),
        "Offset": 0
    }))
    response = ws.recv()
    ws.close()
    return json.loads(response.encode())


def view_label(hostname, cookies, label_id):
    ws = websocket.create_connection(
        WS_URL.format(hostname, PORT),
        timeout=10
    )
    ws.send(create_command_request("view", {
        "RawCookies": get_raw_cookies(cookies),
        "LabelId": label_id
    }))
    response = ws.recv()
    ws.close()
    return json.loads(response.encode())


def get_phrase_data(hostname, cookies):
    r = requests.get(
        url=PHRASE_URL.format(hostport="{}:{}".format(hostname, PORT)),
        headers=generate_headers(),
        cookies=cookies
    )
    r.raise_for_status()
    return PHRASE_PATTERN.findall(r.content.decode())


def get_last_users(hostname):
    ws = websocket.create_connection(
        WS_URL.format(hostname, PORT),
        timeout=10
    )
    ws.send(create_command_request("last_users", {}))
    response = ws.recv()
    ws.close()
    return json.loads(response.encode())


def put_first(hostname, flag_id, flag):
    login = generate_login()
    password = generate_password()
    exit_code = OK
    try:
        cookies = signup("{}:{}".format(hostname, PORT), login, password, generate_phrase())
        label_font, label_size = generate_label()
        if create_label(hostname, cookies, flag, label_font, label_size) != "true":
            print_to_stderr("Can not create label")
            exit(MUMBLE)
        print("{},{},{}".format(
            login,
            password,
            get_hash((flag, label_font, label_size))
        ))
    except (requests.exceptions.ConnectTimeout, socket.timeout, requests.exceptions.ConnectionError):
        traceback.print_exc()
        exit_code = DOWN
    except (
        requests.exceptions.HTTPError, UnicodeDecodeError, json.decoder.JSONDecodeError,
        TypeError, websocket._exceptions.WebSocketBadStatusException,
        websocket._exceptions.WebSocketConnectionClosedException,
        requests.exceptions.ReadTimeout
    ):
        traceback.print_exc()
        exit_code = MUMBLE
    exit(exit_code)


def check_label_correctness(label, flag, expected_label_hash):
    text = label.get("Text", None)
    font = label.get("Font", None)
    size = label.get("Size", None)
    label_id = label.get("ID", None)
    if text is None or font is None or size is None or label_id is None:
        print_to_stderr("Label text =", text)
        print_to_stderr("Label font =", font)
        print_to_stderr("Label size =", size)
        print_to_stderr("Label id =", label_id)
        exit(MUMBLE)
    real_label_hash = get_hash((text, font, size))
    if real_label_hash != expected_label_hash:
        print_to_stderr("Label(text={}, font={}, size={}) real hash='{}', but expected hash='{}'".format(
            text, font, size, real_label_hash, expected_label_hash
        ))
        exit(CORRUPT)
    if text != flag:
        print_to_stderr("Label(text={}, font={}, size={}), but expected text(flag)='{}'".format(
            text, font, size, flag
        ))
        exit(CORRUPT)
    return label_id


def check_users_correctness(users, login):
    if type(users) != list:
        exit(MUMBLE)

    for user in users:
        if type(user) != dict:
            exit(MUMBLE)

        username = user.get("Login")
        if type(username) != str:
            exit(MUMBLE)
        if username == login:
            return True
    return False


def get_first(hostname, flag_id, flag):
    login, password, expected_label_hash = flag_id.split(',')
    exit_code = OK
    try:
        cookies = signin("{}:{}".format(hostname, PORT), login, password)
        labels = list_labels(hostname, cookies)
        if len(labels) != 1:
            print_to_stderr("There is multiple or empty labels={} gotten by cookies={}".format(labels, cookies))
            exit(CORRUPT)
        label = labels[0]
        label_id = check_label_correctness(label, flag, expected_label_hash)
        label = view_label(hostname, cookies, label_id)
        check_label_correctness(label, flag, expected_label_hash)
    except (requests.exceptions.ConnectTimeout, socket.timeout, requests.exceptions.ConnectionError):
        traceback.print_exc()
        exit_code = DOWN
    except (
        requests.exceptions.HTTPError, UnicodeDecodeError, json.decoder.JSONDecodeError,
        TypeError, websocket._exceptions.WebSocketBadStatusException,
        websocket._exceptions.WebSocketConnectionClosedException,
        requests.exceptions.ReadTimeout, KeyError
    ):
        traceback.print_exc()
        exit_code = MUMBLE
    exit(exit_code)


def put_second(hostname, flag_id, flag):
    login = generate_login()
    password = generate_password()
    exit_code = OK
    try:
        signup("{}:{}".format(hostname, PORT), login, password, flag)
        print("{},{},{}".format(
            login,
            password,
            flag
        ))
    except (requests.exceptions.ConnectTimeout, socket.timeout, requests.exceptions.ConnectionError):
        traceback.print_exc()
        exit_code = DOWN
    except (
        requests.exceptions.HTTPError, UnicodeDecodeError, json.decoder.JSONDecodeError,
        TypeError, requests.exceptions.ReadTimeout
    ):
        traceback.print_exc()
        exit_code = MUMBLE
    exit(exit_code)


def get_second(hostname, flag_id, flag):
    login, password, encoded_flag = flag_id.split(',')
    exit_code = OK
    try:
        cookies = signin("{}:{}".format(hostname, PORT), login, password)
        phrase_data = get_phrase_data(hostname, cookies)
        if len(phrase_data) != 1:
            exit(CORRUPT)
        phrase = phrase_data[0]
        if phrase != flag:
            exit(CORRUPT)
        last_users = get_last_users(hostname)
        if not check_users_correctness(last_users, login):
            exit(CORRUPT)

    except (requests.exceptions.ConnectTimeout, socket.timeout, requests.exceptions.ConnectionError):
        traceback.print_exc()
        exit_code = DOWN
    except (
        requests.exceptions.HTTPError, UnicodeDecodeError, TypeError, requests.exceptions.ReadTimeout, KeyError
    ):
        traceback.print_exc()
        exit_code = MUMBLE
    exit(exit_code)


def get(hostname, flag_id, flag, vuln):
    {'1': get_first, '2': get_second}[vuln](hostname, flag_id, flag)


def put(hostname, flag_id, flag, vuln):
    {'1': put_first, '2': put_second}[vuln](hostname, flag_id, flag)


COMMANDS = {'check': check, 'put': put, 'get': get, 'info': info}


def main():
    try:
        COMMANDS.get(argv[1], not_found)(*argv[2:])
    except Exception:
        traceback.print_exc()
        exit(CHECKER_ERROR)


if __name__ == '__main__':
    main()
