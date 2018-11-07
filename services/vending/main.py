from http.server import ThreadingHTTPServer
from http.server import BaseHTTPRequestHandler
from http_helpers.routing import RoutingTable, Route
from http_helpers.objects import Request
from handlers import index_handler, machine_handler
from vmf import VendingMachinesFactory

VENDING_MACHINES = VendingMachinesFactory()


def create_route_table():
    return RoutingTable(
        Route("GET", "/", index_handler.IndexHandler()),
        Route("CREATE", "/vending_machine", machine_handler.MachineHandler(VENDING_MACHINES)),
        Route("GET", "/machine_name", machine_handler.MachineHandler(VENDING_MACHINES)),
        Route("GET", "/machine_manufacturer", machine_handler.MachineHandler(VENDING_MACHINES)),
        Route("GET", "/machine_master_key", machine_handler.MachineHandler(VENDING_MACHINES)),
        Route("GET", "/machine_meta", machine_handler.MachineHandler(VENDING_MACHINES))
    )


class ServiceHttpHandler(BaseHTTPRequestHandler):
    router = create_route_table()
    preset_attrs = frozenset(f'do_{m}' for m in router.available_methods)

    def __getattr__(self, item):
        if item in self.preset_attrs:
            return self.handle_request
        return self.__getattribute__(item)

    def handle_request(self):
        response = self.router.handle(Request(self.command, self.path, self.headers, self.rfile))
        self.send_response(response.code)
        for header in response.headers:
            self.send_header(header[0], header[1])
        self.end_headers()
        self.wfile.write(response.body)


class ConfigurableThreadingHTTPServer(ThreadingHTTPServer):
    def __init__(self, server_address, request_handler_class, request_queue_size):
        self.request_queue_size = request_queue_size
        super().__init__(server_address, request_handler_class)


if __name__ == '__main__':
    port = 1883  # https://en.wikipedia.org/wiki/Vending_machine#Modern_vending_machines
    print("Starting server...")
    server = ConfigurableThreadingHTTPServer(
        ('', port),
        ServiceHttpHandler,
        500
    )
    print("Server started!")
    print(f"Listening for 'http://localhost:{port}/'...")
    server.serve_forever()


    # CREATE_MACHINE = 0  # -> nothing to new machine (NONE -> id) (inc id, anything pub)
    # GET_MACHINE_INFO = 1  # name [0-15] & inventor[16-31] (id -> 32 bits)
    # GET_PRIVATE_MACHINE_INFO = 2  # key[256-383] m_master[384-512] (id, key -> master)
    # GET_META = 3  # (id, meta-point -> bit)
    # CREATE_VENDING_KEYS = 4  # (id, k1-v1, ..., kN-vN -> key1, ..., keyN)
    # GET_DATA_BY_VENDING_KEY = 5  # (id, k -> bit)