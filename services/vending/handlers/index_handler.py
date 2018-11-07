from http_helpers.objects import Response, Request
from handlers.base_handler import BaseHandler

with open("docs.txt", mode='rb') as f:
    DOCS = f.read()


class IndexHandler(BaseHandler):
    def handle(self, request: Request) -> Response:
        return Response(200, DOCS)
