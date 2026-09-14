using Npgsql;

namespace Librus.Api.Features.Books;

public sealed class ReadingTimeService(LibrusDbContext db)
{
    public async Task<ReadingTimeEstimate?> EstimateAsync(
        int bookId, int pages, CancellationToken ct = default)
    {
        var row = await db.ReadingTimeRows
            .FromSqlRaw(SqlResources.Load("ReadingTimeEstimate"),
                new NpgsqlParameter("book_id", bookId))
            .SingleAsync(ct);

        if (row.SampleSize >= ReadingTimePolicy.MinSamples && row.MedianMinutes is > 0)
            return new ReadingTimeEstimate(
                (int)Math.Round(row.MedianMinutes.Value),
                ReadingTimeSource.ReportedForBook,
                row.SampleSize);

        if (row.GlobalMinutesPerPage is > 0)
            return new ReadingTimeEstimate(
                (int)Math.Round(row.GlobalMinutesPerPage.Value * pages),
                ReadingTimeSource.EstimatedFromPages,
                row.SampleSize);

        return null;
    }
}