FROM python:3.11-slim

WORKDIR /app

RUN pip install --no-cache-dir pymodbus==3.6.9

COPY *.py .

CMD ["python", "server.py"]
