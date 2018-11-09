from client import VendingClient
from actions.utils import get_rand_word
from actions import OK, MUMBLE
from time import sleep
from random import randint, choice

COUNTER_MAX = 100000
META_RANGE = slice(16, 128)


def check(team_host):
    # vm creation check
    client = VendingClient(team_host)

    name = get_rand_word(8)
    manf = get_rand_word(8)
    meta = get_rand_word(META_RANGE.start - META_RANGE.stop)
    key = get_rand_word(128)
    flag = get_rand_word(16)

    # protect from easy patches

    vm_id1 = client.create_machine(name, manf, meta, key, flag)
    sleep(randint(1, 15) / 10)
    vm_id2 = client.create_machine(name, manf, meta, key, flag)
    sleep(randint(1, 15) / 10)
    vm_id3 = client.create_machine(name, manf, meta, key, flag)
    try:
        id1 = int(vm_id1)
        id2 = int(vm_id2)
        id3 = int(vm_id3)
        if not all(x for x in (id1, id2, id3) if 0 < x < 100000):
            return {"code": MUMBLE, "public": "0 < VM_ID < 10000"}
        if not (id1 < id2 < id3 or id2 < id3 < id1 or id3 < id1 < id2):
            return {"code": MUMBLE, "public": "Machine id's should grow incrementally"}
    except ValueError as e:
        return {"code": MUMBLE, "public": "Machine id's should be int", "private": e}

    # final checks

    replicas = [vm_id1, vm_id2, vm_id3]

    chk1 = name == client.get_machine_name(choice(replicas))
    chk2 = manf == client.get_machine_manufacturer(choice(replicas))

    meta_start = randint(META_RANGE.start, META_RANGE.stop)
    meta_end = randint(meta_start, META_RANGE.stop)

    chk3 = client.get_machine_meta(choice(replicas), meta_start, meta_end)\
        .startswith(meta[meta_start-META_RANGE.start:meta_end-META_RANGE.start])
    chk4 = client.get_machine_master_key(choice(replicas), key).strip()\
        .startswith(flag)

    # dbg
    # print(meta[meta_start-META_RANGE.start:meta_end-META_RANGE.start], client.get_machine_meta(choice(replicas), meta_start, meta_end))

    if all((chk1, chk2, chk3, chk4)):
        return {"code": OK}
    return {
        "code": MUMBLE,
        "public": "Seems to be a broken structure of vending",
        "private": "1 - {}, 2 - {}, 3 - {}, 4 - {} = chk results".format(chk1, chk2, chk3, chk4)}


