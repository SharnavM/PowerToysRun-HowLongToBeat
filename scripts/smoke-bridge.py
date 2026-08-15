from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parent.parent

BRIDGE_EXE = ROOT / "artifacts" / "bridge" / "hltb-bridge" / "hltb-bridge.exe"


def fail(message: str) -> None:
    print(f"ERROR: {message}", file=sys.stderr)
    raise SystemExit(1)


def send(
    process: subprocess.Popen[str],
    request: dict[str, Any],
) -> dict[str, Any]:
    if process.stdin is None:
        fail("Bridge stdin is unavailable.")

    if process.stdout is None:
        fail("Bridge stdout is unavailable.")

    encoded = json.dumps(
        request,
        ensure_ascii=False,
        separators=(",", ":"),
    )

    process.stdin.write(encoded + "\n")
    process.stdin.flush()

    line = process.stdout.readline()

    if not line:
        stderr = ""

        if process.stderr is not None:
            stderr = process.stderr.read()

        fail(
            "Bridge exited without a response."
            + (f"\nBridge stderr:\n{stderr}" if stderr else "")
        )

    try:
        response = json.loads(line)
    except json.JSONDecodeError:
        fail(f"Bridge wrote non-JSON data to stdout:\n{line!r}")

    return response


def require_success(
    response: dict[str, Any],
    request_id: int,
) -> dict[str, Any]:
    if response.get("id") != request_id:
        fail(f"Expected response ID {request_id}, got {response.get('id')!r}.")

    if response.get("ok") is not True:
        fail(
            "Bridge returned an error:\n"
            + json.dumps(
                response,
                indent=2,
                ensure_ascii=False,
            )
        )

    result = response.get("result")

    if not isinstance(result, dict):
        fail("Response result is not an object.")

    return result


def main() -> None:
    if not BRIDGE_EXE.is_file():
        fail("Packaged bridge not found.\nRun scripts\\build-bridge.cmd first.")

    print(f"Testing: {BRIDGE_EXE}")

    creation_flags = 0

    if sys.platform == "win32":
        creation_flags = subprocess.CREATE_NO_WINDOW

    process = subprocess.Popen(
        [str(BRIDGE_EXE)],
        stdin=subprocess.PIPE,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
        encoding="utf-8",
        errors="replace",
        bufsize=1,
        creationflags=creation_flags,
    )

    try:
        print("[1/4] ping")

        ping = require_success(
            send(
                process,
                {
                    "id": 1,
                    "method": "ping",
                },
            ),
            1,
        )

        if ping.get("protocolVersion") != 1:
            fail("Unexpected protocol version.")

        print(
            "      bridge version:",
            ping.get("bridgeVersion"),
        )

        print("[2/4] live search")

        search = require_success(
            send(
                process,
                {
                    "id": 2,
                    "method": "search",
                    "params": {
                        "query": "Elden Ring",
                        "mode": "hide_dlc",
                    },
                },
            ),
            2,
        )

        results = search.get("results")

        if not isinstance(results, list) or not results:
            fail("Live search returned no results.")

        first = results[0]

        if first.get("gameId") != 68151:
            fail("Expected Elden Ring to be the first live search result.")

        if not isinstance(
            first.get("mainSeconds"),
            int,
        ):
            fail("Expected raw integer Main Story seconds.")

        print(
            "      result:",
            first.get("name"),
            "-",
            first.get("mainSeconds"),
            "seconds",
        )

        print("[3/4] ID lookup")

        lookup = require_success(
            send(
                process,
                {
                    "id": 3,
                    "method": "getById",
                    "params": {
                        "gameId": 68151,
                    },
                },
            ),
            3,
        )

        game = lookup.get("game")

        if not isinstance(game, dict) or game.get("gameId") != 68151:
            fail("ID lookup returned the wrong game.")

        print(
            "      result:",
            game.get("name"),
        )

        print("[4/4] shutdown")

        shutdown = require_success(
            send(
                process,
                {
                    "id": 4,
                    "method": "shutdown",
                },
            ),
            4,
        )

        if shutdown.get("shuttingDown") is not True:
            fail("Bridge rejected shutdown.")

        try:
            exit_code = process.wait(timeout=5)
        except subprocess.TimeoutExpired:
            process.kill()
            fail("Bridge did not exit after shutdown.")

        if exit_code != 0:
            fail(f"Bridge exited with code {exit_code}.")

        print()
        print("Packaged bridge smoke test passed.")

    finally:
        if process.poll() is None:
            process.kill()


if __name__ == "__main__":
    main()
