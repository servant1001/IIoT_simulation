"""Deterministic Modbus TCP simulator for IIoT Fault Diagnosis experiments."""

from pymodbus.datastore import ModbusSequentialDataBlock, ModbusServerContext, ModbusSlaveContext
from pymodbus.server import StartTcpServer


def main() -> None:
    # Registers 0-5: temperature, pressure, RPM, current, production count, status.
    holding_registers = ModbusSequentialDataBlock(0, [562, 805, 2230, 261, 12345, 1])
    slave = ModbusSlaveContext(hr=holding_registers, zero_mode=True)
    context = ModbusServerContext(slaves=slave, single=True)
    StartTcpServer(context=context, address=("0.0.0.0", 502))


if __name__ == "__main__":
    main()
