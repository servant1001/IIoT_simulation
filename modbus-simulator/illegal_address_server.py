"""Minimal Modbus TCP server that returns exception code 02 for every request."""

import asyncio


async def respond_with_illegal_address(
    reader: asyncio.StreamReader, writer: asyncio.StreamWriter
) -> None:
    try:
        while True:
            header = await reader.readexactly(7)
            message_length = int.from_bytes(header[4:6], byteorder="big")
            pdu = await reader.readexactly(message_length - 1)
            function_code = pdu[0]

            # MBAP header + unit id + exception function code + exception code 02.
            response = (
                header[:4]
                + (3).to_bytes(2, byteorder="big")
                + header[6:7]
                + bytes([function_code | 0x80, 0x02])
            )
            writer.write(response)
            await writer.drain()
    except asyncio.IncompleteReadError:
        pass
    finally:
        writer.close()
        await writer.wait_closed()


async def main() -> None:
    server = await asyncio.start_server(respond_with_illegal_address, host="0.0.0.0", port=502)
    async with server:
        await server.serve_forever()


if __name__ == "__main__":
    asyncio.run(main())
