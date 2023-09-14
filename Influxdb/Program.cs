using Microsoft.AspNetCore.Server.Kestrel.Core;
using Influxdb.Infrastructure;
using Influxdb.Models;
using Influxdb.Services;

/*
  ┌─────────────────────────────────────────────────────────────────────────┐
  │ Main program                                                            │
  └─────────────────────────────────────────────────────────────────────────┘
 */
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682
builder.WebHost.ConfigureKestrel(options =>
{
    // Setup a HTTP/2 endpoint without TLS.
    options.ListenLocalhost(5000, o => o.Protocols =
        HttpProtocols.Http2);
});

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();
builder.Services
    .AddOptions<Configurations>()
    .Bind(builder.Configuration.GetSection("Configurations"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<IInfluxDb, InfluxDb>();

WebApplication app = builder.Build();

IWebHostEnvironment env = app.Environment;
if (env.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

// Configure the HTTP request pipeline.
app.MapGrpcService<SampleService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
