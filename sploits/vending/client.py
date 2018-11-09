from urllib.request import urlopen, Request
from urllib.error import HTTPError, URLError
from socket import timeout as STimeoutError
from http.client import RemoteDisconnected


class VendingClient:
    def __init__(self, host, timeout=8):
        self.url = host
        self.timeout = timeout

    def get_machine_meta(self, vm_id: str, start: int, end: int) -> str:
        req = self.create_request(
            "GET", "/machine_meta", ("{} {} {}".format(vm_id, start, end).encode()))
        return self.retry_request(req)

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
            except (HTTPError, URLError) as e:
                return
        return resp

    def create_request(self, method: str, url: str, body: bytes):
        request = Request("http://" + self.url + url, method=method)
        request.data = body + b"\n"
        return request
