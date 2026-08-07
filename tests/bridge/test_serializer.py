from types import SimpleNamespace

from hltb_bridge.serializer import (
    entry_to_game_result,
    game_result_to_dict,
)


def make_entry(**overrides):
    values = {
        "game_id": 68151,
        "game_name": "Elden Ring",
        "game_alias": "Elden Ring Tarnished Edition",
        "game_type": "game",
        "release_world": 2022,
        "profile_platforms": [
            "PC",
            "PlayStation 5",
            "Xbox Series X/S",
        ],
        "similarity": 1.0,
        "game_web_link": "https://howlongtobeat.com/game/68151",
        "game_image_url": "https://howlongtobeat.com/games/68151_Elden_Ring.jpg",
        "json_content": {
            "comp_main": 216306,
            "comp_plus": 364346,
            "comp_100": 489602,
            "comp_all": 379810,
        },
    }

    values.update(overrides)

    return SimpleNamespace(**values)


def test_entry_uses_raw_seconds():
    game = entry_to_game_result(make_entry())

    assert game.main_seconds == 216306
    assert game.main_extra_seconds == 364346
    assert game.completionist_seconds == 489602
    assert game.all_styles_seconds == 379810


def test_zero_values_become_none():
    entry = make_entry(
        game_alias="",
        release_world=0,
        json_content={
            "comp_main": 72000,
            "comp_plus": 102606,
            "comp_100": 0,
            "comp_all": 90454,
        },
    )

    game = entry_to_game_result(entry)

    assert game.alias is None
    assert game.release_year is None
    assert game.main_seconds == 72000
    assert game.completionist_seconds is None


def test_unknown_game_type_is_preserved():
    game = entry_to_game_result(make_entry(game_type="MOD"))

    assert game.game_type == "mod"


def test_platforms_are_cleaned_and_deduplicated():
    game = entry_to_game_result(
        make_entry(
            profile_platforms=[
                "PC",
                "PlayStation 5",
                "pc",
                "",
                None,
            ]
        )
    )

    assert game.platforms == [
        "PC",
        "PlayStation 5",
    ]


def test_wire_dictionary_uses_stable_field_names():
    game = entry_to_game_result(make_entry())

    data = game_result_to_dict(game)

    assert data["gameId"] == 68151
    assert data["name"] == "Elden Ring"
    assert data["releaseYear"] == 2022
    assert data["mainSeconds"] == 216306
    assert data["completionistSeconds"] == 489602
    assert data["type"] == "game"
