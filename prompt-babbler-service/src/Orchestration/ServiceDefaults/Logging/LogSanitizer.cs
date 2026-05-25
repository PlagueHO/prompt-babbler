using System.Text;

namespace PromptBabbler.ServiceDefaults.Logging;

internal static class LogSanitizer
{
    internal static string? Sanitize(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        StringBuilder? sanitized = null;

        for (var index = 0; index < value.Length; index++)
        {
            var current = value[index];
            var replacement = current switch
            {
                '\r' => "\\r",
                '\n' => "\\n",
                '\t' => "\\t",
                _ when char.IsControl(current) => $"\\u{(int)current:x4}",
                _ => null,
            };

            if (replacement is null)
            {
                if (sanitized is not null)
                {
                    sanitized.Append(current);
                }

                continue;
            }

            sanitized ??= new StringBuilder(value.Length + 8);

            if (index > 0 && sanitized.Length == 0)
            {
                sanitized.Append(value, 0, index);
            }

            sanitized.Append(replacement);
        }

        return sanitized?.ToString() ?? value;
    }

    internal static IReadOnlyList<KeyValuePair<string, object?>>? SanitizeAttributes(
        IReadOnlyList<KeyValuePair<string, object?>>? attributes)
    {
        if (attributes is null || attributes.Count == 0)
        {
            return attributes;
        }

        List<KeyValuePair<string, object?>>? sanitized = null;

        for (var index = 0; index < attributes.Count; index++)
        {
            var attribute = attributes[index];

            if (attribute.Value is not string stringValue)
            {
                sanitized?.Add(attribute);
                continue;
            }

            var sanitizedValue = Sanitize(stringValue);

            if (ReferenceEquals(sanitizedValue, stringValue) || sanitizedValue == stringValue)
            {
                sanitized?.Add(attribute);
                continue;
            }

            sanitized ??= new List<KeyValuePair<string, object?>>(attributes.Count);

            if (index > 0 && sanitized.Count == 0)
            {
                for (var copyIndex = 0; copyIndex < index; copyIndex++)
                {
                    sanitized.Add(attributes[copyIndex]);
                }
            }

            sanitized.Add(new KeyValuePair<string, object?>(attribute.Key, sanitizedValue));
        }

        return sanitized ?? attributes;
    }
}