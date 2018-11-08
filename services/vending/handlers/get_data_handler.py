from http_helpers.objects import Response, Request
from handlers.base_handler import BaseHandler
from vmkeys import VMKeys


class GetDataHandler(BaseHandler):
    def __init__(self, vm_keys: VMKeys):
        self.vm_keys = vm_keys

    def handle(self, request: Request) -> Response:
        try:
            machine, key = str(request.body).split()
            return Response(200, (self.vm_keys[machine][key]).encode())
        except (IndexError, KeyError):
            return Response(404, b'Not Found')
        except ValueError:
            return Response(400, b'Bad Request')
