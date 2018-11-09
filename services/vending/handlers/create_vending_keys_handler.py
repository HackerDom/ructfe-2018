from http_helpers.objects import Response, Request
from handlers.base_handler import BaseHandler
from vmkeys import VMKeys


class CreateVendingKeysHandler(BaseHandler):
    def __init__(self, vm_keys: VMKeys):
        self.vm_keys = vm_keys

    def handle(self, request: Request) -> Response:
        try:
            data_in_vending = request.body.readline().decode().strip()[:32]
            vm_pub, vm_private = self.vm_keys.add_new_vending_machine_items(data_in_vending, 8)
            return Response(200, f"{vm_pub}:{':'.join(vm_private)}".encode())
        except ValueError:
            return Response(400, b'Bad Request')
