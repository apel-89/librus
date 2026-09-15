namespace Librus.Api.Features.Loans;

public static class MyLoanEndpoints
{
    public static void MapMyLoanEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/me/loans", async (
            string? status,
            LibrusDbContext db,
            ICurrentUser user,
            IClock clock,
            CancellationToken ct) =>
        {
            var now = clock.UtcNow;

            var query = db.Loans.Where(l => l.UserId == user.Id);

            query = status?.ToLowerInvariant() switch
            {
                "active" => query.Where(l => l.ReturnedAt == null),
                "returned" => query.Where(l => l.ReturnedAt != null),
                _ => query,
            };

            var loans = await query
                .OrderBy(l => l.ReturnedAt == null ? 0 : 1)
                .ThenBy(l => l.ReturnedAt == null ? l.DueAt : DateTime.MaxValue)
                .ThenByDescending(l => l.ReturnedAt)
                .Select(l => new MyLoanResponse(
                    l.Id,
                    l.Copy.BookId,
                    l.Copy.Book.Title,
                    l.Copy.Book.Author.Name,
                    l.Copy.Barcode,
                    l.BorrowedAt,
                    l.DueAt,
                    l.ReturnedAt,
                    l.RenewalCount,
                    l.ReturnedAt == null && l.DueAt < now,
                    l.ReturnedAt == null
                        && l.DueAt >= now
                        && l.RenewalCount < LoanPolicy.MaxRenewals,
                    l.Copy.Book.CoverId))
                .ToListAsync(ct);

            return Results.Ok(loans);
        })
        .WithTags("Loans");
    }
}

public sealed record MyLoanResponse(
    int Id,
    int BookId,
    string Title,
    string Author,
    string Barcode,
    DateTime BorrowedAt,
    DateTime DueAt,
    DateTime? ReturnedAt,
    int RenewalCount,
    bool IsOverdue,
    bool CanRenew, 
    int? CoverId);