from __future__ import annotations

from typing import Any

from .models import GameResult


def _positive_int_or_none(value: Any) -> int | None:
    if value is None or isinstance(value, bool):
        return None

    try:
        result = int(value)
    except (TypeError, ValueError):
        return None

    return result if result > 0 else None


def _string_or_none(value: Any) -> str | None:
    if value is None:
        return None

    result = str(value).strip()
    return result if result else None


def _normalize_game_type(value: Any) -> str | None:
    result = _string_or_none(value)

    if result is None:
        return None

    return result.lower()


def _normalize_platforms(value: Any) -> list[str]:
    if not isinstance(value, (list, tuple)):
        return []

    result: list[str] = []
    seen: set[str] = set()

    for platform in value:
        normalized = _string_or_none(platform)

        if normalized is None:
            continue

        key = normalized.casefold()

        if key in seen:
            continue

        seen.add(key)
        result.append(normalized)

    return result


def entry_to_game_result(entry: Any) -> GameResult:
    raw = getattr(entry, "json_content", None)

    if not isinstance(raw, dict):
        raw = {}

    return GameResult(
        game_id=int(entry.game_id),
        name=str(entry.game_name).strip(),
        alias=_string_or_none(getattr(entry, "game_alias", None)),
        game_type=_normalize_game_type(getattr(entry, "game_type", None)),
        release_year=_positive_int_or_none(getattr(entry, "release_world", None)),
        platforms=_normalize_platforms(getattr(entry, "profile_platforms", None)),
        main_seconds=_positive_int_or_none(raw.get("comp_main")),
        main_extra_seconds=_positive_int_or_none(raw.get("comp_plus")),
        completionist_seconds=_positive_int_or_none(raw.get("comp_100")),
        all_styles_seconds=_positive_int_or_none(raw.get("comp_all")),
        similarity=float(getattr(entry, "similarity", 0.0) or 0.0),
        url=_string_or_none(getattr(entry, "game_web_link", None)),
        image_url=_string_or_none(getattr(entry, "game_image_url", None)),
    )


def game_result_to_dict(game: GameResult) -> dict[str, Any]:
    return {
        "gameId": game.game_id,
        "name": game.name,
        "alias": game.alias,
        "type": game.game_type,
        "releaseYear": game.release_year,
        "platforms": game.platforms,
        "mainSeconds": game.main_seconds,
        "mainExtraSeconds": game.main_extra_seconds,
        "completionistSeconds": game.completionist_seconds,
        "allStylesSeconds": game.all_styles_seconds,
        "similarity": game.similarity,
        "url": game.url,
        "imageUrl": game.image_url,
    }
