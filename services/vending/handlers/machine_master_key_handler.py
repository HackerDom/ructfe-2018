from http_helpers.objects import Response, Request
from handlers.base_handler import BaseHandler
from vmf import VendingMachinesFactory


class MachineMasterKeyHandler(BaseHandler):
    def __init__(self, vm_object: VendingMachinesFactory):
        self.vm = vm_object

    def handle(self, request: Request) -> Response:
        try:
            vm_id, key = str(request.body).split()
            return Response(200, bytes(self.vm.get_master_info(int(vm_id), key.encode('ascii'))))
        except ValueError:
            return Response(400, b'Bad Request')
