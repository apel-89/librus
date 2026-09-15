using Npgsql;

namespace Librus.Api.Features.Books;

public enum PopularityPeriod
{
    Month,
    Year,
    All,
}

public static class DiscoverEndpoints
{
    public static void MapDiscoverEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/books/popular", async (
            PopularityPeriod? period,
            int? limit,
            LibrusDbContext db,
            IClock clock,
            CancellationToken ct) =>
        {
            var take = Math.Clamp(limit ?? 10, 1, 100);

            DateTime? since = (period ?? PopularityPeriod.All) switch
            {
                PopularityPeriod.Month => clock.UtcNow.AddMonths(-1),
                PopularityPeriod.Year => clock.UtcNow.AddYears(-1),
                _ => null,
            };

            var sinceParameter = new NpgsqlParameter("since", NpgsqlTypes.NpgsqlDbType.TimestampTz)
            {
                Value = (object?)since ?? DBNull.Value
            };

            var counts = await db.BookLoanCounts
                .FromSqlRaw(SqlResources.Load("TopBooks"),
                    sinceParameter,
                    new NpgsqlParameter("limit", take))
                .ToListAsync(ct);

            if (counts.Count == 0)
                return Results.Ok(Array.Empty<PopularBook>());

            var ids = counts.Select(c => c.BookId).ToList();

            var books = await db.Books
                .Where(b => ids.Contains(b.Id))
                .Select(b => new
                {
                    b.Id,
                    b.Title,
                    b.CoverId,
                    Author = b.Author.Name,
                    Genre = b.Genre.Name,
                    CopiesTotal = b.Copies.Count,
                    CopiesAvailable = b.Copies.Count(c => c.Loans.All(l => l.ReturnedAt != null)),
                    AverageScore = db.Feedback.Where(f => f.BookId == b.Id)
                        .Average(f => (double?)f.Score),
                })
                .ToDictionaryAsync(b => b.Id, ct);

            // SQL-frågan äger ordningen; uppslagningen lägger bara på bokdata.
            var result = counts
                .Where(c => books.ContainsKey(c.BookId))
                .Select((c, index) =>
                {
                    var b = books[c.BookId];
                    return new PopularBook(
                        index + 1, b.Id, b.Title, b.Author, b.Genre,
                        c.LoanCount, b.CopiesTotal, b.CopiesAvailable, b.AverageScore, b.CoverId);
                })
                .ToList();

            return Results.Ok(result);
        })
        .WithTags("Discover");

        app.MapGet("/api/books/{id:int}/recommendations", async (
            int id,
            int? limit,
            LibrusDbContext db,
            ICurrentUser user,
            CancellationToken ct) =>
        {
            var counts = await db.BookLoanCounts
                .FromSqlRaw(SqlResources.Load("Recommendations"),
                    new NpgsqlParameter("book_id", id),
                    new NpgsqlParameter("user_id", user.Id),
                    new NpgsqlParameter("limit", Math.Clamp(limit ?? 8, 1, 20)))
                .ToListAsync(ct);

            if (counts.Count == 0)
                return Results.Ok(Array.Empty<RecommendedBook>());

            var ids = counts.Select(c => c.BookId).ToList();

            var books = await db.Books
                .Where(b => ids.Contains(b.Id))
                .Select(b => new
                {
                    b.Id,
                    b.Title,
                    b.CoverId,
                    Author = b.Author.Name,
                    CopiesTotal = b.Copies.Count,
                    CopiesAvailable = b.Copies.Count(c => c.Loans.All(l => l.ReturnedAt != null)),
                    AverageScore = db.Feedback.Where(f => f.BookId == b.Id)
                        .Average(f => (double?)f.Score),
                })
                .ToDictionaryAsync(b => b.Id, ct);

            var result = counts
                .Where(c => books.ContainsKey(c.BookId))
                .Select(c =>
                {
                    var b = books[c.BookId];
                    return new RecommendedBook(
                        b.Id, b.Title, b.Author, b.CoverId,
                        b.CopiesTotal, b.CopiesAvailable, b.AverageScore, c.LoanCount);
                })
                .ToList();

            return Results.Ok(result);
        })
        .WithTags("Discover");
    }
}

public sealed record PopularBook(
    int Rank,
    int Id,
    string Title,
    string Author,
    string Genre,
    int LoanCount,
    int CopiesTotal,
    int CopiesAvailable,
    double? AverageScore, 
    int? CoverId);

public sealed record RecommendedBook(
    int Id, string Title, string Author, int? CoverId,
    int CopiesTotal, int CopiesAvailable, double? AverageScore,
    int SharedBorrowers);