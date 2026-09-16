import { BookCarousel } from "@/components/BookCarousel/BookCarousel";
import { api } from "@/lib/api";
import { ROUTES } from "@/lib/routes";
import { PopularBook } from "@/types/general";
import { Star } from "lucide-react";

export default async function Explore() {
  const data = await api<PopularBook[]>(`/api/books/popular`);

  return (
    <div className="flex items-center justify-center flex-col">
      <BookCarousel
        data={data}
        linkTo={(book) => ROUTES.BOOKS + `/${book.id}`}
        title="På topplistan"
        renderDetails={(book) => (
          <div className="flex items-center justify-center mt-4 gap-1">
            <Star className="fill-yellow-400 text-yellow-400 h-4" />
            <span className="text-sm text-gray-600">
              {book.averageScore?.toFixed(1) ?? "—"}
            </span>
          </div>
        )}
      />
    </div>
  );
}
