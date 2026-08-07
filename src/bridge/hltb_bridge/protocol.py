from __future__ import annotations

import json
from dataclasses import dataclass
from typing import Any


MAX_REQUEST_LENGTH = 64 * 1024


@dataclass(frozen=True, slots=True)
class Request:
    request_id: int
    method: str
    params: dict[str, Any]


class ProtocolError(ValueError):
    def __init__(
        self,
        code: str,
        message: str,
        request_id: int | None = None,
    ) -> None:
        super().__init__(message)

        self.code = code
        self.message = message
        self.request_id = request_id


def parse_request(line: str) -> Request:
    if not line:
        raise ProtocolError(
            "invalid_request",
            "Request must not be empty.",
        )

    if len(line) > MAX_REQUEST_LENGTH:
        raise ProtocolError(
            "request_too_large",
            "Request exceeds the maximum allowed length.",
        )

    try:
        payload = json.loads(line)
    except json.JSONDecodeError as exc:
        raise ProtocolError(
            "invalid_json",
            f"Invalid JSON: {exc.msg}",
        ) from exc

    if not isinstance(payload, dict):
        raise ProtocolError(
            "invalid_request",
            "Request must be a JSON object.",
        )

    request_id = payload.get("id")

    if (
        isinstance(request_id, bool)
        or not isinstance(request_id, int)
        or request_id < 0
    ):
        raise ProtocolError(
            "invalid_request",
            "Request 'id' must be a non-negative integer.",
        )

    method = payload.get("method")

    if not isinstance(method, str) or not method.strip():
        raise ProtocolError(
            "invalid_request",
            "Request 'method' must be a non-empty string.",
            request_id,
        )

    params = payload.get("params", {})

    if not isinstance(params, dict):
        raise ProtocolError(
            "invalid_request",
            "Request 'params' must be a JSON object.",
            request_id,
        )

    return Request(
        request_id=request_id,
        method=method.strip(),
        params=params,
    )


def success_response(
    request_id: int,
    result: Any,
) -> dict[str, Any]:
    return {
        "id": request_id,
        "ok": True,
        "result": result,
    }


def error_response(
    request_id: int | None,
    code: str,
    message: str,
) -> dict[str, Any]:
    return {
        "id": request_id,
        "ok": False,
        "error": {
            "code": code,
            "message": message,
        },
    }


def encode_message(message: dict[str, Any]) -> str:
    return json.dumps(
        message,
        ensure_ascii=False,
        separators=(",", ":"),
    )
