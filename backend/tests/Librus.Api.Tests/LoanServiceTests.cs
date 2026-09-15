using Librus.Api.Data;
using Librus.Api.Domain;
using Librus.Api.Features.Loans;
using Microsoft.EntityFrameworkCore;

namespace Librus.Api.Tests;

[Collection(DatabaseCollection.Name)]
public sealed class LoanServiceTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private readonly TestClock _clock = TestClock.At(2026, 3, 2);

    public async Task InitializeAsync() => await fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Borrow_allokerar_ett_ledigt_exemplar()
    {
        var (bookId, _) = await GivenBookWithCopies(2);
        var userId = await GivenUser();

        await using var db = fixture.CreateContext();
        var loan = await new LoanService(db, _clock).BorrowAsync(bookId, userId);

        Assert.True(loan.IsActive);
        Assert.Equal(_clock.UtcNow, loan.BorrowedAt);
        Assert.Equal(_clock.UtcNow.AddDays(LoanPolicy.LoanPeriodDays), loan.DueAt);
    }

    [Fact]
    public async Task Borrow_kastar_konflikt_när_alla_exemplar_är_utlånade()
    {
        var (bookId, _) = await GivenBookWithCopies(1);
        var first = await GivenUser();
        var second = await GivenUser();

        await using var db = fixture.CreateContext();
        var service = new LoanService(db, _clock);
        await service.BorrowAsync(bookId, first);

        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => service.BorrowAsync(bookId, second));
        Assert.Contains("utlånade", ex.Message);
    }

    [Fact]
    public async Task Borrow_tillåter_inte_två_samtidiga_lån_på_samma_bok()
    {
        var (bookId, _) = await GivenBookWithCopies(3);
        var userId = await GivenUser();

        await using var db = fixture.CreateContext();
        var service = new LoanService(db, _clock);
        await service.BorrowAsync(bookId, userId);

        await Assert.ThrowsAsync<ConflictException>(
            () => service.BorrowAsync(bookId, userId));
    }

    [Fact]
    public async Task Borrow_blockeras_av_försenat_lån()
    {
        var (firstBook, _) = await GivenBookWithCopies(1);
        var (secondBook, _) = await GivenBookWithCopies(1);
        var userId = await GivenUser();

        await using var db = fixture.CreateContext();
        var service = new LoanService(db, _clock);
        await service.BorrowAsync(firstBook, userId);

        _clock.Advance(TimeSpan.FromDays(LoanPolicy.LoanPeriodDays + 1));

        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => service.BorrowAsync(secondBook, userId));
        Assert.Contains("försenade", ex.Message);
    }

    [Fact]
    public async Task Borrow_respekterar_taket_för_antal_aktiva_lån()
    {
        var userId = await GivenUser();
        var service = new LoanService(fixture.CreateContext(), _clock);

        for (var i = 0; i < LoanPolicy.MaxActiveLoansPerUser; i++)
        {
            var (bookId, _) = await GivenBookWithCopies(1);
            await service.BorrowAsync(bookId, userId);
        }

        var (oneMore, _) = await GivenBookWithCopies(1);
        await Assert.ThrowsAsync<ConflictException>(
            () => service.BorrowAsync(oneMore, userId));
    }

    [Fact]
    public async Task Två_samtidiga_lån_på_samma_exemplar_ger_bara_ett_lån()
    {
        var (bookId, _) = await GivenBookWithCopies(1);
        var first = await GivenUser();
        var second = await GivenUser();

        await using var dbA = fixture.CreateContext();
        await using var dbB = fixture.CreateContext();

        var results = await Task.WhenAll(
            Attempt(new LoanService(dbA, _clock), bookId, first),
            Attempt(new LoanService(dbB, _clock), bookId, second));

        Assert.Equal(1, results.Count(ok => ok));

        await using var verify = fixture.CreateContext();
        var active = await verify.Loans.CountAsync(l => l.ReturnedAt == null);
        Assert.Equal(1, active);

        static async Task<bool> Attempt(LoanService service, int bookId, int userId)
        {
            try
            {
                await service.BorrowAsync(bookId, userId);
                return true;
            }
            catch (ConflictException)
            {
                return false;
            }
        }
    }

    [Fact]
    public async Task Return_frigör_exemplaret_för_nästa_låntagare()
    {
        var (bookId, _) = await GivenBookWithCopies(1);
        var first = await GivenUser();
        var second = await GivenUser();

        await using var db = fixture.CreateContext();
        var service = new LoanService(db, _clock);

        var loan = await service.BorrowAsync(bookId, first);
        _clock.Advance(TimeSpan.FromDays(3));
        await service.ReturnAsync(loan.Id, first);

        var next = await service.BorrowAsync(bookId, second);
        Assert.True(next.IsActive);
    }

    [Fact]
    public async Task Return_av_redan_återlämnat_lån_ger_konflikt()
    {
        var (bookId, _) = await GivenBookWithCopies(1);
        var userId = await GivenUser();

        await using var db = fixture.CreateContext();
        var service = new LoanService(db, _clock);

        var loan = await service.BorrowAsync(bookId, userId);
        await service.ReturnAsync(loan.Id, userId);

        await Assert.ThrowsAsync<ConflictException>(
            () => service.ReturnAsync(loan.Id, userId));
    }

    [Fact]
    public async Task Return_av_annans_lån_nekas()
    {
        var (bookId, _) = await GivenBookWithCopies(1);
        var owner = await GivenUser();
        var stranger = await GivenUser();

        await using var db = fixture.CreateContext();
        var service = new LoanService(db, _clock);
        var loan = await service.BorrowAsync(bookId, owner);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.ReturnAsync(loan.Id, stranger));
    }

    [Fact]
    public async Task Renew_räknar_från_förfallodatumet()
    {
        var (bookId, _) = await GivenBookWithCopies(1);
        var userId = await GivenUser();

        await using var db = fixture.CreateContext();
        var service = new LoanService(db, _clock);

        var loan = await service.BorrowAsync(bookId, userId);
        var originalDue = loan.DueAt;

        _clock.Advance(TimeSpan.FromDays(5));
        var renewed = await service.RenewAsync(loan.Id, userId);

        Assert.Equal(originalDue.AddDays(LoanPolicy.RenewalDays), renewed.DueAt);
        Assert.Equal(1, renewed.RenewalCount);
    }

    [Fact]
    public async Task Renew_nekas_för_försenat_lån()
    {
        var (bookId, _) = await GivenBookWithCopies(1);
        var userId = await GivenUser();

        await using var db = fixture.CreateContext();
        var service = new LoanService(db, _clock);
        var loan = await service.BorrowAsync(bookId, userId);

        _clock.Advance(TimeSpan.FromDays(LoanPolicy.LoanPeriodDays + 2));

        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => service.RenewAsync(loan.Id, userId));
        Assert.Contains("försenat", ex.Message);
    }

    [Fact]
    public async Task Renew_nekas_efter_maxantalet_förlängningar()
    {
        var (bookId, _) = await GivenBookWithCopies(1);
        var userId = await GivenUser();

        await using var db = fixture.CreateContext();
        var service = new LoanService(db, _clock);
        var loan = await service.BorrowAsync(bookId, userId);

        for (var i = 0; i < LoanPolicy.MaxRenewals; i++)
            await service.RenewAsync(loan.Id, userId);

        await Assert.ThrowsAsync<ConflictException>(
            () => service.RenewAsync(loan.Id, userId));
    }

    private async Task<(int BookId, List<int> CopyIds)> GivenBookWithCopies(int copies)
    {
        await using var db = fixture.CreateContext();

        var book = new Book
        {
            Title = $"Testbok {Guid.NewGuid():N}"[..20],
            PublishedYear = 2020,
            Pages = 300,
            Author = new Author { Name = "Testförfattare" },
            Genre = new Genre { Name = $"Genre {Guid.NewGuid():N}"[..14] },
        };

        db.Books.Add(book);
        await db.SaveChangesAsync();

        var copyList = Enumerable.Range(0, copies)
            .Select(i => new BookCopy
            {
                BookId = book.Id,
                Barcode = $"T-{Guid.NewGuid():N}"[..16],
                AcquiredAt = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            })
            .ToList();

        db.BookCopies.AddRange(copyList);
        await db.SaveChangesAsync();

        return (book.Id, copyList.Select(c => c.Id).ToList());
    }

    private async Task<int> GivenUser()
    {
        await using var db = fixture.CreateContext();

        var user = new User
        {
            FirstName = "Test",
            LastName = "Låntagare",
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }
}