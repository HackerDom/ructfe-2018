from http.server import ThreadingHTTPServer, BaseHTTPRequestHandler
from http_helpers.routing import RoutingTable, Route
from http_helpers.objects import Request

from handlers import index_handler, machine_name_handler, \
    create_machine_handler, machine_manufacturer_handler, \
    machine_master_key_handler, machine_meta_handler, \
    create_vending_keys_handler, get_data_handler

from vmf import VendingMachinesFactory
from vmkeys import VMKeys
from dumper import Dumper


VENDING_MACHINES = VendingMachinesFactory()
VM_KEYS = VMKeys()
DUMPER = Dumper(VENDING_MACHINES, VM_KEYS).start()


def create_route_table():
    return RoutingTable(
        Route("GET", "/", index_handler.IndexHandler()),
        Route("CREATE", "/vending_machine", create_machine_handler.CreateMachineHandler(VENDING_MACHINES)),
        Route("GET", "/machine_name", machine_name_handler.MachineNameHandler(VENDING_MACHINES)),
        Route("GET", "/machine_manufacturer", machine_manufacturer_handler.MachineManufacturerHandler(VENDING_MACHINES)),
        Route("GET", "/machine_meta", machine_meta_handler.MachineMetaHandler(VENDING_MACHINES)),
        Route("GET", "/machine_master_key", machine_master_key_handler.MachineMasterKeyHandler(VENDING_MACHINES)),
        Route("CREATE", "/vending_item", create_vending_keys_handler.CreateVendingKeysHandler(VM_KEYS)),
        Route("GET", "/vending_item", get_data_handler.GetDataHandler(VM_KEYS))
    )


class ServiceHttpHandler(BaseHTTPRequestHandler):
    router = create_route_table()
    preset_attrs = frozenset(f'do_{m}' for m in router.available_methods)

    def __getattr__(self, item):
        if item in self.preset_attrs:
            return self.handle_request
        return self.__getattribute__(item)

    def handle_request(self):
        response = self.router.handle(Request(self.command, self.path, self.rfile, self.headers))
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
    port = 11883  # https://en.wikipedia.org/wiki/Vending_machine#Modern_vending_machines
    print("Starting server...")
    server = ConfigurableThreadingHTTPServer(
        ('', port),
        ServiceHttpHandler,
        500
    )
    print("Server started!")
    print(f"Listening for 'http://127.0.0.1:{port}/'...")
    server.serve_forever()