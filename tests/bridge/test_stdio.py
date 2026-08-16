from __future__ import annotations

import io

from hltb_bridge.stdio import _reconfigure_stream


def test_reconfigure_stream_uses_utf8() -> None:
    raw = io.BytesIO()

    stream = io.TextIOWrapper(
        raw,
        encoding="cp1252",
    )

    _reconfigure_stream(
        stream,
        errors="strict",
    )

    assert (
        stream.encoding.lower().replace(
            "-",
            "",
        )
        == "utf8"
    )

    stream.write("不祥的预感")

    stream.flush()

    raw.seek(0)

    assert raw.read().decode("utf-8") == "不祥的预感"
