"""Minimal MQTT 5 endpoint that rejects every subscription with NotAuthorized."""

import asyncio
import logging


logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(message)s")


async def read_remaining_length(reader: asyncio.StreamReader) -> int:
    multiplier = 1
    value = 0
    while True:
        encoded = (await reader.readexactly(1))[0]
        value += (encoded & 127) * multiplier
        if not encoded & 128:
            return value
        multiplier *= 128
        if multiplier > 128 * 128 * 128:
            raise ValueError("Malformed MQTT remaining length")


async def read_packet(reader: asyncio.StreamReader) -> tuple[int, bytes]:
    header = (await reader.readexactly(1))[0]
    remaining_length = await read_remaining_length(reader)
    return header, await reader.readexactly(remaining_length)


async def reject_subscriptions(reader: asyncio.StreamReader, writer: asyncio.StreamWriter) -> None:
    peer = writer.get_extra_info("peername")
    try:
        header, _ = await read_packet(reader)
        if header >> 4 != 1:
            logging.warning("Rejected non-CONNECT first packet from %s", peer)
            return

        # MQTT 5 CONNACK: Session Present=false, Success, no properties.
        writer.write(b"\x20\x03\x00\x00\x00")
        await writer.drain()

        while True:
            header, payload = await read_packet(reader)
            packet_type = header >> 4
            if packet_type == 8:  # SUBSCRIBE
                packet_identifier = payload[:2]
                # MQTT 5 SUBACK: no properties, reason code 0x87 = Not authorized.
                writer.write(b"\x90\x04" + packet_identifier + b"\x00\x87")
                await writer.drain()
                logging.info("Rejected MQTT subscription from %s with NotAuthorized", peer)
            elif packet_type == 12:  # PINGREQ
                writer.write(b"\xd0\x00")
                await writer.drain()
            elif packet_type == 14:  # DISCONNECT
                return
    except asyncio.IncompleteReadError:
        logging.info("MQTT client %s disconnected", peer)
    finally:
        writer.close()
        await writer.wait_closed()


async def main() -> None:
    server = await asyncio.start_server(reject_subscriptions, host="0.0.0.0", port=1885)
    logging.info("MQTT topic rejection simulator listening on port 1885")
    async with server:
        await server.serve_forever()


if __name__ == "__main__":
    asyncio.run(main())
