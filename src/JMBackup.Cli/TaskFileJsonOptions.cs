using System.Text.Json;
using System.Text.Json.Serialization;

namespace JMBackup.Cli;

internal static class TaskFileJsonOptions
{
    public static readonly JsonSerializerOptions Value = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}
