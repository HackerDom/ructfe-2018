from http_helpers.objects import Response, Request
from handlers.base_handler import BaseHandler

from vmf import VendingMachinesFactory


class MachineMetaHandler(BaseHandler):
    def __init__(self, vm_object: VendingMachinesFactory):
        self.vm = vm_object

    def handle(self, request: Request) -> Response:
        try:
            return Response(200, bytes(self.vm.get_machine_meta(*map(int, (request.body.readline().decode().split())))))
        except (TypeError, ValueError):
            return Response(400, b'Bad Request')
