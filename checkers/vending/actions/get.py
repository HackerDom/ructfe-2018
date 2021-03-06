from client import VendingClient, VendingClientException
from actions import OK, CORRUPT, MUMBLE, DOWN
from time import sleep
from random import randint
from traceback import format_exc
from http.client import RemoteDisconnected

META_RANGE = slice(16, 128)


def get(command_ip, flag_id, flag, vuln=None):
    try:
        if int(vuln) == 1:
            return get_from_struct(command_ip, flag_id, flag)
        else:
            return get_from_keys(command_ip, flag_id, flag)
    except VendingClientException as e:
        return {"code": e.code, "public": e.public, "private": e.private}
    except (IndexError, ValueError) as e:
        return {"code": MUMBLE, "private": "{} {}".format(e, format_exc())}


def get_from_struct(command_ip, flag_id, flag):
    vending_client = VendingClient(command_ip)
    try:
        flag_rcv = vending_client.get_machine_master_key(*flag_id.split(":"))
    except VendingClientException as e:
        if isinstance(e, RemoteDisconnected):
            return {"code": CORRUPT, "public": "Couldn't find key!"}
        raise e

    if flag_rcv and flag_rcv.startswith(flag):
        return {"code": OK}
    else:
        return {"code": CORRUPT}


def get_from_keys(command_ip, flag_id, flag):
    vending_client = VendingClient(command_ip)
    vm_id, p1, p2, p3, p4 = flag_id.split(":")

    try:
        keys_holder_addr = vending_client.get_machine_meta(vm_id, META_RANGE.start, META_RANGE.stop - 1).strip("\x00")
        sleep(randint(1, 15) / 10)
        flag_p1 = vending_client.get_vending_item(keys_holder_addr, p1)
        sleep(randint(1, 15) / 10)
        flag_p2 = vending_client.get_vending_item(keys_holder_addr, p2)
        sleep(randint(1, 15) / 10)
        flag_p3 = vending_client.get_vending_item(keys_holder_addr, p3)
        sleep(randint(1, 15) / 10)
        flag_p4 = vending_client.get_vending_item(keys_holder_addr, p4)
    except VendingClientException as e:
        try:
            if e.private.msg == "Not Found":
                return {"code": CORRUPT, "public": "Couldn't find flag!"}
        except AttributeError:
                return {"code": DOWN, "private": 'from fix'}
        raise e

    if (flag_p1 + flag_p2 + flag_p3 + flag_p4) == flag:
        return {"code": OK}
    else:
        return {"code": CORRUPT}
