from client import VendingClient
from .utils import get_rand_word
from actions import OK


def put(command_ip, flag_id, flag, vuln=None):
    if int(vuln) == 1:
        return put_into_struct(command_ip, flag)
    else:
        return put_into_keys(command_ip, flag)


def put_into_struct(command_ip, flag):
    vending_client = VendingClient(command_ip)
    access_key = get_rand_word(128)
    vm_id = vending_client.create_machine(get_rand_word(8), get_rand_word(8), get_rand_word(16), access_key, flag)
    return {"code": OK, "flag_id": "{}:{}".format(vm_id, access_key)}


def put_into_keys(command_ip, flag):
    vending_client = VendingClient(command_ip)
    access_key, p1, p2, p3, p4 = vending_client.create_vending_item(flag)
    vm_id = vending_client.create_machine(
        get_rand_word(8), get_rand_word(8), access_key, get_rand_word(20), get_rand_word(20))
    return {"code": OK, "flag_id": "{}:{}:{}:{}:{}".format(vm_id, p1, p2, p3, p4)}