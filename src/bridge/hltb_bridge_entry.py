from __future__ import annotations

import asyncio
import sys
import traceback

from hltb_bridge.server import BridgeServer
from hltb_bridge.stdio import configure_stdio


async def main() -> None:
    server = BridgeServer()
    await server.run()


if __name__ == "__main__":
    try:
        configure_stdio()
        asyncio.run(main())
    except KeyboardInterrupt:
        pass
    except Exception:
        traceback.print_exc(file=sys.stderr)
        raise
