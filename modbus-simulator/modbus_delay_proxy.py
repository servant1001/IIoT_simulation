"""Deterministic Modbus TCP proxy that injects response delay and jitter."""

import asyncio
import contextlib
import itertools
import logging


LISTEN_PORT = 502
UPSTREAM_HOST = "modbus-simulator"
UPSTREAM_PORT = 502
DELAY_SEQUENCE_MS = (150, 300, 450, 600, 750)
delays = itertools.cycle(DELAY_SEQUENCE_MS)

logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(message)s")


async def proxy_connection(client_reader: asyncio.StreamReader, client_writer: asyncio.StreamWriter) -> None:
    upstream_writer = None
    peer = client_writer.get_extra_info("peername")
    try:
        upstream_reader, upstream_writer = await asyncio.open_connection(UPSTREAM_HOST, UPSTREAM_PORT)
        while request := await client_reader.read(65536):
            upstream_writer.write(request)
            await upstream_writer.drain()
            response = await upstream_reader.read(65536)
            if not response:
                break

            delay_ms = next(delays)
            await asyncio.sleep(delay_ms / 1000)
            client_writer.write(response)
            await client_writer.drain()
            logging.info("Forwarded Modbus response to %s after %d ms delay", peer, delay_ms)
    except ConnectionError as error:
        logging.warning("Modbus delay proxy connection failed for %s: %s", peer, error)
    finally:
        client_writer.close()
        with contextlib.suppress(ConnectionError):
            await client_writer.wait_closed()
        if upstream_writer is not None:
            upstream_writer.close()
            with contextlib.suppress(ConnectionError):
                await upstream_writer.wait_closed()


async def main() -> None:
    server = await asyncio.start_server(proxy_connection, host="0.0.0.0", port=LISTEN_PORT)
    logging.info(
        "Modbus delay proxy listening on port %d; response delay sequence=%s ms",
        LISTEN_PORT,
        DELAY_SEQUENCE_MS,
    )
    async with server:
        await server.serve_forever()


if __name__ == "__main__":
    asyncio.run(main())
