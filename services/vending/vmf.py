from array import array


class VendingMachinesFactory:
    def __init__(self):
        self.vms = array('l', [])
        self.name = slice(0, 15)

    def add_new_machine(self, m_name, m_inventor, m_meta, m_key, m_master_key):
        # name [0-15]> inventor[16-31]> meta[32-255]> || key[256-383]> m_master[384-512]
        # allow only to 256
        # meta = keys
        pass

    def get_info(self):
        pass