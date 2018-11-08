from dataclasses import dataclass

from http_helpers.objects import Request, Response
from handlers.base_handler import BaseHandler


@dataclass(frozen=True)
class Route:
    method: str
    url: str
    handler: BaseHandler


class RoutingTable:
    def __init__(self, *routing_list: [Route]):
        self.route_table = {}
        for route in routing_list:
            if route.url not in self.route_table:
                self.route_table[route.url] = {}
            self.route_table[route.url][route.method] = route.handler

    @property
    def available_methods(self) -> frozenset:
        methods = set()
        for i in ([m for m in x.keys()] for x in self.route_table.values()):
            methods.update(i)
        return frozenset(methods)

    def handle(self, request: Request) -> Response:
        if request.path not in self.route_table:
            return Response(404, b'Not Found')
        if request.method not in self.route_table[request.path]:
            return Response(
                405, b'Method not allowed!',
                (("Allow", ','.join(self.route_table[request.path].keys())),))
        return self.route_table[request.path][request.method].handle(request)


