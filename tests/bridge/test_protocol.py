import json

import pytest

from hltb_bridge.protocol import (
    ProtocolError,
    encode_message,
    error_response,
    parse_request,
    success_response,
)


def test_parse_valid_request():
    request = parse_request(
        '{"id":7,"method":"search","params":{"query":"Elden Ring"}}'
    )

    assert request.request_id == 7
    assert request.method == "search"
    assert request.params == {"query": "Elden Ring"}


def test_params_default_to_empty_object():
    request = parse_request('{"id":1,"method":"ping"}')

    assert request.params == {}


def test_invalid_json_is_rejected():
    with pytest.raises(ProtocolError) as exc:
        parse_request("{bad json")

    assert exc.value.code == "invalid_json"


def test_boolean_id_is_rejected():
    with pytest.raises(ProtocolError):
        parse_request('{"id":true,"method":"ping"}')


def test_non_object_params_are_rejected():
    with pytest.raises(ProtocolError) as exc:
        parse_request('{"id":1,"method":"search","params":[]}')

    assert exc.value.code == "invalid_request"


def test_success_response_shape():
    response = success_response(
        4,
        {"value": 42},
    )

    assert response == {
        "id": 4,
        "ok": True,
        "result": {
            "value": 42,
        },
    }


def test_error_response_shape():
    response = error_response(
        5,
        "test_error",
        "Something failed.",
    )

    assert response["id"] == 5
    assert response["ok"] is False
    assert response["error"]["code"] == "test_error"


def test_encoded_message_is_one_line_json():
    encoded = encode_message(
        success_response(
            1,
            {"name": "Pokémon"},
        )
    )

    assert "\n" not in encoded
    assert json.loads(encoded)["result"]["name"] == "Pokémon"
