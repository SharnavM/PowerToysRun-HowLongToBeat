from howlongtobeatpy import SearchModifiers

from hltb_bridge.provider import (
    SearchMode,
    search_modifier_for,
)


def test_all_maps_to_no_modifier():
    assert search_modifier_for(SearchMode.ALL) == SearchModifiers.NONE


def test_hide_dlc_maps_to_library_filter():
    assert search_modifier_for(SearchMode.HIDE_DLC) == SearchModifiers.HIDE_DLC


def test_dlc_only_maps_to_library_filter():
    assert search_modifier_for(SearchMode.DLC_ONLY) == SearchModifiers.ISOLATE_DLC
