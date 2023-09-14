namespace Influxdb.Models
{
    public class Configurations

    {
        public InfluxDbConfig InfluxDbConfig { get; set; }
    }

    public class InfluxDbConfig
    {
        public string Url { get; set; }
        public string Token { get; set; }
        public string Org { get; set; }
    }

}
