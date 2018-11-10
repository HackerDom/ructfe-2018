from http_helpers.objects import Response, Request
from handlers.base_handler import BaseHandler

from vmf import VendingMachinesFactory


class CreateMachineHandler(BaseHandler):
    def __init__(self, vm_object: VendingMachinesFactory):
        self.vm = vm_object

    def handle(self, request: Request) -> Response:
        try:
            name, inventor, meta, key, master_key = request.body.readline().decode().strip().split()
        except (TypeError, ValueError):
            return Response(400, b'Bad request')
        res = self.vm.add_new_machine(name, inventor, meta, key, master_key)
        if res is not None:
            return Response(200, b"Created:" + str(res).encode())
        return Response(400, b'Bad request')
