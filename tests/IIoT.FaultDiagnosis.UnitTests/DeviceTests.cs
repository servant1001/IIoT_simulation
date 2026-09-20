using IIoT.FaultDiagnosis.Domain.Entities;
using IIoT.FaultDiagnosis.Domain.Enums;
using Xunit;

namespace IIoT.FaultDiagnosis.UnitTests;

public sealed class DeviceTests
{
    [Fact]
    public void Create_TrimsValuesAndSetsUtcTimestamps()
    {
        var now = new DateTimeOffset(2026, 9, 12, 13, 0, 0, TimeSpan.Zero);

        var device = new Device(Guid.NewGuid(), "  Machine 01  ", ProtocolType.ModbusTcp, " 127.0.0.1 ", 502, true, now);

        Assert.Equal("Machine 01", device.Name);
        Assert.Equal("127.0.0.1", device.Host);
        Assert.Equal(now, device.CreatedAt);
        Assert.Equal(now, device.UpdatedAt);
        Assert.Equal(TimeSpan.Zero, device.CreatedAt.Offset);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(65536)]
    public void Create_RejectsPortsOutsideTcpRange(int port)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Device(Guid.NewGuid(), "Machine 01", ProtocolType.ModbusTcp, "127.0.0.1", port, true, DateTimeOffset.UtcNow));
    }
}
