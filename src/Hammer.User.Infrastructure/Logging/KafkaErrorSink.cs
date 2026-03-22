using System.Text.Json;
using Confluent.Kafka;
using Serilog.Core;
using Serilog.Events;

namespace Hammer.User.Infrastructure.Logging;

/// <summary>
///     Serilog sink that publishes error log events to a Kafka topic.
/// </summary>
internal sealed class KafkaErrorSink : ILogEventSink, IDisposable
{
    private const string Topic = "service-error-log";
    private const string Source = "hammer-user";

    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly IProducer<string, string> _producer;

    public KafkaErrorSink(string bootstrapServers)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.Leader,
            LingerMs = 5,
            BatchNumMessages = 100,
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    internal KafkaErrorSink(IProducer<string, string> producer)
    {
        _producer = producer;
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }

    public void Emit(LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);

        if (logEvent.Exception is null || logEvent.Level < LogEventLevel.Error)
            return;

        var traceId = GetProperty(logEvent, "TraceId");
        var requestPath = GetProperty(logEvent, "RequestPath");
        var requestMethod = GetProperty(logEvent, "RequestMethod");

        var payload = JsonSerializer.Serialize(
            new
            {
                TraceId = traceId,
                Source,
                Level = logEvent.Level.ToString(),
                ExceptionType = logEvent.Exception.GetType().FullName,
                logEvent.Exception.Message,
                logEvent.Exception.StackTrace,
                RequestPath = requestPath,
                RequestMethod = requestMethod,
                Timestamp = logEvent.Timestamp.UtcDateTime,
            },
            _jsonOptions);

        var message = new Message<string, string> { Key = traceId, Value = payload };

#pragma warning disable CA1031 // Fire-and-forget: logging must not throw
        try
        {
            _producer.Produce(Topic, message);
        }
        catch
        {
            // Swallow — logging infrastructure must not crash the application.
        }
#pragma warning restore CA1031
    }

    private static string GetProperty(LogEvent logEvent, string name)
    {
        if (logEvent.Properties.TryGetValue(name, out var value) && value is ScalarValue scalar)
            return scalar.Value?.ToString() ?? string.Empty;

        return string.Empty;
    }
}
