import pytest

from hltb_bridge.models import GameResult
from hltb_bridge.protocol import (
    ProtocolError,
    Request,
)
from hltb_bridge.provider import SearchMode
from hltb_bridge.service import BridgeService


def make_game() -> GameResult:
    return GameResult(
        game_id=68151,
        name="Elden Ring",
        alias="Elden Ring Tarnished Edition",
        game_type="game",
        release_year=2022,
        platforms=[
            "PC",
            "PlayStation 5",
        ],
        main_seconds=216306,
        main_extra_seconds=364346,
        completionist_seconds=489602,
        all_styles_seconds=379810,
        similarity=1.0,
        url="https://howlongtobeat.com/game/68151",
        image_url=("https://howlongtobeat.com/games/68151_Elden_Ring.jpg"),
    )


class FakeProvider:
    def __init__(self) -> None:
        self.last_query = None
        self.last_mode = None
        self.last_game_id = None

    async def search(
        self,
        query: str,
        mode: SearchMode = SearchMode.ALL,
    ):
        self.last_query = query
        self.last_mode = mode

        return [make_game()]

    async def get_by_id(
        self,
        game_id: int,
    ):
        self.last_game_id = game_id

        if game_id == 68151:
            return make_game()

        return None


@pytest.mark.asyncio
async def test_ping():
    service = BridgeService(FakeProvider())

    result = await service.execute(
        Request(
            request_id=1,
            method="ping",
            params={},
        )
    )

    assert result["protocolVersion"] == 1
    assert result["bridgeVersion"] == "0.1.0"


@pytest.mark.asyncio
async def test_search():
    provider = FakeProvider()
    service = BridgeService(provider)

    result = await service.execute(
        Request(
            request_id=2,
            method="search",
            params={
                "query": "Elden Ring",
                "mode": "hide_dlc",
            },
        )
    )

    assert provider.last_query == "Elden Ring"
    assert provider.last_mode == SearchMode.HIDE_DLC

    assert result["count"] == 1
    assert result["results"][0]["gameId"] == 68151
    assert result["results"][0]["mainSeconds"] == 216306


@pytest.mark.asyncio
async def test_get_by_id():
    provider = FakeProvider()
    service = BridgeService(provider)

    result = await service.execute(
        Request(
            request_id=3,
            method="getById",
            params={
                "gameId": 68151,
            },
        )
    )

    assert provider.last_game_id == 68151
    assert result["game"]["name"] == "Elden Ring"


@pytest.mark.asyncio
async def test_get_by_id_not_found():
    service = BridgeService(FakeProvider())

    result = await service.execute(
        Request(
            request_id=4,
            method="getById",
            params={
                "gameId": 999999999,
            },
        )
    )

    assert result == {"game": None}


@pytest.mark.asyncio
async def test_invalid_search_mode():
    service = BridgeService(FakeProvider())

    with pytest.raises(ProtocolError) as exc:
        await service.execute(
            Request(
                request_id=5,
                method="search",
                params={
                    "query": "Elden Ring",
                    "mode": "banana",
                },
            )
        )

    assert exc.value.code == "invalid_params"


@pytest.mark.asyncio
async def test_unknown_method():
    service = BridgeService(FakeProvider())

    with pytest.raises(ProtocolError) as exc:
        await service.execute(
            Request(
                request_id=6,
                method="explode",
                params={},
            )
        )

    assert exc.value.code == "method_not_found"
