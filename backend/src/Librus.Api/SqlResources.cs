using System.Collections.Concurrent;
using System.Reflection;

namespace Librus.Api;

public static class SqlResources
{
    private static readonly ConcurrentDictionary<string, string> Cache = new();

    public static string Load(string name) => Cache.GetOrAdd(name, key =>
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resource = $"Librus.Api.Sql.{key}.sql";

        using var stream = assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Hittade inte SQL-resursen {resource}.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    });
}