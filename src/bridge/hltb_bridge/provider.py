from __future__ import annotations

from enum import Enum

from howlongtobeatpy import HowLongToBeat, SearchModifiers

from .models import GameResult
from .serializer import entry_to_game_result


class SearchMode(str, Enum):
    ALL = "all"
    HIDE_DLC = "hide_dlc"
    DLC_ONLY = "dlc_only"


class HltbProviderError(RuntimeError):
    pass


def search_modifier_for(mode: SearchMode) -> SearchModifiers:
    mapping = {
        SearchMode.ALL: SearchModifiers.NONE,
        SearchMode.HIDE_DLC: SearchModifiers.HIDE_DLC,
        SearchMode.DLC_ONLY: SearchModifiers.ISOLATE_DLC,
    }

    return mapping[mode]


class HltbProvider:
    def __init__(self) -> None:
        self._client = HowLongToBeat(
            input_minimum_similarity=0.0,
            input_auto_filter_times=False,
        )

    async def search(
        self,
        query: str,
        mode: SearchMode = SearchMode.ALL,
    ) -> list[GameResult]:
        query = query.strip()

        if not query:
            return []

        results = await self._client.async_search(
            query,
            search_modifiers=search_modifier_for(mode),
            similarity_case_sensitive=False,
        )

        if results is None:
            raise HltbProviderError(f"HowLongToBeat search failed for query: {query!r}")

        return [entry_to_game_result(entry) for entry in results]

    async def get_by_id(
        self,
        game_id: int,
    ) -> GameResult | None:
        if game_id <= 0:
            return None

        result = await self._client.async_search_from_id(game_id)

        if result is None:
            return None

        return entry_to_game_result(result)
