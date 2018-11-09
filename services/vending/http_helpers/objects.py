from dataclasses import dataclass, field
from typing import Tuple
from io import BufferedReader


@dataclass(frozen=True)
class Response:
    code: int
    body: bytes
    headers: Tuple[Tuple[str, str]] = field(default=())


@dataclass(frozen=True)
class Request:
    method: str
    path: str
    body: BufferedReader
    headers: Tuple[Tuple[str, str]] = field(default=())
