using System.Globalization;
using System.Text;

namespace IIoT.FaultDiagnosis.Application.Datasets;

public sealed class DatasetService(IDatasetRepository repository) : IDatasetService
{
    private const string Header = "dataset_record_id,experiment_id,experiment_run_id,run_number,device_id,protocol,actual_fault_type,predicted_fault_type,diagnosis_method,diagnosis_confidence,started_at,completed_at,response_time_ms,status,observed_fault_type,error_code,error_message,retry_count,raw_request,raw_response,parsed_value";

    public async Task<DatasetExport> ExportAsync(DatasetQuery query, CancellationToken cancellationToken)
    {
        var records = await repository.GetRecordsAsync(cancellationToken);
        var filtered = records
            .Where(record => query.ProtocolType is null || record.ProtocolType == query.ProtocolType)
            .Where(record => query.ActualFaultType is null || record.ActualFaultType == query.ActualFaultType)
            .Where(record => !query.OnlyDiagnosed || record.PredictedFaultType is not null)
            .OrderBy(record => record.StartedAt)
            .ThenBy(record => record.CommunicationRecordId);

        var builder = new StringBuilder().AppendLine(Header);
        foreach (var record in filtered)
        {
            builder.AppendLine(string.Join(',',
                Csv(record.CommunicationRecordId), Csv(record.ExperimentId), Csv(record.ExperimentRunId), Csv(record.RunNumber),
                Csv(record.DeviceId), Csv(record.ProtocolType), Csv(record.ActualFaultType), Csv(record.PredictedFaultType),
                Csv(record.DiagnosisMethod), Csv(record.DiagnosisConfidence), Csv(record.StartedAt), Csv(record.CompletedAt),
                Csv(record.ResponseTimeMs), Csv(record.Status), Csv(record.ObservedFaultType), Csv(record.ErrorCode),
                Csv(record.ErrorMessage), Csv(record.RetryCount), Csv(record.RawRequest), Csv(record.RawResponse), Csv(record.ParsedValue)));
        }

        return new DatasetExport($"iiot-fault-dataset-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.csv", Encoding.UTF8.GetBytes(builder.ToString()));
    }

    public async Task<DatasetSummary> GetSummaryAsync(int minimumRunsPerFault, CancellationToken cancellationToken)
    {
        if (minimumRunsPerFault < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumRunsPerFault), "Minimum runs per fault must be at least 1.");
        }

        var records = await repository.GetRecordsAsync(cancellationToken);
        var coverage = records
            .GroupBy(record => new { record.ProtocolType, record.ActualFaultType })
            .OrderBy(group => group.Key.ProtocolType)
            .ThenBy(group => group.Key.ActualFaultType)
            .Select(group =>
            {
                var runCount = group.Where(record => record.ExperimentRunId is not null).Select(record => record.ExperimentRunId).Distinct().Count();
                return new DatasetCoverage(
                    group.Key.ProtocolType,
                    group.Key.ActualFaultType,
                    runCount,
                    group.Count(),
                    group.Count(record => record.PredictedFaultType is not null),
                    minimumRunsPerFault,
                    Math.Max(0, minimumRunsPerFault - runCount));
            })
            .ToList();

        return new DatasetSummary(records.Count, records.Select(record => record.ExperimentId).Distinct().Count(), coverage);
    }

    private static string Csv(object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var text = value switch
        {
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };

        return $"\"{text.Replace("\"", "\"\"")}\"";
    }
}
