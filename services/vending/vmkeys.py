import secrets
from hashlib import sha256
from typing import Tuple, Iterable


class VMKeys:
    def __init__(self):
        self.__keys = {}

    def add_new_vending_machine_items(self, data: str, partitioning: int) -> Tuple[str, Iterable[str]]:
        vm_guid = self.generate_rand_key()[:16]
        self.__keys[vm_guid] = {}
        partitioned_data = [data[i:i + partitioning] for i in range(len(data))[::partitioning]]
        results = tuple(map(lambda h: sha256(h.encode()).hexdigest(), ("".join(map(str, (dt, rnd_key, vm_guid)))
            for dt, rnd_key in ((i, self.generate_rand_key(partitioning - i)) for i, x in enumerate(partitioned_data)))))

        for i, result in enumerate(results):
            print(partitioned_data[i], result)
            self.__keys[vm_guid][result] = partitioned_data[i]
        return vm_guid, results

    def __getitem__(self, item):
        return self.__keys[item]

    def generate_rand_key(self, ln=2):
        try:
            return secrets.token_hex(2 ** 2 ** ln)
        except:
            pass