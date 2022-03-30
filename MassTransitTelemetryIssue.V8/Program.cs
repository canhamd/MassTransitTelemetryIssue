using MassTransit;
using MassTransitTelemetryIssue.V8;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Diagnostics.Metrics;

using static MassTransitTelemetryIssue.V8.Meters;

var resourceAttributes = new Dictionary<string, object> {
    { "service.name", "my-service" },
    { "service.namespace", "my-namespace" },
    { "service.instance.id", "my-instance" }
};

// without this the listener isn't aware of it before the first measurement is emitted
//var enabled = CustomCounter.Enabled;

Meter s_meter = new Meter("HatCo.HatStore", "1.0.0");
Counter<int> s_hatsSold = s_meter.CreateCounter<int>(name: "hats-sold", unit: "Hats", description: "The number of hats sold in our store");

//using MeterProvider meterProvider = Sdk.CreateMeterProviderBuilder()
//    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddAttributes(resourceAttributes))
//    .AddHttpClientInstrumentation()
//    .AddAspNetCoreInstrumentation()
//    .AddMeter("HatCo.HatStore")
//    .AddMeter("Custom.Meter")
//    .AddConsoleExporter((exporterConfig, readerConfig) =>
//    {
//        readerConfig.MetricReaderType = MetricReaderType.Periodic;
//        readerConfig.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 5000;
//    })
//    .Build();

// does not matter if listener comes before or after instrument creation,
// except in regards to not listening to events emitted prior to creation
MeterListener listener = new MeterListener();
listener.InstrumentPublished = (instrument, meterListener) =>
{
    if (instrument.Name == "CustomCounter" && instrument.Meter.Name == "Custom.Meter")
    {
        meterListener.EnableMeasurementEvents(instrument, null);
    }
    else if (instrument.Name == "hats-sold" && instrument.Meter.Name == "HatCo.HatStore")
    {
        meterListener.EnableMeasurementEvents(instrument, null);
    }
};
listener.SetMeasurementEventCallback<int>((instrument, measurement, tags, state) =>
{
    Console.WriteLine($"Instrument: {instrument.Name} has recorded the measurement {measurement}");
});
listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
{
    Console.WriteLine($"Instrument: {instrument.Name} has recorded the measurement {measurement}");
});
listener.Start();

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddOpenTelemetry(openTelemetryLoggerOptions =>
{
    openTelemetryLoggerOptions
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddAttributes(resourceAttributes))
        .AddConsoleExporter();
});

// Add services to the container.

builder.Services.AddOpenTelemetryMetrics(meterProviderBuilder =>
{
    meterProviderBuilder
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddAttributes(resourceAttributes))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddMeter("Custom.Meter")
        .AddMeter("HatCo.HatStore")
        .AddConsoleExporter((exporterConfig, readerConfig) =>
        {
            readerConfig.MetricReaderType = MetricReaderType.Periodic;
            readerConfig.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 5000;
        });
});

builder.Services.AddOpenTelemetryTracing(traceProviderBuilder =>
{
    traceProviderBuilder
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddAttributes(resourceAttributes).AddTelemetrySdk().AddEnvironmentVariableDetector())
        .AddAspNetCoreInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSqlClientInstrumentation()
        .AddSource("MassTransit")
        .AddSource("Test.Meter")
        .AddConsoleExporter()
        .AddJaegerExporter(o =>
        {
            o.AgentHost = "localhost";
            o.AgentPort = 6831;
            o.MaxPayloadSizeInBytes = 4096;
            o.ExportProcessorType = ExportProcessorType.Batch;
            o.BatchExportProcessorOptions = new BatchExportProcessorOptions<Activity>
            {
                MaxQueueSize = 2048,
                ScheduledDelayMilliseconds = 5000,
                ExporterTimeoutMilliseconds = 30000,
                MaxExportBatchSize = 512,
            };
        });
});

//builder.Services.AddApplicationInsightsTelemetry(opts =>
//{
//    opts.ConnectionString = builder.Configuration.GetConnectionString("APP_INSIGHTS_CONNECTION_STRING");
//});

//builder.Services.ConfigureTelemetryModule<DependencyTrackingTelemetryModule>((module, o) =>
//{
//    module.IncludeDiagnosticSourceActivities.Add("MassTransit");
//});

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMassTransit(config =>
{
    config.AddConsumer<SubmitOrderConsumer>();

    config.UsingInMemory((ctx, cfg) =>
    {
        cfg.ConfigureEndpoints(ctx);
    });
});

var app = builder.Build();

app.MapGet("/Metric", async (ILogger<Program> logger) =>
{
    s_hatsSold.Add(4);

    CustomCounter.Add(5, new KeyValuePair<string, object?>("tag-api-type", "minimal api"), new KeyValuePair<string, object?>("tag-url", "/Metric"));

    return await Task.FromResult("Ok");
});

// Configure the HTTP request pipeline.

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
