from abc import ABC, abstractmethod
from http_helpers.objects import Request, Response


class BaseHandler(ABC):
    @abstractmethod
    def handle(self, request: Request) -> Response: pass
