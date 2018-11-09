from client import VendingClient
from actions.utils import get_rand_word
from actions import OK


def check(team_host):
    # vm creation check
    client = VendingClient(team_host)
    vm_id = client.create_machine(
        get_rand_word(8), get_rand_word(8), get_rand_word(16), get_rand_word(128), "not-a-flag-here-at-all")


    return {"code": OK}


