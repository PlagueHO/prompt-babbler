using OpenTelemetry;
using OpenTelemetry.Logs;

namespace PromptBabbler.ServiceDefaults.Logging;

internal sealed class SanitizingLogRecordProcessor : BaseProcessor<LogRecord>
{
    public override void OnEnd(LogRecord data)
    {
        data.FormattedMessage = LogSanitizer.Sanitize(data.FormattedMessage);

        if (data.Body is string body)
        {
            data.Body = LogSanitizer.Sanitize(body);
        }

        data.Attributes = LogSanitizer.SanitizeAttributes(data.Attributes);
    }
}