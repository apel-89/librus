using Librus.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Librus.Api.Data;

public class LibrusDbContext(DbContextOptions<LibrusDbContext> options)
    : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();
    public DbSet<BookCopy> BookCopies => Set<BookCopy>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<UserFeedback> Feedback => Set<UserFeedback>();
    public DbSet<ReadingTimeRow> ReadingTimeRows => Set<ReadingTimeRow>();
    public DbSet<BookLoanCountRow> BookLoanCounts => Set<BookLoanCountRow>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Book>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(300).IsRequired();
            e.HasIndex(x => x.Title);
            e.HasOne(x => x.Author).WithMany(a => a.Books)
                .HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<BookCopy>(e =>
        {
            e.HasIndex(x => x.Barcode).IsUnique();
            e.HasIndex(x => x.BookId);
            e.HasOne(x => x.Book).WithMany(x => x.Copies)
                .HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Loan>(e =>
        {
            // Ett exemplar kan bara ha ett aktivt lån åt gången.
            e.HasIndex(x => x.CopyId)
                .IsUnique()
                .HasFilter("returned_at IS NULL")
                .HasDatabaseName("ix_loans_active_copy");

            e.HasIndex(x => new { x.UserId, x.ReturnedAt });
            e.HasIndex(x => x.BorrowedAt);

            e.HasOne(x => x.Copy).WithMany(c => c.Loans)
                .HasForeignKey(x => x.CopyId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<UserFeedback>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.BookId }).IsUnique();
            e.ToTable(t => t.HasCheckConstraint(
                "ck_feedback_score", "score BETWEEN 1 AND 10"));
            e.HasQueryFilter(x => x.DeletedAt == null);
        });

        b.Entity<ReadingTimeRow>().HasNoKey().ToView(null);
        b.Entity<BookLoanCountRow>().HasNoKey().ToView(null);
    }
}