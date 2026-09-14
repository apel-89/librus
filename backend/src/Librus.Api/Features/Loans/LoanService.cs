using Librus.Api.Data;
using Librus.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Librus.Api.Features.Loans;


public sealed class LoanService(LibrusDbContext db, IClock clock)
{
    public async Task<Loan> BorrowAsync(int bookId, int userId, CancellationToken ct = default)
    {
        var now = clock.UtcNow;

        var bookExists = await db.Books.AnyAsync(b => b.Id == bookId, ct);
        if (!bookExists)
            throw new NotFoundException($"Boken med id {bookId} finns inte.");

        var activeLoans = await db.Loans
            .Where(l => l.UserId == userId && l.ReturnedAt == null)
            .Select(l => new { l.DueAt, l.Copy.BookId })
            .ToListAsync(ct);

        if (activeLoans.Any(l => l.BookId == bookId))
            throw new ConflictException("Du har redan ett pågående lån på den här boken.");

        if (activeLoans.Count >= LoanPolicy.MaxActiveLoansPerUser)
            throw new ConflictException(
                $"Du kan ha högst {LoanPolicy.MaxActiveLoansPerUser} pågående lån samtidigt.");

        if (activeLoans.Any(l => l.DueAt < now))
            throw new ConflictException(
                "Du har försenade lån. Lämna tillbaka dem innan du lånar något nytt.");

        var candidateIds = await db.BookCopies
            .Where(c => c.BookId == bookId && !c.Loans.Any(l => l.ReturnedAt == null))
            .OrderBy(c => c.Id)
            .Select(c => c.Id)
            .Take(5)
            .ToListAsync(ct);

        if (candidateIds.Count == 0)
            throw new ConflictException("Alla exemplar av boken är utlånade just nu.");

        foreach (var copyId in candidateIds)
        {
            var loan = Loan.Start(copyId, userId, now);
            db.Loans.Add(loan);

            try
            {
                await db.SaveChangesAsync(ct);
                return loan;
            }
            catch (DbUpdateException ex) when (IsActiveCopyConflict(ex))
            {
                db.Entry(loan).State = EntityState.Detached;
            }
        }

        throw new ConflictException("Alla exemplar av boken är utlånade just nu.");
    }

    public async Task<Loan> ReturnAsync(int loanId, int userId, CancellationToken ct = default)
    {
        var loan = await GetOwnedLoanAsync(loanId, userId, ct);

        if (!loan.IsActive)
            throw new ConflictException("Lånet är redan återlämnat.");

        loan.Return(clock.UtcNow);
        await db.SaveChangesAsync(ct);
        return loan;
    }

    public async Task<Loan> RenewAsync(int loanId, int userId, CancellationToken ct = default)
    {
        var loan = await GetOwnedLoanAsync(loanId, userId, ct);

        if (!loan.IsActive)
            throw new ConflictException("Lånet är redan återlämnat och kan inte förlängas.");

        if (loan.IsOverdue(clock.UtcNow))
            throw new ConflictException(
                "Lånet är försenat och måste lämnas tillbaka. Försenade lån kan inte förlängas.");

        if (loan.RenewalCount >= LoanPolicy.MaxRenewals)
            throw new ConflictException(
                $"Lånet har redan förlängts {LoanPolicy.MaxRenewals} gånger.");

        loan.Renew();
        await db.SaveChangesAsync(ct);
        return loan;
    }

    private async Task<Loan> GetOwnedLoanAsync(int loanId, int userId, CancellationToken ct)
    {
        var loan = await db.Loans.FirstOrDefaultAsync(l => l.Id == loanId, ct)
            ?? throw new NotFoundException($"Lånet med id {loanId} finns inte.");

        if (loan.UserId != userId)
            throw new ForbiddenException("Lånet tillhör en annan låntagare.");

        return loan;
    }


    private static bool IsActiveCopyConflict(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg
        && pg.ConstraintName == "ix_loans_active_copy";
}