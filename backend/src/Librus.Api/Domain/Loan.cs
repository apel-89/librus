namespace Librus.Api.Domain;

public class Loan
{
    public int Id { get; set; }

    public int CopyId { get; set; }
    public BookCopy Copy { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime BorrowedAt { get; set; }
    public DateTime DueAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public int RenewalCount { get; set; }

    public bool IsActive => ReturnedAt is null;
    public bool IsOverdue(DateTime now) => IsActive && now > DueAt;

    public static Loan Start(int copyId, int userId, DateTime now) => new()
    {
        CopyId = copyId,
        UserId = userId,
        BorrowedAt = now,
        DueAt = now.AddDays(LoanPolicy.LoanPeriodDays)
    };

    public void Return(DateTime now) => ReturnedAt = now;

    public void Renew()
    {
        DueAt = DueAt.AddDays(LoanPolicy.RenewalDays);
        RenewalCount++;
    }
}