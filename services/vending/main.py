from socketserver import ThreadingTCPServer
from typing import Type
import socketserver

with open('docs.txt') as file:
    docs = file.read()
    HTTP_DOCS = b'HTTP/1.1 200 OK\nContent-Type: text/markdown\n\n ' + docs.encode()


class PerfThreadingTCPServer(ThreadingTCPServer):
    def __init__(self, server_address, rq_handler_cls: Type[socketserver.BaseRequestHandler], rq_size: int) -> None:
        self.request_queue_size = rq_size
        super().__init__(server_address, rq_handler_cls)


class RequestsHandler(socketserver.StreamRequestHandler):
    def handle(self) -> None:
        data = self.rfile.readline(65537).strip()
        # frontend
        if data.startswith(b'GET / HTTP'):
            self.wfile.write(HTTP_DOCS)
            return
        # backend
        self.wfile.write(data + b"lol\r\n")


if __name__ == '__main__':
    port = 1883  # https://en.wikipedia.org/wiki/Vending_machine#Modern_vending_machines
    server = PerfThreadingTCPServer(('', port), RequestsHandler, 500)
    print(f"Serving at {port}...")
    server.serve_forever()
