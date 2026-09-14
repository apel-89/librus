namespace Librus.Api.Features.Users;

public static class MeEndpoints
{
    public static void MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/me", async (
            LibrusDbContext db,
            ICurrentUser current,
            IClock clock,
            CancellationToken ct) =>
        {
            var now = clock.UtcNow;

            var user = await db.Users
                .Where(u => u.Id == current.Id)
                .Select(u => new { u.Id, u.FirstName, u.LastName })
                .FirstOrDefaultAsync(ct);

            if (user is null)
                throw new NotFoundException("Låntagaren finns inte.");

            var active = await db.Loans
                .Where(l => l.UserId == current.Id && l.ReturnedAt == null)
                .Select(l => l.DueAt)
                .ToListAsync(ct);

            var overdue = active.Count(due => due < now);
            var atLimit = active.Count >= LoanPolicy.MaxActiveLoansPerUser;

            // Samma ordning som kontrollerna i LoanService, så att förklaringen
            // frontenden visar stämmer med det fel ett lånförsök faktiskt ger.
            var blockedReason =
                atLimit ? BorrowBlockedReason.LoanLimitReached
                : overdue > 0 ? BorrowBlockedReason.HasOverdueLoans
                : (BorrowBlockedReason?)null;

            return Results.Ok(new MeResponse(
                user.Id,
                $"{user.FirstName} {user.LastName}",
                active.Count,
                LoanPolicy.MaxActiveLoansPerUser,
                overdue,
                blockedReason is null,
                blockedReason));
        })
        .WithTags("Me");
    }
}

public enum BorrowBlockedReason
{
    HasOverdueLoans,
    LoanLimitReached,
}

public sealed record MeResponse(
    int Id,
    string Name,
    int ActiveLoanCount,
    int MaxActiveLoans,
    int OverdueCount,
    bool CanBorrow,
    BorrowBlockedReason? BlockedReason);