namespace Librus.Api.Features.Books;

public static class BookEndpoints
{
    public static void MapBookEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/books", async (
            string? search,
            int? genreId,
            bool? availableOnly,
            int? page,
            int? pageSize,
            LibrusDbContext db,
            ICurrentUser user,
            CancellationToken ct) =>
        {
            var currentPage = page is null or < 1 ? 1 : page.Value;
            var size = Math.Clamp(pageSize ?? 20, 1, 100);

            var query = db.Books.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = $"%{search.Trim()}%";
                query = query.Where(b =>
                    EF.Functions.ILike(b.Title, pattern) ||
                    EF.Functions.ILike(b.Author.Name, pattern));
            }

            if (genreId is not null)
                query = query.Where(b => b.GenreId == genreId);

            if (availableOnly == true)
                query = query.Where(b => db.BookCopies
                    .Any(c => c.BookId == b.Id && !db.Loans
                        .Any(l => l.CopyId == c.Id && l.ReturnedAt == null)));

            var total = await query.CountAsync(ct);

            var items = await query
                .OrderBy(b => b.Title)
                .Skip((currentPage - 1) * size)
                .Take(size)
                .Select(b => new BookListItem(
                    b.Id,
                    b.Title,
                    b.Author.Name,
                    b.Genre.Name,
                    b.PublishedYear,
                    b.Pages,
                    b.Copies.Count,
                    b.Copies.Count(c => c.Loans.All(l => l.ReturnedAt != null)),
                    db.Feedback.Where(f => f.BookId == b.Id).Average(f => (double?)f.Score),
                    b.Copies.Any(c => c.Loans.Any(l => l.ReturnedAt == null && l.UserId == user.Id)),
                    b.CoverId))
                .ToListAsync(ct);

            return Results.Ok(new PagedResult<BookListItem>(items, currentPage, size, total));
        })
        .WithTags("Books");

        app.MapGet("/api/books/{id:int}", async (
            int id,
            LibrusDbContext db,
            ReadingTimeService readingTime,
            ICurrentUser user,
            CancellationToken ct) =>
        {
            var book = await db.Books
                .Where(b => b.Id == id)
                .Select(b => new
                {
                    b.Id,
                    b.Title,
                    b.Description,
                    b.PublishedYear,
                    b.Pages,
                    b.CoverId,
                    Author = b.Author.Name,
                    Genre = b.Genre.Name,
                    CopiesTotal = b.Copies.Count,
                    CopiesAvailable = b.Copies.Count(c => c.Loans.All(l => l.ReturnedAt != null)),
                    MyActiveLoanId = b.Copies
                        .SelectMany(c => c.Loans)
                        .Where(l => l.ReturnedAt == null && l.UserId == user.Id)
                        .Select(l => (int?)l.Id)
                        .FirstOrDefault(),
                    AverageScore = db.Feedback.Where(f => f.BookId == b.Id)
                        .Average(f => (double?)f.Score),
                    FeedbackCount = db.Feedback.Count(f => f.BookId == b.Id),
                })
                .FirstOrDefaultAsync(ct);

            if (book is null)
                throw new NotFoundException($"Boken med id {id} finns inte.");

            var reviews = await db.Feedback
                .Where(f => f.BookId == id && f.Review != null)
                .OrderByDescending(f => f.CreatedAt)
                .Take(5)
                .Select(f => new ReviewResponse(
                    f.Score, f.Review!, f.User.FirstName, f.CreatedAt))
                .ToListAsync(ct);

            var estimate = await readingTime.EstimateAsync(id, book.Pages, ct);

            return Results.Ok(new BookDetailResponse(
                book.Id, book.Title, book.Author, book.Genre, book.Description,
                book.PublishedYear, book.Pages,
                book.CopiesTotal, book.CopiesAvailable,
                book.AverageScore, book.FeedbackCount,
                book.MyActiveLoanId,
                estimate,
                reviews, 
                book.CoverId));
        })
        .WithTags("Books");
    }
}

public sealed record BookListItem(
    int Id,
    string Title,
    string Author,
    string Genre,
    int PublishedYear,
    int Pages,
    int CopiesTotal,
    int CopiesAvailable,
    double? AverageScore,
    bool BorrowedByMe, 
    int? CoverId);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);

public sealed record ReviewResponse(int Score, string Review, string By, DateTime CreatedAt);

public sealed record BookDetailResponse(
    int Id, string Title, string Author, string Genre, string? Description,
    int PublishedYear, int Pages,
    int CopiesTotal, int CopiesAvailable,
    double? AverageScore, int FeedbackCount,
    int? MyActiveLoanId,
    ReadingTimeEstimate? ReadingTime,
    IReadOnlyList<ReviewResponse> Reviews, 
    int? CoverId);