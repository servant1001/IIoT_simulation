"""TCP endpoint that accepts a Modbus connection then sends a TCP reset."""

import asyncio
import socket
import struct


async def reset_connection(reader: asyncio.StreamReader, writer: asyncio.StreamWriter) -> None:
    del reader
    transport = writer.transport
    connection = writer.get_extra_info("socket")
    try:
        if connection is not None:
            # Linger timeout zero makes close send RST instead of a graceful FIN.
            connection.setsockopt(socket.SOL_SOCKET, socket.SO_LINGER, struct.pack("ii", 1, 0))
    finally:
        transport.abort()


async def main() -> None:
    server = await asyncio.start_server(reset_connection, host="0.0.0.0", port=502)
    async with server:
        await server.serve_forever()


if __name__ == "__main__":
    asyncio.run(main())
