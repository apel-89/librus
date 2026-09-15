namespace Librus.Api.Features.Loans;

public static class LoanEndpoints
{
    public static void MapLoanEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/loans").WithTags("Loans");

        group.MapPost("/", async (
            BorrowRequest request,
            LoanService loans,
            ICurrentUser user,
            CancellationToken ct) =>
        {
            var loan = await loans.BorrowAsync(request.BookId, user.Id, ct);
            return Results.Created($"/api/loans/{loan.Id}", LoanResponse.From(loan));
        });
        
        group.MapPost("/{id:int}/return", async (
            int id, LoanService loans, ICurrentUser user, CancellationToken ct) =>
        {
            var loan = await loans.ReturnAsync(id, user.Id, ct);
            return Results.Ok(LoanResponse.From(loan));
        });

        group.MapPost("/{id:int}/renew", async (
            int id, LoanService loans, ICurrentUser user, CancellationToken ct) =>
        {
            var loan = await loans.RenewAsync(id, user.Id, ct);
            return Results.Ok(LoanResponse.From(loan));
        });
    }
}

public sealed record BorrowRequest(int BookId);

public sealed record LoanResponse(
    int Id,
    int CopyId,
    DateTime BorrowedAt,
    DateTime DueAt,
    DateTime? ReturnedAt,
    int RenewalCount)
{
    public static LoanResponse From(Loan loan) => new(
        loan.Id, loan.CopyId, loan.BorrowedAt, loan.DueAt,
        loan.ReturnedAt, loan.RenewalCount);
}