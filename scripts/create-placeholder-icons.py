from __future__ import annotations

import struct
import zlib
from pathlib import Path


ROOT = Path(__file__).resolve().parent.parent

IMAGE_DIR = (
    ROOT / "src" / "plugin" / "Community.PowerToys.Run.Plugin.HowLongToBeat" / "Images"
)


def chunk(kind: bytes, data: bytes) -> bytes:
    checksum = zlib.crc32(kind + data) & 0xFFFFFFFF

    return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", checksum)


def write_icon(path: Path) -> None:
    width = 64
    height = 64

    rows: list[bytes] = []

    for y in range(height):
        row = bytearray()

        for x in range(width):
            left = 14 <= x <= 21 and 12 <= y <= 51
            right = 42 <= x <= 49 and 12 <= y <= 51
            middle = 14 <= x <= 49 and 28 <= y <= 35

            if left or right or middle:
                row.extend((128, 128, 128, 255))
            else:
                row.extend((0, 0, 0, 0))

        rows.append(b"\x00" + bytes(row))

    raw = b"".join(rows)

    png = b"\x89PNG\r\n\x1a\n"

    png += chunk(
        b"IHDR",
        struct.pack(
            ">IIBBBBB",
            width,
            height,
            8,
            6,
            0,
            0,
            0,
        ),
    )

    png += chunk(
        b"IDAT",
        zlib.compress(raw, level=9),
    )

    png += chunk(
        b"IEND",
        b"",
    )

    path.write_bytes(png)


def main() -> None:
    IMAGE_DIR.mkdir(
        parents=True,
        exist_ok=True,
    )

    write_icon(IMAGE_DIR / "howlongtobeat.dark.png")

    write_icon(IMAGE_DIR / "howlongtobeat.light.png")

    print("Placeholder icons created.")


if __name__ == "__main__":
    main()
