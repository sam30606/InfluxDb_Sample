using Grpc.Core;
using InfluxDB.Client.Core.Flux.Domain;
using InfluxDB.Client.Writes;
using Influxdb.Infrastructure;
using Influxdb.Models;
using Influxdb.Service;
using System.Reflection;

namespace Influxdb.Services
{
    public class SampleService : Sample.SampleBase
    {
        private readonly ILogger<SampleService> _logger;
        private readonly IInfluxDb influxDb;
        public SampleService(ILogger<SampleService> logger, IInfluxDb influxDb)
        {
            _logger = logger;
            this.influxDb = influxDb;
        }

        public override async Task<SampleReply> Sample(SampleRequest request, ServerCallContext context)
        {
             influxDb.WriteMeasurement(new SamplePoco
            {
                sensor_id = request.SensorId,
                co = request.Co,
                humidity = request.Humidity,
                temperature = request.Temperature,
                Timestamp = DateTime.UtcNow.ToLocalTime()
            }, "default");
             
            // List<SamplePoco> result = await influxDb.FetchDataLinq<SamplePoco>("default", request.SensorId, request.TimeRange, request.Method, request.AggregateSec);
            // if (result.Count != 0)
            // {
            //     foreach (SamplePoco item in result)
            //     {
            //         Console.WriteLine();
            //         PrintObjectProperties(item);
            //         Console.WriteLine();
            //     }
            // }
            // else
            // {
            //     Console.WriteLine("EMPTY");
            // }
            var result =influxDb.FetchDataSimple("default","1h","airSensors");
            Console.WriteLine(result.Result);
            
            foreach (FluxTable table in result.Result)
            {
                Console.WriteLine("\n\n");
                foreach (FluxRecord record in table.Records)
                {
                    foreach (KeyValuePair<string, object> kvp in record.Values)
                    {
                        Console.WriteLine($"{kvp.Key}: {kvp.Value}");
                    }
                }
            }
            
            return await Task.FromResult(new SampleReply
            {
                Message = ""
            });
        }
        public static void PrintObjectProperties(object obj)
        {
            Type type = obj.GetType();
            PropertyInfo[] properties = type.GetProperties();

            foreach (PropertyInfo property in properties)
            {
                string propertyName = property.Name;
                object propertyValue = property.GetValue(obj, null);
                Console.WriteLine($"{propertyName}: {propertyValue}");
            }
        }
    }

}
