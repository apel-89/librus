namespace Librus.Api.Domain;

public static class ReadingTimePolicy
{
    public const int MinSamples = 5;
}

public enum ReadingTimeSource
{
    ReportedForBook,
    EstimatedFromPages,
}

public sealed record ReadingTimeEstimate(int Minutes, ReadingTimeSource Source, int SampleSize);