from __future__ import annotations

import asyncio

import pytest

from hltb_bridge.protocol import Request
from hltb_bridge.server import BridgeServer


class SlowService:
    def __init__(self) -> None:
        self.started = asyncio.Event()

    async def execute(self, request: Request):
        if request.method == "search":
            self.started.set()

            # Deliberately wait forever.
            # The server must cancel this task.
            await asyncio.Event().wait()

        return {}


class CaptureBridgeServer(BridgeServer):
    def __init__(self, service) -> None:
        super().__init__(service)

        self.messages: list[dict] = []

    async def _send(self, message: dict) -> None:
        self.messages.append(message)


@pytest.mark.asyncio
async def test_cancel_stops_active_request():
    service = SlowService()
    server = CaptureBridgeServer(service)

    await server._accept_line(
        '{"id":10,"method":"search","params":{"query":"Elden Ring"}}'
    )

    # Ensure the background search has actually started.
    await asyncio.wait_for(
        service.started.wait(),
        timeout=1.0,
    )

    assert 10 in server._tasks
    assert not server._tasks[10].done()

    await server._accept_line('{"id":11,"method":"cancel","params":{"requestId":10}}')

    # Give the cancelled task time to process CancelledError
    # and emit its response.
    await asyncio.sleep(0)
    await asyncio.sleep(0)

    cancel_response = next(
        message for message in server.messages if message["id"] == 11
    )

    assert cancel_response == {
        "id": 11,
        "ok": True,
        "result": {
            "requestId": 10,
            "cancelled": True,
        },
    }

    target_response = next(
        message for message in server.messages if message["id"] == 10
    )

    assert target_response == {
        "id": 10,
        "ok": False,
        "error": {
            "code": "cancelled",
            "message": "Request was cancelled.",
        },
    }

    # Let the done callback remove the task from the map.
    await asyncio.sleep(0)

    assert 10 not in server._tasks
