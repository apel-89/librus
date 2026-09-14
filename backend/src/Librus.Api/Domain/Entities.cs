namespace Librus.Api.Domain;

public class Author
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public ICollection<Book> Books { get; set; } = [];
}

public class Genre
{
    public int Id { get; set; }
    public required string Name { get; set; }
}

public class Book
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public int PublishedYear { get; set; }
    public int Pages { get; set; }

    public int AuthorId { get; set; }
    public Author Author { get; set; } = null!;

    public int GenreId { get; set; }
    public Genre Genre { get; set; } = null!;

    public ICollection<BookCopy> Copies { get; set; } = [];
}

public class BookCopy
{
    public int Id { get; set; }
    public required string Barcode { get; set; }
    public DateTime AcquiredAt { get; set; }

    public int BookId { get; set; }
    public Book Book { get; set; } = null!;

    public ICollection<Loan> Loans { get; set; } = [];
}

public class User
{
    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public DateTime CreatedAt { get; set; }
}