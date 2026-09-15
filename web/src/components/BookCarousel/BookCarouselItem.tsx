import { CarouselItem } from "@/components/ui/carousel";
import { getCoverUrl } from "@/lib/utils";
import { BookBase } from "@/types/general";
import Image from "next/image";
import Link from "next/link";

interface BookCarouselItemProps<T extends BookBase> {
  book: T;
  renderDetails?: (book: T) => React.ReactNode;
  linkTo: string;
}

export const BookCarouselItem = <T extends BookBase>({
  book,
  renderDetails,
  linkTo,
}: BookCarouselItemProps<T>) => {
  const imageUrl = book.coverId ? getCoverUrl(book.coverId, "M") : null;
  return (
    <CarouselItem className="group relative flex flex-col basis-1/4 h-88 w-40 p-4 hover:bg-gray-50 border rounded-md">
      <div className="flex flex-1 flex-col items-center justify-between min-h-0">
        {imageUrl ? (
          <Image
            width={120}
            height={180}
            src={imageUrl}
            alt=""
            className="object-cover group-hover:scale-105 transition-transform duration-300 rounded-sm border"
          />
        ) : (
          <div className="w-30 h-45 bg-gray-200 ..." />
        )}
        <div className="mt-2 text-center flex flex-col">
          <Link
            href={linkTo}
            className="font-semibold after:absolute after:inset-0 after:content-['']"
          >
            {book.title}
          </Link>
          <span className="text-sm text-gray-600">{book.author}</span>
        </div>
      </div>

      {renderDetails && (
        <div className="relative z-10 mt-2 shrink-0 text-center">
          {renderDetails(book)}
        </div>
      )}
    </CarouselItem>
  );
};
