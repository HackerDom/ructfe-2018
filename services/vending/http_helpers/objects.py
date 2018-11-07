from dataclasses import dataclass, field
from typing import Tuple, BinaryIO


@dataclass(frozen=True)
class Response:
    code: int
    body: bytes
    headers: Tuple[Tuple[str, str]] = field(default=())


@dataclass(frozen=True)
class Request:
    method: str
    path: str
    body: BinaryIO
    headers: Tuple[Tuple[str, str]] = field(default=())
