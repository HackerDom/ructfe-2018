from typing import Tuple
from urllib.request import urlopen, Request
from urllib.error import HTTPError, URLError
from socket import timeout as STimeoutError
from http.client import RemoteDisconnected
from useragents import get
from actions import MUMBLE, DOWN


class VendingClient:
    def __init__(self, host, timeout=8):
        self.url = host
        self.timeout = timeout

    def create_machine(self, name: str, inventor: str, meta: str, key: str, flag: str) -> str:
        req = self.create_request(
            "CREATE", "/vending_machine", ("{} {} {} {} {}".format(name, inventor, meta, key, flag)).encode())
        return self.retry_request(req).split(":")[1]

    def get_machine_name(self, vm_id: str) -> str:
        req = self.create_request(
            "GET", "/machine_name", vm_id.encode())
        return self.retry_request(req)

    def get_machine_manufacturer(self, vm_id: str) -> str:
        req = self.create_request(
            "GET", "/machine_manufacturer", vm_id.encode())
        return self.retry_request(req)

    def get_machine_meta(self, vm_id: str, start: int, end: int) -> str:
        req = self.create_request(
            "GET", "/machine_meta", ("{} {} {}".format(vm_id, start, end).encode()))
        return self.retry_request(req)

    def get_machine_master_key(self, vm_id: str, access_key: str) -> str:
        req = self.create_request(
            "GET", "/machine_master_key", ("{} {}".format(vm_id, access_key)).encode())
        return self.retry_request(req)

    def create_vending_item(self, flag: str) -> Tuple[str, str, str, str, str]:
        req = self.create_request(
            "CREATE", "/vending_item", flag.encode())
        data = self.retry_request(req)
        return data.split(":")

    def get_vending_item(self, vm_id: str, access_key: str) -> str:
        req = self.create_request(
            "GET", "/vending_item", ("{} {}".format(vm_id, access_key)).encode())
        return self.retry_request(req)

    def retry_request(self, req: Request) -> str or None:
        try:
            resp = urlopen(req, timeout=self.timeout).read().decode().strip()
        except (HTTPError, URLError, STimeoutError, RemoteDisconnected):
            try:
                resp = urlopen(req, timeout=self.timeout).read().decode().strip()
            except HTTPError as e:
                raise VendingClientException(MUMBLE, "Incorrect page was given!", e)
            except (STimeoutError, RemoteDisconnected, URLError) as e:
                raise VendingClientException(DOWN, "Can't reach server!", e)
        return resp

    def create_request(self, method: str, url: str, body: bytes):
        request = Request("http://" + self.url + url, method=method, headers={"User-Agent": get()})
        request.data = body + b"\n"
        return request


class VendingClientException(Exception):
    def __init__(self, exit_code, message_public, message_private):
        self.code = exit_code
        self.public = message_public
        self.private = message_private
