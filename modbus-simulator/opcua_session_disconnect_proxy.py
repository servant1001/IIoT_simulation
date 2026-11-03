"""TCP proxy that closes an established OPC UA transport after a short delay."""

import asyncio
import contextlib
import logging


LISTEN_PORT = 53532
UPSTREAM_HOST = "host.docker.internal"
UPSTREAM_PORT = 53530
CLOSE_AFTER_SECONDS = 1.5

logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(message)s")


async def copy_stream(reader: asyncio.StreamReader, writer: asyncio.StreamWriter) -> None:
    try:
        while data := await reader.read(65536):
            writer.write(data)
            await writer.drain()
    except (ConnectionError, asyncio.IncompleteReadError):
        pass


async def proxy_connection(client_reader: asyncio.StreamReader, client_writer: asyncio.StreamWriter) -> None:
    peer = client_writer.get_extra_info("peername")
    upstream_writer = None
    try:
        upstream_reader, upstream_writer = await asyncio.open_connection(UPSTREAM_HOST, UPSTREAM_PORT)
        logging.info("Forwarding OPC UA client %s; transport will close after %.1f seconds", peer, CLOSE_AFTER_SECONDS)
        client_to_upstream = asyncio.create_task(copy_stream(client_reader, upstream_writer))
        upstream_to_client = asyncio.create_task(copy_stream(upstream_reader, client_writer))
        await asyncio.sleep(CLOSE_AFTER_SECONDS)
        logging.info("Closing established OPC UA transport for %s", peer)
        client_to_upstream.cancel()
        upstream_to_client.cancel()
        await asyncio.gather(client_to_upstream, upstream_to_client, return_exceptions=True)
    except ConnectionError as error:
        logging.warning("Cannot connect OPC UA upstream for %s: %s", peer, error)
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
    logging.info("OPC UA session disconnect proxy listening on port %d", LISTEN_PORT)
    async with server:
        await server.serve_forever()


if __name__ == "__main__":
    asyncio.run(main())
