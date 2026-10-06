using Npgsql;
using SmartFactory.BuildingBlocks.DashboardAccess;
namespace SmartFactory.Services.AnalyticsService;

public sealed record CoverageRequest(string[] AssetIds, long StartTime, long EndTime);
public sealed record MetricCoverage(string Metric, long Samples, double? Average, double? Minimum, double? Maximum);
public sealed record AssetCoverage(string AssetId, long Samples, long FirstAt, long LastAt, long? LongestGapMs, List<MetricCoverage> Metrics);
public sealed class CoverageAnalytics(DashboardStore store)
{
    public static bool Valid(CoverageRequest request) => request.AssetIds is { Length: > 0 and <= 200 }
        && request.AssetIds.All(id => !string.IsNullOrWhiteSpace(id) && id.Length <= 128 && !id.Any(char.IsControl))
        && request.StartTime >= 0 && request.EndTime > request.StartTime && request.EndTime - request.StartTime <= 93L * 86400000;
    public async Task<List<AssetCoverage>> Query(CoverageRequest request, CancellationToken ct)
    {
        // SQL aggregation scans the indexed range without loading each sample into the API process.
        const string sql = """
        WITH samples AS (
          SELECT *, "timestamp" - lag("timestamp") OVER (PARTITION BY "assetId" ORDER BY "timestamp", id) AS gap
          FROM sensor_readings WHERE "assetId" = ANY($1) AND "timestamp" >= $2 AND "timestamp" <= $3
        )
        SELECT "assetId", count(*), min("timestamp"), max("timestamp"), max(gap),
          count(temperature), avg(temperature), min(temperature), max(temperature),
          count(humidity), avg(humidity), min(humidity), max(humidity),
          count(vibration), avg(vibration), min(vibration), max(vibration),
          count(power), avg(power), min(power), max(power),
          count(pressure), avg(pressure), min(pressure), max(pressure),
          count(rpm), avg(rpm), min(rpm), max(rpm)
        FROM samples GROUP BY "assetId" ORDER BY "assetId"
        """;
        await using var command = store.Source.CreateCommand(sql);
        command.Parameters.AddWithValue(request.AssetIds.Distinct().ToArray());
        command.Parameters.AddWithValue(request.StartTime);
        command.Parameters.AddWithValue(request.EndTime);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var output = new List<AssetCoverage>();
        while (await reader.ReadAsync(ct)) {
            var metrics = new List<MetricCoverage>();
            var names = new[] { "temperature", "humidity", "vibration", "power", "pressure", "rpm" };
            for (var i = 0; i < names.Length; i++) {
                var column = 5 + i * 4;
                double? Number(int index) => reader.IsDBNull(index) ? null : Convert.ToDouble(reader.GetValue(index));
                metrics.Add(new(names[i], reader.GetInt64(column), Number(column + 1), Number(column + 2), Number(column + 3)));
            }
            output.Add(new(reader.GetString(0), reader.GetInt64(1), reader.GetInt64(2), reader.GetInt64(3),
                reader.IsDBNull(4) ? null : reader.GetInt64(4), metrics));
        }
        return output;
    }
}
