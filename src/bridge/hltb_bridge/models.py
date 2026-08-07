from __future__ import annotations

from dataclasses import dataclass


@dataclass(frozen=True, slots=True)
class GameResult:
    game_id: int
    name: str
    alias: str | None
    game_type: str | None
    release_year: int | None
    platforms: list[str]

    main_seconds: int | None
    main_extra_seconds: int | None
    completionist_seconds: int | None
    all_styles_seconds: int | None

    similarity: float

    url: str | None
    image_url: str | None
