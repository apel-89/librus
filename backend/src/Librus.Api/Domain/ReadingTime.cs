namespace Librus.Api.Domain;

public static class ReadingTimePolicy
{
    public const int MinSamples = 5;
}

public enum ReadingTimeSource
{
    /// <summary>Median av vad andra låntagare faktiskt rapporterat för boken.</summary>
    ReportedForBook,
    /// <summary>Beräknad från sidantal och bibliotekets genomsnittliga lästakt.</summary>
    EstimatedFromPages,
}

public sealed record ReadingTimeEstimate(int Minutes, ReadingTimeSource Source, int SampleSize);