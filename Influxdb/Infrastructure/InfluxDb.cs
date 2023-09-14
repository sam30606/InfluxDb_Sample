using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core.Flux.Domain;
using InfluxDB.Client.Linq;
using InfluxDB.Client.Writes;
using Microsoft.Extensions.Options;
using Influxdb.Models;
using System.Diagnostics;

namespace Influxdb.Infrastructure
{
    public class InfluxDb : IInfluxDb
    {
        private readonly InfluxDBClient _client;
        private readonly string _org;
        private readonly Configurations configurations;
        private readonly WriteOptions writeOptions;
        public InfluxDb(IOptions<Configurations> options)
        {
            configurations = options.Value;
            _client = new InfluxDBClient(configurations.InfluxDbConfig.Url, configurations.InfluxDbConfig.Token);
            _org = configurations.InfluxDbConfig.Org;
            writeOptions = new WriteOptions
            {
                BatchSize = 5000,
                FlushInterval = 1000,
                JitterInterval = 1000,
                RetryInterval = 5000,
            };

        }
        public  void WritePoint<T>(PointData point, string? bucket)
        {
            try
            {
                using WriteApi? writeApi = _client.GetWriteApi();
                writeApi.WritePoint(point, bucket, _org);
            }
            catch (Exception)
            {
                Debug.WriteLine("Failed to write point.");
            }
        }


        public void WriteLineProtocol(string line, string bucket)
        {
            using WriteApi? writeApi = _client.GetWriteApi();
            try
            {
                writeApi.WriteRecord(line, WritePrecision.Ns, bucket, _org);
            }
            catch (Exception ex)
            {
                Console.WriteLine("InfluxDL - WriteData()", ex);
            }

        }
        public bool WriteMeasurement<T>(T measurement, string bucket)
        {
            try
            {
                WriteApi? writeApi = _client.GetWriteApi(writeOptions);
                 writeApi.WriteMeasurement( measurement, WritePrecision.Ns, bucket, _org);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public bool WriteMeasurements<T>(List<T> measurementList, string bucket)
        {
            try
            {
                using WriteApi? writeApi = _client.GetWriteApi();
                writeApi.WriteMeasurements(measurementList, WritePrecision.Ms, bucket, _org);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

        }
        public Task<List<T>> FetchDataLinq<T>(string bucket, string macAddress, string timeRange, string? method = null, double? aggregateSec = null)
            where T : BasicPoco
        {
            DateTime timestamp;
            var settings = new QueryableOptimizerSettings
            {
                DropMeasurementColumn = false,
                AlignFieldsWithPivot = true
            };

            switch (timeRange[^1])
            {
                case 's':
                    int seconds = Convert.ToInt32(timeRange.Remove(timeRange.Length - 1));
                    timestamp = DateTime.UtcNow.AddSeconds(-seconds).ToLocalTime();
                    break;
                case 'm':
                    int minutes = Convert.ToInt32(timeRange.Remove(timeRange.Length - 1));
                    timestamp = DateTime.UtcNow.AddMinutes(-minutes).ToLocalTime();
                    break;
                case 'h':
                    int hours = Convert.ToInt32(timeRange.Remove(timeRange.Length - 1));
                    timestamp = DateTime.UtcNow.AddHours(-hours).ToLocalTime();

                    break;
                case 'd':
                    int days = Convert.ToInt32(timeRange.Remove(timeRange.Length - 1));
                    timestamp = DateTime.UtcNow.AddDays(-days).ToLocalTime();

                    break;
                default:
                    timestamp = DateTime.UtcNow.AddDays(-1).ToLocalTime();
                    break;
            }

            if (method != null && method.Length > 0 && aggregateSec > 0)
            {
                try
                {
                    IOrderedQueryable<T> query = from s in InfluxDBQueryable<T>.Queryable(bucket,
                            _org, _client.GetQueryApiSync(), settings)
                        where s.sensor_id == macAddress
                        where s.Timestamp > timestamp
                        where s.Timestamp.AggregateWindow(TimeSpan.FromSeconds(aggregateSec.Value), null, method)
                        orderby s.Timestamp
                        select s;
                    return Task.FromResult(query.ToList());
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to load data. - " + e);
                    return Task.FromResult(new List<T>());
                }
            }
            try
            {

                IOrderedQueryable<T> query = from s in InfluxDBQueryable<T>.Queryable(bucket,
                        _org, _client.GetQueryApiSync(), settings)
                    where s.sensor_id == macAddress
                    where s.Timestamp > timestamp
                    orderby s.Timestamp
                    select s;
                return Task.FromResult(query.ToList());
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to load data. - " + e);
                return Task.FromResult(new List<T>());
            }

        }
        public async Task<List<FluxTable>?> FetchDataSimple(string bucket, string timeRange, string measurement)
        {
            try
            {
                string fluxQuery = $"from(bucket: \"{bucket}\")"
                    + $" |> range(start: -{timeRange})"
                    + $" |> filter(fn: (r) => (r[\"_measurement\"] == \"{measurement}\"))";

                return await _client.GetQueryApi().QueryAsync(fluxQuery, _org);
            }
            catch (Exception)
            {
                Debug.WriteLine("Failed to load data.");
                return null;
            }
        }

        public async Task<List<FluxTable?>> FetchData(string? bucket, string? deviceId, string timeRange,
            string measurement, string field)
        {
            try
            {
                string aggregate = "1s";

                switch (timeRange)
                {
                    case "1h":
                        aggregate = "30s";
                        break;
                    case "6h":
                    case "1d":
                        aggregate = "5m";
                        break;
                    case "3d":
                    case "7d":
                        aggregate = "30m";
                        break;
                    case "30d":
                        aggregate = "2h";
                        break;
                }

                string fluxQuery = $"from(bucket: \"{bucket}\")"
                    + $" |> range(start: -{timeRange})"
                    + $" |> filter(fn: (r) => (r[\"_measurement\"] == \"{measurement}\"))"
                    + $" |> filter(fn: (r) => (r.clientId == \"{deviceId}\"))"
                    + $" |> filter(fn: (r) => (r[\"_field\"] == \"{field}\"))"
                    + " |> keep(columns: [\"_value\", \"_time\", \"_field\", \"clientId\"])"
                    + $" |> aggregateWindow(column: \"_value\", every: {aggregate}, fn: mean)";

                return await _client.GetQueryApi().QueryAsync(fluxQuery, _org);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Failed to load data. - " + e);
                return new List<FluxTable?>();
            }
        }

        public async Task<FluxTable?> FetchDataMean(string? bucket, string? deviceId, string timeRange,
            string measurement, string field)
        {
            try
            {
                string fluxQuery = $"from(bucket: \"{bucket}\")"
                    + $" |> range(start: -{timeRange})"
                    + $" |> filter(fn: (r) => (r[\"_measurement\"] == \"{measurement}\"))"
                    + $" |> filter(fn: (r) => (r.clientId == \"{deviceId}\"))"
                    + $" |> filter(fn: (r) => (r[\"_field\"] == \"{field}\"))"
                    + " |> mean()";

                return (await _client.GetQueryApi().QueryAsync(fluxQuery, _org)).FirstOrDefault();
            }
            catch (Exception)
            {
                Debug.WriteLine("Failed to load data.");
                return new FluxTable();
            }
        }

        public async Task<List<string?>?> FetchMeasurements(string bucket)
        {
            try
            {
                string query = "import \"influxdata/influxdb/schema\""
                    + $" schema.measurements(bucket: \"{bucket}\")";

                List<FluxTable>? result = await _client.GetQueryApi().QueryAsync(query, _org);
                return result.First().Records.Select(item => item.GetValue().ToString()).ToList();
            }
            catch (Exception)
            {
                Debug.WriteLine("Failed to load measurements.");
                return null;
            }
        }

        public async Task<List<FluxTable>?> FetchMeasurements(string bucket, string? deviceId)
        {
            try
            {
                string fluxQuery =
                    $"deviceData = from(bucket: \"{bucket}\")"
                    + "     |> range(start: -30d)"
                    + "     |> filter(fn: (r) => (r[\"_measurement\"] == \"environment\"))"
                    + $"    |> filter(fn: (r) => (r.clientId == \"{deviceId}\"))"
                    + "measurements = deviceData"
                    + "     |> keep(columns: [\"_field\", \"_value\", \"_time\"])"
                    + "     |> group(columns: [\"_field\"])"
                    + "counts = measurements |> count()"
                    + "     |> keep(columns: [\"_field\", \"_value\"])"
                    + "     |> rename(columns: {_value: \"count\"   })"
                    + "maxValues = measurements |> max  ()"
                    + "     |> toFloat()"
                    + "     |> keep(columns: [\"_field\", \"_value\"])"
                    + "     |> rename(columns: {_value: \"maxValue\"})"
                    + "minValues = measurements |> min  ()"
                    + "     |> toFloat()"
                    + "     |> keep(columns: [\"_field\", \"_value\"])"
                    + "     |> rename(columns: {_value: \"minValue\"})"
                    + "maxTimes  = measurements |> max  (column: \"_time\")"
                    + "     |> keep(columns: [\"_field\", \"_time\" ])"
                    + "     |> rename(columns: {_time : \"maxTime\" })"
                    + "j = (tables=<-, t) => join(tables: {tables, t}, on:[\"_field\"])"
                    + "counts"
                    + "|> j(t: maxValues)"
                    + "|> j(t: minValues)"
                    + "|> j(t: maxTimes)"
                    + "|> yield(name: \"measurements\")";

                Debug.WriteLine("Loading measurements.");
                return await _client.GetQueryApi().QueryAsync(fluxQuery, _org);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Failed to load data. -" + e);
                return new List<FluxTable>();
            }
        }


    }
}


// var queryApi =  _client.GetQueryApiSync();
// var settings = new QueryableOptimizerSettings{DropMeasurementColumn = false};
// var queryable = new InfluxDBQueryable<GetRouteInfo>("Cmd", _org, queryApi, new DefaultMemberNameResolver(),settings);
// var latest =  queryable.Select(c=>c);
// var queryResult = latest.ToList();
// Console.WriteLine(queryResult);
// foreach (var item in queryResult)
// {
//     Console.WriteLine(item);
// }
// var query = (from s in InfluxDBQueryable<GetRouteInfo>.Queryable("Cmd", _org, queryApi)
//             // where s.MacAddress == "FF:FF:FF:FF:FF:FF"
//             // orderby s.Timestamp
//             select s
//         )
//     ;
//
// Console.WriteLine("==== Debug LINQ Queryable Flux output ====");
// var influxQuery = ((InfluxDBQueryable<GetRouteInfo>) query).ToDebugQuery();
// foreach (var statement in influxQuery.Extern.Body)
// {
//     var os = statement as OptionStatement;
//     var va = os?.Assignment as VariableAssignment;
//     var name = va?.Id.Name;
//     var value = va?.Init.GetType().GetProperty("Value")?.GetValue(va.Init, null);
//
//     Console.WriteLine($"{name}={value}");
// }
// Console.WriteLine();
// Console.WriteLine(influxQuery._Query);
