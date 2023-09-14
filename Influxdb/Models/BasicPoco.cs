using InfluxDB.Client.Core;

namespace Influxdb.Models

{
    public class BasicPoco
    {
        [Column("sensor_id", IsTag = true)]
        public string sensor_id { get; set; }
        [Column(IsTimestamp = true)]
        public DateTime Timestamp { get; set; }
    }
}
