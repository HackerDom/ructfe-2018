from vmf import VendingMachinesFactory
from vmkeys import VMKeys
import os
import time
import json
import threading

TEMP_VMF = "vmk.dump.tmp"
VMF_COUNTER = "vmf.counter"
VMF_DUMP = "vmf.dump"
VMK_JSON_TMP = "vmk.json.tmp"
VMK_JSON = "vmk.json"


class Dumper:
    def __init__(self, vmf: VendingMachinesFactory, vmk: VMKeys):
        self.vmf = vmf
        self.vmk = vmk

    def start(self):
        self.load_vmf_snapshot()
        self.load_vmk_snapshot()
        th = threading.Thread(target=self.snapshot_forever, daemon=True)
        th.start()

    def snapshot_forever(self):
        while True:
            self.make_vmf_snapshot()
            self.make_vmk_snapshot()
            time.sleep(30)

    def make_vmf_snapshot(self):
        with memoryview(self.vmf.vms) as mv:
            with open(TEMP_VMF, mode="wb") as file:
                file.write(mv)
            with open(VMF_COUNTER, mode="w") as file:
                file.write(str(self.vmf.vms_counter))
        os.rename(TEMP_VMF, VMF_DUMP)

    def make_vmk_snapshot(self):
        with open(VMK_JSON_TMP, mode="w") as file:
            json.dump(self.vmk.get_dump(), file)
        os.rename(VMK_JSON_TMP, VMK_JSON)

    def load_vmf_snapshot(self):
        if not os.path.exists(VMF_DUMP):
            print("No VMF backups :(")
            return
        with open(VMF_COUNTER, mode="r") as file:
            counter = int(file.read().strip())
        with open(VMF_DUMP, mode="rb") as file:
            self.vmf.load_from(file.read(), counter)
        print(f"Loaded VMF from backup... ({len(self.vmf.vms)})")

    def load_vmk_snapshot(self):
        if not os.path.exists(VMK_JSON):
            print("No VMK backups :(")
            return
        with open(VMK_JSON, mode="r") as file:
            self.vmk.load_from(json.load(file))
        print(f"Loaded VMK from backup... ({len(self.vmk.get_dump())})")