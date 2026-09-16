using System.Text.Json;
using Librus.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Librus.Api.Data.Seed;

/// <summary>
/// Fyller databasen med utgångsdata. Böckerna kommer från Seed/books.json
/// (hämtade från Open Library), medan användare, exemplar, lån och feedback
/// genereras relativt aktuell tid — så att topplistans
/// tidsfönster och försenade lån alltid är aktuella när appen körs.
/// </summary>
public static class DatabaseSeeder
{
    private const int RandomSeed = 42;
    private const int HistoryMonths = 24;
    private const int UserCount = 25;
    private const int CurrentUserId = 1;

    public static async Task SeedAsync(LibrusDbContext db, CancellationToken ct = default)
    {
        if (await db.Books.AnyAsync(ct)) return;

        var rng = new Random(RandomSeed);
        var now = DateTime.UtcNow;
        var windowStart = now.AddMonths(-HistoryMonths);

        var source = await LoadBooksAsync(ct);

        var genres = SeedGenres(db, source);
        var authors = SeedAuthors(db, source);
        var books = SeedBooks(db, source, genres, authors);
        await db.SaveChangesAsync(ct);

        var copies = SeedCopies(db, books, rng);
        var users = SeedUsers(db, rng, windowStart);
        await db.SaveChangesAsync(ct);

        var readers = BuildReaderProfiles(users, genres.Values.ToList(), rng);
        var loans = GenerateLoans(copies, books, readers, rng, windowStart, now);
        EnsureCurrentUserHasInterestingState(loans, rng, now);

        db.Loans.AddRange(loans);
        await db.SaveChangesAsync(ct);

        db.Feedback.AddRange(GenerateFeedback(loans, copies, books, readers, rng, now));
        await db.SaveChangesAsync(ct);
    }

    // ---------- Inläsning ----------

    private sealed record BookSource(
        string Title,
        string AuthorName,
        string? Description,
        int PublishedYear,
        int Pages,
        string Genre, 
        int? CoverId);

    private sealed record SeedFile(List<string> Genres, List<BookSource> Books);

    private static async Task<SeedFile> LoadBooksAsync(CancellationToken ct)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Seed", "books.json");
        if (!File.Exists(path))
            throw new FileNotFoundException(
                $"Hittade inte {path}", path);

        await using var stream = File.OpenRead(path);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return await JsonSerializer.DeserializeAsync<SeedFile>(stream, options, ct)
               ?? throw new InvalidOperationException("books.json kunde inte tolkas.");
    }

    // ---------- Grunddata ----------

    private static Dictionary<string, Genre> SeedGenres(LibrusDbContext db, SeedFile source)
    {
        var genres = source.Genres.ToDictionary(
            name => name,
            name => new Genre { Name = name });

        db.Genres.AddRange(genres.Values);
        return genres;
    }

    private static Dictionary<string, Author> SeedAuthors(LibrusDbContext db, SeedFile source)
    {
        var authors = source.Books
            .Select(b => b.AuthorName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                name => name,
                name => new Author { Name = name },
                StringComparer.OrdinalIgnoreCase);

        db.Authors.AddRange(authors.Values);
        return authors;
    }

    private static List<Book> SeedBooks(
        LibrusDbContext db,
        SeedFile source,
        Dictionary<string, Genre> genres,
        Dictionary<string, Author> authors)
    {
        var books = source.Books
            .Where(b => genres.ContainsKey(b.Genre))
            .Select(b => new Book
            {
                Title = b.Title,
                Description = b.Description,
                PublishedYear = b.PublishedYear,
                Pages = b.Pages,
                Author = authors[b.AuthorName],
                Genre = genres[b.Genre],
                CoverId = b.CoverId
            })
            .ToList();

        db.Books.AddRange(books);
        return books;
    }

    /// <summary>
    /// Populära böcker får fler exemplar. Popularitetsfaktorn återanvänds
    /// sedan för att styra hur ofta boken lånas ut.
    /// </summary>
    private static List<BookCopy> SeedCopies(LibrusDbContext db, List<Book> books, Random rng)
    {
        var copies = new List<BookCopy>();
        var barcode = 100_000;

        foreach (var book in books)
        {
            var popularity = Popularity(book, books);
            var count = popularity switch
            {
                > 0.75 => rng.Next(3, 5),
                > 0.45 => rng.Next(2, 4),
                _ => rng.Next(1, 3),
            };

            for (var i = 0; i < count; i++)
            {
                copies.Add(new BookCopy
                {
                    Book = book,
                    Barcode = $"LIB-{barcode++}",
                    AcquiredAt = DateTime.UtcNow.AddMonths(-rng.Next(24, 60)),
                });
            }
        }

        db.BookCopies.AddRange(copies);
        return copies;
    }

    /// <summary>Zipf-liknande fördelning: några få böcker står för en stor del av lånen.</summary>
    private static double Popularity(Book book, List<Book> books)
    {
        var hash = Math.Abs(book.Title.GetHashCode(StringComparison.Ordinal));
        var rank = hash % books.Count + 1;
        return 1.0 / Math.Pow(rank, 0.45);
    }

    private static readonly string[] FirstNames =
    [
        "Elsa", "Nils", "Maja", "Oskar", "Alva", "Hugo", "Vera", "Arvid", "Sigrid", "Melker",
        "Tuva", "Vidar", "Ingrid", "Folke", "Signe", "Ove", "Linnea", "Gustav", "Astrid",
        "Emil", "Saga", "Rasmus", "Britt", "Åke", "Nora", "Lars", "Karin", "Per", "Eva", "Jan",
    ];

    private static readonly string[] LastNames =
    [
        "Lindqvist", "Sandberg", "Hollander", "Ek", "Norell", "Wikander", "Bergström",
        "Falk", "Ohlsson", "Ranstam", "Dahlin", "Melin", "Sjöberg", "Åkesson", "Hedlund",
        "Ternström", "Lundgren", "Björk", "Kvist", "Almgren", "Rydell", "Palm", "Nyström",
        "Söderberg", "Ahlin", "Månsson", "Bergman"
    ];

    private static List<User> SeedUsers(LibrusDbContext db, Random rng, DateTime windowStart)
    {
        var users = Enumerable.Range(0, UserCount)
            .Select(i => new User
            {
                FirstName = FirstNames[i % FirstNames.Length],
                LastName = LastNames[i % LastNames.Length],
                CreatedAt = windowStart.AddDays(-rng.Next(0, 400)),
            })
            .ToList();

        db.Users.AddRange(users);
        return users;
    }

    // ---------- Läsarprofiler ----------

    /// <summary>
    /// Varje låntagare får ett par favoritgenrer och en läshastighet.
    /// Genrepreferenserna är det som gör att rekommendationerna får signal:
    /// utan dem blir lånemönstret brus och "andra lånade också" meningslöst.
    /// </summary>
    private sealed record Reader(User User, HashSet<int> FavouriteGenreIds, double MinutesPerPage);

    private static List<Reader> BuildReaderProfiles(List<User> users, List<Genre> genres, Random rng)
    {
        return users.Select(user =>
        {
            var favourites = new HashSet<int>();
            while (favourites.Count < 2)
                favourites.Add(genres[rng.Next(genres.Count)].Id);

            // 0,8–2,2 minuter per sida täcker snabbläsare till långsamläsare.
            var speed = 0.8 + rng.NextDouble() * 1.4;
            return new Reader(user, favourites, speed);
        }).ToList();
    }

    // ---------- Lån ----------

    /// <summary>
    /// Lånen byggs som en kedja per exemplar: ett lån i taget, framåt i tiden,
    /// med hylltid emellan. Det garanterar att det partiella unika indexet
    /// (ett aktivt lån per exemplar) aldrig kan brytas av seeddatan.
    /// </summary>
    private static List<Loan> GenerateLoans(
        List<BookCopy> copies,
        List<Book> books,
        List<Reader> readers,
        Random rng,
        DateTime windowStart,
        DateTime now)
    {
        var loans = new List<Loan>();

        foreach (var copy in copies)
        {
            var popularity = Popularity(copy.Book, books);
            var cursor = windowStart.AddDays(rng.Next(0, 60));

            while (cursor < now)
            {
                var reader = PickReader(readers, copy.Book.GenreId, rng);

                var readingDays = Math.Clamp(
                    (int)Math.Round(copy.Book.Pages * reader.MinutesPerPage / 45.0),
                    3, 70);
                readingDays += rng.Next(-2, 6);

                var borrowedAt = cursor;
                var loan = Loan.Start(copy.Id, reader.User.Id, borrowedAt);

                var returnedAt = borrowedAt.AddDays(readingDays);
                if (returnedAt >= now)
                {
                    // Lånet pågår fortfarande.
                    loans.Add(loan);
                    break;
                }

                loan.Return(returnedAt);
                loans.Add(loan);

                // Hylltid: populära böcker plockas upp igen snabbare.
                var shelfDays = (int)Math.Round(rng.Next(2, 45) * (1.2 - popularity));
                cursor = returnedAt.AddDays(Math.Max(1, shelfDays));
            }
        }

        return loans;
    }

    /// <summary>Fyra gånger högre sannolikhet att låna inom sin favoritgenre.</summary>
    private static Reader PickReader(List<Reader> readers, int genreId, Random rng)
    {
        var weights = readers
            .Select(r => r.FavouriteGenreIds.Contains(genreId) ? 4.0 : 1.0)
            .ToArray();

        var total = weights.Sum();
        var roll = rng.NextDouble() * total;

        for (var i = 0; i < readers.Count; i++)
        {
            roll -= weights[i];
            if (roll <= 0) return readers[i];
        }

        return readers[^1];
    }

    /// <summary>
    /// Den inloggade låntagaren ska ha något att titta på direkt: pågående lån
    /// och historik. Lånen finns redan — här flyttas bara ägarskapet.
    /// </summary>
    private static void EnsureCurrentUserHasInterestingState(
        List<Loan> loans, Random rng, DateTime now)
    {
        var active = loans.Where(l => l.IsActive && l.UserId != CurrentUserId).ToList();
        var returned = loans
            .Where(l => !l.IsActive && l.UserId != CurrentUserId && l.ReturnedAt > now.AddMonths(-10))
            .OrderBy(_ => rng.Next())
            .ToList();

        foreach (var loan in active.OrderBy(_ => rng.Next()).Take(2))
            loan.UserId = CurrentUserId;


        foreach (var loan in loans.Where(l => l.IsActive && l.UserId != CurrentUserId))
        {
            if (rng.NextDouble() > 0.08) continue;
            loan.BorrowedAt = now.AddDays(-LoanPolicy.LoanPeriodDays - rng.Next(1, 30));
            loan.DueAt = loan.BorrowedAt.AddDays(LoanPolicy.LoanPeriodDays);
        }

        foreach (var loan in returned.Take(6))
            loan.UserId = CurrentUserId;
    }

    // ---------- Feedback ----------

    private static List<UserFeedback> GenerateFeedback(
        List<Loan> loans,
        List<BookCopy> copies,
        List<Book> books,
        List<Reader> readers,
        Random rng,
        DateTime now)
    {
        var copyToBook = copies.ToDictionary(c => c.Id, c => c.Book);
        var readerByUser = readers.ToDictionary(r => r.User.Id, r => r);
        var feedback = new List<UserFeedback>();
        var seen = new HashSet<(int UserId, int BookId)>();

        foreach (var loan in loans.Where(l => !l.IsActive).OrderBy(l => l.ReturnedAt))
        {
            var book = copyToBook[loan.CopyId];
            if (!seen.Add((loan.UserId, book.Id))) continue;

            // Knappt två av tre avslutade lån ger feedback.
            if (rng.NextDouble() > 0.62) continue;

            var reader = readerByUser[loan.UserId];
            var likesGenre = reader.FavouriteGenreIds.Contains(book.GenreId);

            var created = loan.ReturnedAt!.Value.AddHours(rng.Next(1, 72));
            if (created > now) created = now;

            feedback.Add(new UserFeedback
            {
                BookId = book.Id,
                UserId = loan.UserId,
                Score = Math.Clamp((likesGenre ? 7 : 5) + rng.Next(-2, 4), 1, 10),
                Review = rng.NextDouble() < 0.35 ? null : PickReview(rng),
                // Faktisk lästid, inte lånetid: sidor gånger läsarens hastighet.
                ReadingMinutes = rng.NextDouble() < 0.2
                    ? null
                    : (int)Math.Round(book.Pages * reader.MinutesPerPage * (0.85 + rng.NextDouble() * 0.3)),
                CreatedAt = created,
                UpdatedAt = created,
            });
        }

        return feedback;
    }

    private static readonly string[] Reviews =
    [
        "Tog ett tag att komma in i, men blev bättre och bättre.",
        "Läste ut den på två kvällar. Rekommenderas.",
        "Bra idé, men mitten drar ut på tiden.",
        "Precis den sortens bok jag brukar tycka om.",
        "Snyggt språk, lite tunn handling.",
        "Bättre än jag väntade mig.",
        "Fastnade inte riktigt för den här.",
        "Kommer läsa om den.",
        "Slutet kom för snabbt.",
        "Perfekt längd, inget överflödigt.",
        "En riktig bladvändare.",
        "Kände mig inte engagerad i berättelsen.",
        "Vackert illustrerad, men handlingen saknas.",
        "En bok jag kommer minnas länge.",
        "Inte min typ av bok, men välskriven.",
        "Hade svårt att lägga ifrån mig den.",
        "Förutsägbar handling, men trevligt språk.",
        "En bok som växer vid omläsning.",
        "Karaktärerna kändes levande och trovärdiga.",
        "Boken hade en oväntad vändning som jag gillade.",
        "Språket var vackert, men handlingen kändes svag.",
        "En bok som verkligen fick mig att tänka.",
        "Jag kunde inte lägga ifrån mig den förrän sista sidan.",
        "Handlingens tempo var perfekt, inga tråkiga partier.",
        "Karaktärsutvecklingen var imponerande.",
        "Boken levde inte upp till mina förväntningar.",
        "En gripande berättelse som stannar kvar länge.",
        "Jag uppskattade de historiska detaljerna i boken.",
        "En bok som överraskade mig positivt.",
        "Språket var rikt och levande.",
        "Handlingens komplexitet höll mig engagerad.",
        "Karaktärerna kändes platta och orealistiska.",
        "En bok jag gärna rekommenderar till vänner.",
        "Berättelsen kändes långsam i början, men tog sig.",
        "Jag lärde mig mycket av den här boken.",
        "Slutet var oväntat och tillfredsställande.",
    ];

    private static string PickReview(Random rng) => Reviews[rng.Next(Reviews.Length)];
}