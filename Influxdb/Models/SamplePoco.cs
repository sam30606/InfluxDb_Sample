using InfluxDB.Client.Core;

namespace Influxdb.Models
{
    /// <param name="sensor_id"></param>
    /// <param name="co"></param>
    /// <param name="humidity"></param>
    /// <param name="temperature"></param>
    [Measurement("airSensors")]
    public class SamplePoco : BasicPoco
    {
        [Column("co")]
        public float co { get; set; }
        [Column("humidity")]
        public float humidity { get; set; }
        [Column("temperature")]
        public float temperature { get; set; }
    }
}
