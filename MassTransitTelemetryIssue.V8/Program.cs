using Azure.Monitor.OpenTelemetry.Exporter;
using MassTransit;
using MassTransitTelemetryIssue.V8;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var resourceAttributes = new Dictionary<string, object> {
    { "service.name", "my-service" },
    { "service.namespace", "my-namespace" },
    { "service.instance.id", "my-instance" }};

builder.Services.AddOpenTelemetryTracing(telemetryBuilder =>
{
    telemetryBuilder
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddAttributes(resourceAttributes))
        .AddSource("MassTransit")
        .AddAspNetCoreInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSqlClientInstrumentation()
        .AddAzureMonitorTraceExporter(opts =>
        {
            opts.ConnectionString = builder.Configuration.GetConnectionString("APP_INSIGHTS_CONNECTION_STRING");
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
