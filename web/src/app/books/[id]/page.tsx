import { BookActions } from "@/app/books/[id]/BookActions";
import { ReadingTime } from "@/app/books/[id]/ReadingTime";
import { Recommendations } from "@/app/books/[id]/Recommendations";
import { Reviews } from "@/app/books/[id]/Reviews";
import { api } from "@/lib/api";
import { getCoverUrl } from "@/lib/utils";
import { BookDetail } from "@/types/general";
import Image from "next/image";
import { notFound } from "next/navigation";
import { Suspense } from "react";

interface BookPageProps {
  params: Promise<{ id: string }>;
}

export default async function BookPage({ params }: BookPageProps) {
  const { id } = await params;

  const book = await api<BookDetail>(`/api/books/${id}`).catch(() => null);
  if (!book) notFound();
  console.log(book);

  const imageUrl = book.coverId ? getCoverUrl(book.coverId, "L") : null;

  return (
    <>
      <article className="flex gap-8 p-8">
        <div className="shrink-0">
          {imageUrl ? (
            <Image
              src={imageUrl}
              alt=""
              width={300}
              height={450}
              className="rounded-sm border object-cover"
            />
          ) : (
            <div className="h-[450px] w-[300px] rounded-sm bg-gray-200" />
          )}
        </div>

        <div className="flex flex-col gap-4">
          <div>
            <h1 className="text-2xl font-bold">{book.title}</h1>
            <p className="text-gray-600">{book.author}</p>
          </div>

          <dl className="grid grid-cols-[auto_1fr] gap-x-4 gap-y-1 text-sm">
            <dt className="text-gray-600">Genre</dt>
            <dd>{book.genre}</dd>

            <dt className="text-gray-600">Utgiven</dt>
            <dd>{book.publishedYear}</dd>

            <dt className="text-gray-600">Längd</dt>
            <dd>{book.pages} sidor</dd>

            <dt className="text-gray-600">Tillgängliga</dt>
            <dd>
              {book.copiesAvailable} av {book.copiesTotal}
            </dd>
          </dl>

          {book.readingTime && <ReadingTime estimate={book.readingTime} />}

          {book.description && (
            <p className="max-w-prose text-sm leading-relaxed">
              {book.description}
            </p>
          )}

          <BookActions book={book} />
        </div>
        <Reviews
          reviews={book.reviews}
          averageScore={book.averageScore}
          feedbackCount={book.feedbackCount}
        />
      </article>
      <Suspense
        fallback={<div className="mt-12 h-88 animate-pulse rounded bg-muted" />}
      >
        <Recommendations bookId={book.id} />
      </Suspense>
    </>
  );
}
