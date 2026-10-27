"""TCP endpoint that accepts Modbus connections but deliberately sends no response."""

import asyncio


async def hold_connection(reader: asyncio.StreamReader, writer: asyncio.StreamWriter) -> None:
    try:
        # Consume the request without responding.  The client closes the connection
        # once its one-second Modbus read timeout has elapsed.
        while await reader.read(1024):
            pass
    finally:
        writer.close()
        await writer.wait_closed()


async def main() -> None:
    server = await asyncio.start_server(hold_connection, host="0.0.0.0", port=502)
    async with server:
        await server.serve_forever()


if __name__ == "__main__":
    asyncio.run(main())
