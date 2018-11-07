from http_helpers.objects import Response, Request
from handlers.base_handler import BaseHandler
from vmf import VendingMachinesFactory


class MachineMetaHandler(BaseHandler):
    def __init__(self, vm_object: VendingMachinesFactory):
        self.vm = vm_object

    def handle(self, request: Request) -> Response:
        return Response(200, b"OK")
