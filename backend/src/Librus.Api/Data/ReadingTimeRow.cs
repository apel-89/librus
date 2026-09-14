namespace Librus.Api.Data;

public sealed class ReadingTimeRow
{
    public double? MedianMinutes { get; set; }
    public int SampleSize { get; set; }
    public double? GlobalMinutesPerPage { get; set; }
}