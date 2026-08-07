from __future__ import annotations

from typing import Any

from . import __version__
from .provider import HltbProvider, SearchMode
from .protocol import ProtocolError, Request
from .serializer import game_result_to_dict


PROTOCOL_VERSION = 1


class BridgeService:
    def __init__(
        self,
        provider: HltbProvider | None = None,
    ) -> None:
        self._provider = provider or HltbProvider()

    async def execute(
        self,
        request: Request,
    ) -> dict[str, Any]:
        if request.method == "ping":
            return {
                "protocolVersion": PROTOCOL_VERSION,
                "bridgeVersion": __version__,
            }

        if request.method == "search":
            return await self._search(request)

        if request.method == "getById":
            return await self._get_by_id(request)

        raise ProtocolError(
            "method_not_found",
            f"Unknown method: {request.method}",
            request.request_id,
        )

    async def _search(
        self,
        request: Request,
    ) -> dict[str, Any]:
        query = request.params.get("query")

        if not isinstance(query, str) or not query.strip():
            raise ProtocolError(
                "invalid_params",
                "'query' must be a non-empty string.",
                request.request_id,
            )

        raw_mode = request.params.get(
            "mode",
            SearchMode.ALL.value,
        )

        if not isinstance(raw_mode, str):
            raise ProtocolError(
                "invalid_params",
                "'mode' must be a string.",
                request.request_id,
            )

        try:
            mode = SearchMode(raw_mode)
        except ValueError as exc:
            valid = ", ".join(item.value for item in SearchMode)

            raise ProtocolError(
                "invalid_params",
                f"Unknown search mode {raw_mode!r}. Valid modes: {valid}.",
                request.request_id,
            ) from exc

        games = await self._provider.search(
            query=query,
            mode=mode,
        )

        serialized = [game_result_to_dict(game) for game in games]

        return {
            "results": serialized,
            "count": len(serialized),
        }

    async def _get_by_id(
        self,
        request: Request,
    ) -> dict[str, Any]:
        game_id = request.params.get("gameId")

        if isinstance(game_id, bool) or not isinstance(game_id, int) or game_id <= 0:
            raise ProtocolError(
                "invalid_params",
                "'gameId' must be a positive integer.",
                request.request_id,
            )

        game = await self._provider.get_by_id(game_id)

        return {"game": (game_result_to_dict(game) if game is not None else None)}
