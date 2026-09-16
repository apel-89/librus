import { BookActions } from "@/app/books/[id]/BookActions";
import { ReadingTime } from "@/app/books/[id]/ReadingTime";
import { Recommendations } from "@/app/books/[id]/Recommendations";
import { Reviews } from "@/app/books/[id]/Reviews";
import { api, ApiError } from "@/lib/api";
import { getCoverUrl } from "@/lib/utils";
import { BookDetail, Me } from "@/types/general";
import { Metadata } from "next";
import Image from "next/image";
import { notFound } from "next/navigation";
import { Suspense } from "react";
import { getBook } from "./getBook";

interface BookPageProps {
  params: Promise<{ id: string }>;
}

export async function generateMetadata({
  params,
}: BookPageProps): Promise<Metadata> {
  const { id } = await params;
  const book = await getBook(id);

  if (!book) return { title: "Boken hittades inte" };

  return {
    title: book.title,
    description:
      book.description?.slice(0, 155) ?? `${book.title} av ${book.author}`,
    openGraph: {
      title: `${book.title} · Librus`,
      images: book.coverId ? [getCoverUrl(book.coverId, "L")!] : [],
    },
  };
}

export default async function Books({ params }: BookPageProps) {
  const { id } = await params;
  const [me, book] = await Promise.all([
    api<Me>(`/api/me`).catch((e) => {
      if (e instanceof ApiError && e.status === 404) return null;
      throw e;
    }),
    api<BookDetail>(`/api/books/${id}`).catch((e) => {
      if (e instanceof ApiError && e.status === 404) return null;
      throw e;
    }),
  ]);

  if (!book) notFound();

  const imageUrl = book.coverId ? getCoverUrl(book.coverId, "L") : null;

  return (
    <>
      <div className="flex gap-8">
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
              <div className="h-112 w-75 rounded-sm bg-gray-200" />
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

            <BookActions
              bookId={book.id}
              myActiveLoanId={book.myActiveLoanId}
              copiesAvailable={book.copiesAvailable}
              canBorrow={Boolean(me && me.canBorrow)}
              blockedReason={me?.blockedReason}
            />
          </div>
        </article>
        <Reviews
          reviews={book.reviews}
          averageScore={book.averageScore}
          feedbackCount={book.feedbackCount}
        />
      </div>
      <Suspense
        fallback={<div className="mt-12 h-88 animate-pulse rounded bg-muted" />}
      >
        <Recommendations bookId={book.id} />
      </Suspense>
    </>
  );
}
