from client import VendingClient, VendingClientException
from .utils import get_rand_word
from actions import OK
from time import sleep
from random import randint
from actions import MUMBLE
from traceback import format_exc


def put(command_ip, _, flag, vuln=None):
    try:
        if int(vuln) == 1:
            return put_into_struct(command_ip, flag)
        else:
            return put_into_keys(command_ip, flag)
    except VendingClientException as e:
        return {"code": e.code, "public": e.public, "private": e.private}
    except (IndexError, ValueError) as e:
        return {"code": MUMBLE, "private": "{} {}".format(e, format_exc())}


def put_into_struct(command_ip, flag):
    vending_client = VendingClient(command_ip)
    access_key = get_rand_word(128)
    vm_id = vending_client.create_machine(get_rand_word(8), get_rand_word(8), get_rand_word(16), access_key, flag)
    return {"code": OK, "flag_id": "{}:{}".format(vm_id, access_key)}


def put_into_keys(command_ip, flag):
    vending_client = VendingClient(command_ip)
    access_key, p1, p2, p3, p4 = vending_client.create_vending_item(flag)
    sleep(randint(0, 15) / 10)
    vm_id = vending_client.create_machine(
        get_rand_word(8), get_rand_word(8), access_key, get_rand_word(20), get_rand_word(20))
    return {"code": OK, "flag_id": "{}:{}:{}:{}:{}".format(vm_id, p1, p2, p3, p4)}