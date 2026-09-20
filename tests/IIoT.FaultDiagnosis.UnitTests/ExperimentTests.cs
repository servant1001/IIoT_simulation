using IIoT.FaultDiagnosis.Domain.Entities;
using IIoT.FaultDiagnosis.Domain.Enums;
using Xunit;

namespace IIoT.FaultDiagnosis.UnitTests;

public sealed class ExperimentTests
{
    [Fact]
    public void Lifecycle_TracksRunAndExperimentStatus()
    {
        var now = DateTimeOffset.UtcNow;
        var experiment = new Experiment(Guid.NewGuid(), "NORMAL", ProtocolType.ModbusTcp, FaultType.None, 1, 6, 1000, 10, 2, now);
        experiment.Start();
        var run = new ExperimentRun(Guid.NewGuid(), experiment.Id, 1, now);
        run.Complete(10, 0, 1.2, 2.0, 0.8, 5.1m, 45m, now.AddSeconds(10), ExperimentStatus.Completed);
        experiment.Complete();

        Assert.Equal(ExperimentStatus.Completed, experiment.Status);
        Assert.Equal(10, run.SuccessCount);
        Assert.Equal(ExperimentStatus.Completed, run.Status);
    }

    [Fact]
    public void Create_RejectsInvalidExperimentConfiguration()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Experiment(Guid.NewGuid(), "bad", ProtocolType.ModbusTcp, FaultType.None, 0, 1, 1000, 1, 1, DateTimeOffset.UtcNow));
    }
}
