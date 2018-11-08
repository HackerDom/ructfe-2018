import struct

from array import array
from threading import Lock


class VendingMachinesFactory:
    def __init__(self):
        self.__name = slice(0, 8)
        self.__manufacturer = slice(8, 16)
        self.__meta = slice(16, 128)

        self.__key = slice(128, 160)
        self.__master_key = slice(160, 288)

        self.struct_size = sum(
            map(self.slice_range, (self.__name, self.__manufacturer, self.__meta, self.__key, self.__master_key)))
        self.vms = array('B', (0 for _ in range(self.struct_size)))
        self.composition = f"{self.slice_range(self.__name)}s" \
                           f"{self.slice_range(self.__manufacturer)}s" \
                           f"{self.slice_range(self.__meta)}s" \
                           f"{self.slice_range(self.__key)}s" \
                           f"{self.slice_range(self.__master_key)}s"

        self.lock_obj = Lock()
        self.vms_counter = 0

    @staticmethod
    def slice_range(sls: slice) -> int: return sls.stop - sls.start

    def add_new_machine(self, name: str, inventor: str, meta: str, key: str, master_key: str) -> int:
        name = name.encode('ascii')[:self.slice_range(self.__name)]
        inventor = inventor.encode('ascii')[:self.slice_range(self.__manufacturer)]
        meta = meta.encode('ascii')[:self.slice_range(self.__meta)]
        key = key.encode('ascii')[:self.slice_range(self.__key)]
        master_key = master_key.encode('ascii')[:self.slice_range(self.__master_key)]

        with self.lock_obj:
            counter = self.vms_counter
            self.vms.extend(0 for _ in range(self.struct_size))
            self.vms_counter += 1

        with memoryview(self.vms) as mv:
            struct.pack_into(
                self.composition, mv[counter * self.struct_size: (counter + 1) * self.struct_size],
                0, name, inventor, meta, key, master_key)

        return counter

    def __getitem__(self, item: int) -> memoryview:
        return memoryview(self.vms)[item * self.struct_size: (item + 1) * self.struct_size]

    def get_machine_name(self, vm_id: int) -> memoryview:
        return self[vm_id][self.__name]

    def get_machine_manufacturer(self, vm_id: int) -> memoryview:
        return self[vm_id][self.__manufacturer]

    def get_machine_meta(self, vm_id: int, start: int, stop: int) -> memoryview:
        return self[vm_id][start:stop] if start < self.__meta.stop and stop < self.__meta.stop else None

    def get_master_info(self, vm_id: int, key: bytes) -> memoryview:
        vm = self[vm_id]
        key_len = self.slice_range(self.__key)
        if struct.pack(f"{key_len}s", key[:key_len]) == vm[self.__key]:
            return vm[self.__master_key]

    def __len__(self) -> int: return len(self.vms)
