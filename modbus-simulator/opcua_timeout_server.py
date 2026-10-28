"""OPC UA TCP endpoint that accepts connections but deliberately sends no response."""

import asyncio


async def hold_connection(reader: asyncio.StreamReader, writer: asyncio.StreamWriter) -> None:
    try:
        # Consume the OPC UA hello/request bytes without responding. The OPC UA client
        # reaches its configured operation timeout while waiting for an endpoint reply.
        while await reader.read(1024):
            pass
    finally:
        writer.close()
        await writer.wait_closed()


async def main() -> None:
    server = await asyncio.start_server(hold_connection, host="0.0.0.0", port=53531)
    async with server:
        await server.serve_forever()


if __name__ == "__main__":
    asyncio.run(main())
