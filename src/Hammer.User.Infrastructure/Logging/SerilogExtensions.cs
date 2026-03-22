using Serilog;
using Serilog.Events;

namespace Hammer.User.Infrastructure.Logging;

/// <summary>
/// Extension methods for configuring Serilog with Kafka error sink.
/// </summary>
public static class SerilogExtensions
{
    /// <summary>
    /// Adds a Kafka sink that publishes error-level log events with exceptions.
    /// </summary>
    /// <param name="configuration">The Serilog logger configuration.</param>
    /// <param name="bootstrapServers">Kafka bootstrap servers connection string.</param>
    /// <returns>The logger configuration for chaining.</returns>
    public static LoggerConfiguration WriteToKafkaErrors(
        this LoggerConfiguration configuration,
        string bootstrapServers)
    {
        ArgumentNullException.ThrowIfNull(configuration);

#pragma warning disable CA2000 // Serilog owns the sink lifecycle and disposes it on Log.CloseAndFlush()
        return configuration.WriteTo.Sink(
            new KafkaErrorSink(bootstrapServers),
            LogEventLevel.Error);
#pragma warning restore CA2000
    }
}
