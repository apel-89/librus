import { BookCarousel } from "@/components/BookCarousel/BookCarousel";
import { api } from "@/lib/api";
import { ROUTES } from "@/lib/routes";
import type { RecommendedBook } from "@/types/general";

export async function Recommendations({ bookId }: { bookId: number }) {
  const books = await api<RecommendedBook[]>(
    `/api/books/${bookId}/recommendations`,
  );

  if (books.length === 0) return null;

  return (
    <section className="mt-12">
      <h2 className="mb-4 text-lg font-bold">Andra har också läst</h2>
      <BookCarousel
        data={books}
        linkTo={(book) => ROUTES.BOOKS + `/${book.id}`}
      />
    </section>
  );
}
