using MassTransit;
using MassTransitTelemetryIssue.V7;
using Microsoft.ApplicationInsights.DependencyCollector;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddApplicationInsightsTelemetry();

builder.Services.ConfigureTelemetryModule<DependencyTrackingTelemetryModule>((module, o) =>
{
    module.IncludeDiagnosticSourceActivities.Add("MassTransit");
});

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddGenericRequestClient();

builder.Services.AddMassTransit(config =>
{
    config.AddConsumer<SubmitOrderConsumer>();

    config.UsingInMemory((ctx, cfg) =>
    {
        cfg.ConfigureEndpoints(ctx);
    });
});

builder.Services.AddMassTransitHostedService();

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
