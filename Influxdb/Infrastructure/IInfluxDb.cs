using InfluxDB.Client.Core.Flux.Domain;
using InfluxDB.Client.Writes;
using Influxdb.Models;

namespace Influxdb.Infrastructure
{
    public interface IInfluxDb
    {
        void WritePoint(PointData point, string? bucket);
        bool WriteMeasurement<T>(T measurement, string bucket);
        bool WriteMeasurements<T>(List<T> measurementList, string bucket);
        Task<List<T>> FetchDataLinq<T>(string bucket, string macAddress, string timeRange, string? method = null, double? aggregateSec = null)
            where T : BasicPoco;
        Task<List<FluxTable>?> FetchDataSimple(string bucket, string timeRange, string measurement);
    }
}
