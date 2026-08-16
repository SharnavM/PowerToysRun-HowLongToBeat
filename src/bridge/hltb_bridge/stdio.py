from __future__ import annotations

import sys
from typing import TextIO


def _reconfigure_stream(
    stream: TextIO | None,
    *,
    errors: str,
) -> None:
    if stream is None:
        return

    reconfigure = getattr(
        stream,
        "reconfigure",
        None,
    )

    if reconfigure is None:
        return

    reconfigure(
        encoding="utf-8",
        errors=errors,
    )


def configure_stdio() -> None:
    """
    Force UTF-8 for the bridge's text streams.

    The NDJSON protocol is UTF-8. On Windows, redirected
    standard streams may otherwise inherit a legacy locale
    encoding such as cp1252.
    """

    _reconfigure_stream(
        sys.stdin,
        errors="strict",
    )

    _reconfigure_stream(
        sys.stdout,
        errors="strict",
    )

    _reconfigure_stream(
        sys.stderr,
        errors="backslashreplace",
    )
