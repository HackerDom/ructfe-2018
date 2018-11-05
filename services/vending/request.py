import struct
import dataclasses
import enum
from typing import Tuple


class Action(enum.Enum):  # todo remove comments
    CREATE_MACHINE = 100  # -> nothing to new machine (NONE -> id) (inc id, anything pub)
    GET_MACHINE_INFO = 101  # name [0-15] & inventor[16-31] (id -> 32 bits)
    GET_PRIVATE_MACHINE_INFO = 102  # key[256-383] m_master[384-512] (id, key -> master)
    GET_META = 103  # (id, meta-point -> bit)
    CREATE_VENDING_KEYS = 110  # (id, k1-v1, ..., kN-vN -> key1, ..., keyN)
    GET_DATA_BY_VENDING_KEY = 111  # (id, k -> bit)


@dataclasses.dataclass
class Request:
    action: Action
    args: Tuple[str]

    def unpack(self):
        pass