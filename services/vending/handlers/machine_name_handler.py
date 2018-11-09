from http_helpers.objects import Response, Request
from handlers.base_handler import BaseHandler

from vmf import VendingMachinesFactory


class MachineNameHandler(BaseHandler):
    def __init__(self, vm_object: VendingMachinesFactory):
        self.vm = vm_object

    def handle(self, request: Request) -> Response:
        try:
            self.vm.get_machine_name(int(request.body.readline().decode()))
        except ValueError:
            return Response(400, b"Bad Request")