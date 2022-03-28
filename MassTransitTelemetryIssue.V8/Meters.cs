using System.Diagnostics.Metrics;

namespace MassTransitTelemetryIssue.V8
{
    public static class Meters
    {
        public static readonly Meter CustomMeter = new("Custom.Meter", "1.0");

        public static readonly Counter<long> CustomCounter = CustomMeter.CreateCounter<long>("CustomCounter");
    }
}
