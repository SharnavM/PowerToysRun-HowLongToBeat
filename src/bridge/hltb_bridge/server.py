from __future__ import annotations

import asyncio
import sys
import traceback
from typing import Any

from .provider import HltbProviderError
from .protocol import (
    ProtocolError,
    Request,
    encode_message,
    error_response,
    parse_request,
    success_response,
)
from .service import BridgeService


class BridgeServer:
    def __init__(
        self,
        service: BridgeService | None = None,
    ) -> None:
        self._service = service or BridgeService()

        self._tasks: dict[int, asyncio.Task[None]] = {}
        self._write_lock = asyncio.Lock()

        self._stopping = False

    async def run(self) -> None:
        while not self._stopping:
            line = await asyncio.to_thread(sys.stdin.readline)

            if line == "":
                break

            line = line.strip()

            if not line:
                continue

            await self._accept_line(line)

        await self._cancel_all_tasks()

    async def _accept_line(
        self,
        line: str,
    ) -> None:
        try:
            request = parse_request(line)
        except ProtocolError as exc:
            await self._send(
                error_response(
                    exc.request_id,
                    exc.code,
                    exc.message,
                )
            )
            return

        if request.method == "shutdown":
            await self._handle_shutdown(request)
            return

        if request.method == "cancel":
            await self._handle_cancel(request)
            return

        # Ping is intentionally handled immediately.
        if request.method == "ping":
            await self._execute_inline(request)
            return

        if request.request_id in self._tasks:
            await self._send(
                error_response(
                    request.request_id,
                    "duplicate_id",
                    "A request with this ID is already active.",
                )
            )
            return

        task = asyncio.create_task(self._execute_background(request))

        self._tasks[request.request_id] = task

        task.add_done_callback(
            lambda _task, request_id=request.request_id: self._tasks.pop(
                request_id, None
            )
        )

    async def _execute_inline(
        self,
        request: Request,
    ) -> None:
        try:
            result = await self._service.execute(request)

            await self._send(
                success_response(
                    request.request_id,
                    result,
                )
            )

        except ProtocolError as exc:
            await self._send(
                error_response(
                    request.request_id,
                    exc.code,
                    exc.message,
                )
            )

    async def _execute_background(
        self,
        request: Request,
    ) -> None:
        try:
            result = await self._service.execute(request)

            await self._send(
                success_response(
                    request.request_id,
                    result,
                )
            )

        except asyncio.CancelledError:
            await self._send(
                error_response(
                    request.request_id,
                    "cancelled",
                    "Request was cancelled.",
                )
            )
            raise

        except ProtocolError as exc:
            await self._send(
                error_response(
                    request.request_id,
                    exc.code,
                    exc.message,
                )
            )

        except HltbProviderError as exc:
            await self._send(
                error_response(
                    request.request_id,
                    "upstream_error",
                    str(exc),
                )
            )

        except Exception:
            traceback.print_exc(file=sys.stderr)

            await self._send(
                error_response(
                    request.request_id,
                    "internal_error",
                    "Unexpected bridge error.",
                )
            )

    async def _handle_cancel(
        self,
        request: Request,
    ) -> None:
        target_id = request.params.get("requestId")

        if (
            isinstance(target_id, bool)
            or not isinstance(target_id, int)
            or target_id < 0
        ):
            await self._send(
                error_response(
                    request.request_id,
                    "invalid_params",
                    "'requestId' must be a non-negative integer.",
                )
            )
            return

        task = self._tasks.get(target_id)

        if task is None or task.done():
            cancelled = False
        else:
            task.cancel()
            cancelled = True

        await self._send(
            success_response(
                request.request_id,
                {
                    "requestId": target_id,
                    "cancelled": cancelled,
                },
            )
        )

    async def _handle_shutdown(
        self,
        request: Request,
    ) -> None:
        self._stopping = True

        await self._cancel_all_tasks()

        await self._send(
            success_response(
                request.request_id,
                {
                    "shuttingDown": True,
                },
            )
        )

    async def _cancel_all_tasks(self) -> None:
        current_tasks = [task for task in self._tasks.values() if not task.done()]

        for task in current_tasks:
            task.cancel()

        if current_tasks:
            await asyncio.gather(
                *current_tasks,
                return_exceptions=True,
            )

        self._tasks.clear()

    async def _send(
        self,
        message: dict[str, Any],
    ) -> None:
        encoded = encode_message(message)

        async with self._write_lock:
            sys.stdout.write(encoded)
            sys.stdout.write("\n")
            sys.stdout.flush()
