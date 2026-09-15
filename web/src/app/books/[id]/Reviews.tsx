import type { Review } from "@/types/general";
import { Star } from "lucide-react";

export function Reviews({
  reviews,
  averageScore,
  feedbackCount,
}: {
  reviews: Review[];
  averageScore: number | null;
  feedbackCount: number;
}) {
  return (
    <section className="mt-12">
      <div className="mb-4 flex items-baseline gap-3">
        <h2 className="text-lg font-bold">Läsarnas omdömen</h2>
        {averageScore !== null && (
          <span className="text-sm text-gray-600">
            {averageScore.toFixed(1)} av 10 · {feedbackCount}{" "}
            {feedbackCount === 1 ? "omdöme" : "omdömen"}
          </span>
        )}
      </div>

      {reviews.length === 0 ? (
        <p className="text-sm text-gray-600">
          Ingen har skrivit något om den här boken än.
        </p>
      ) : (
        <ul className="flex flex-col gap-4">
          {reviews.map((review, i) => (
            <li key={i} className="max-w-prose border-l-2 pl-4">
              <div className="mb-1 flex items-center gap-2">
                <Score value={review.score} />
                <span className="text-sm font-medium">{review.by}</span>
                <span className="text-sm text-gray-600">
                  {new Date(review.createdAt).toLocaleDateString("sv-SE")}
                </span>
              </div>
              <p className="text-sm leading-relaxed">{review.review}</p>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}

function Score({ value }: { value: number }) {
  const stars = Math.round(value / 2);

  return (
    <span className="flex gap-0.5" aria-label={`${value} av 10`}>
      {Array.from({ length: 5 }).map((_, i) => (
        <Star
          key={i}
          size={14}
          className={
            i < stars ? "fill-yellow-400 text-yellow-400" : "text-gray-300"
          }
        />
      ))}
    </span>
  );
}
