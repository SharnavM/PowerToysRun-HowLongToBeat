from __future__ import annotations

import argparse
import re
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
VERSION_FILE = ROOT / "VERSION"

TARGETS = [
    (
        ROOT / "Directory.Build.props",
        re.compile(r"(<PluginVersion>)([^<]+)(</PluginVersion>)"),
    ),
    (
        ROOT
        / "src"
        / "plugin"
        / "Community.PowerToys.Run.Plugin.HowLongToBeat"
        / "plugin.json",
        re.compile(r'("Version"\s*:\s*")([^"]+)(")'),
    ),
    (
        ROOT / "src" / "bridge" / "hltb_bridge" / "__init__.py",
        re.compile(r'(__version__\s*=\s*")([^"]+)(")'),
    ),
]


def read_version() -> str:
    version = VERSION_FILE.read_text(encoding="utf-8").strip()

    if not re.fullmatch(
        r"\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?",
        version,
    ):
        raise SystemExit(f"Invalid VERSION value: {version!r}")

    return version


def process_target(
    path: Path,
    pattern: re.Pattern[str],
    version: str,
    check_only: bool,
) -> bool:
    text = path.read_text(encoding="utf-8")

    match = pattern.search(text)

    if match is None:
        raise SystemExit(f"Could not locate version field in {path}")

    current = match.group(2)

    if current == version:
        print(f"OK   {path.relative_to(ROOT)} = {version}")
        return True

    if check_only:
        print(f"FAIL {path.relative_to(ROOT)} = {current}; expected {version}")
        return False

    updated = pattern.sub(
        rf"\g<1>{version}\g<3>",
        text,
        count=1,
    )

    path.write_text(
        updated,
        encoding="utf-8",
    )

    print(f"SYNC {path.relative_to(ROOT)} {current} -> {version}")

    return True


def main() -> int:
    parser = argparse.ArgumentParser()

    parser.add_argument(
        "--check",
        action="store_true",
        help="Check version consistency without modifying files.",
    )

    args = parser.parse_args()

    version = read_version()

    print(f"Project version: {version}")

    success = True

    for path, pattern in TARGETS:
        success = (
            process_target(
                path,
                pattern,
                version,
                args.check,
            )
            and success
        )

    return 0 if success else 1


if __name__ == "__main__":
    raise SystemExit(main())
