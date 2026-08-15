from __future__ import annotations

import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]

LOCK_FILE = ROOT / "src" / "bridge" / "requirements-lock.txt"


def normalize(lines: list[str]) -> set[str]:
    return {
        line.strip().lower()
        for line in lines
        if line.strip() and not line.lstrip().startswith("#")
    }


def main() -> int:
    if not LOCK_FILE.exists():
        print(
            f"ERROR: Lock file not found: {LOCK_FILE}",
            file=sys.stderr,
        )
        return 1

    expected = normalize(
        LOCK_FILE.read_text(
            encoding="utf-8",
        ).splitlines()
    )

    result = subprocess.run(
        [
            sys.executable,
            "-m",
            "pip",
            "freeze",
        ],
        check=True,
        capture_output=True,
        text=True,
    )

    actual = normalize(result.stdout.splitlines())

    missing = sorted(expected - actual)
    extra = sorted(actual - expected)

    if not missing and not extra:
        print("Python environment matches requirements-lock.txt.")
        return 0

    print("ERROR: Python environment does not match requirements-lock.txt.")

    if missing:
        print("\nMissing or mismatched:")
        for item in missing:
            print(f"  - {item}")

    if extra:
        print("\nUnexpected:")
        for item in extra:
            print(f"  + {item}")

    print("\nRecreate/update the environment using:")
    print("  pip install -r src\\bridge\\requirements-lock.txt")

    return 1


if __name__ == "__main__":
    raise SystemExit(main())
