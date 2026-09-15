import { BookCarouselItem } from "@/components/BookCarousel/BookCarouselItem";
import {
  Carousel,
  CarouselContent,
  CarouselNext,
  CarouselPrevious,
} from "@/components/ui/carousel";
import { BookBase } from "@/types/general";

interface BookCarouselProps<T extends BookBase> {
  title?: string;
  data: T[];
  renderDetails?: (book: T) => React.ReactNode;
  linkTo: (book: T) => string;
}

export const BookCarousel = <T extends BookBase>({
  data,
  title,
  renderDetails,
  linkTo,
}: BookCarouselProps<T>) => {
  return (
    <div>
      {title && <h2 className="text-3xl font-semibold my-10">{title}</h2>}
      <Carousel
        opts={{
          align: "start",
        }}
        className="max-w-5xl"
      >
        <CarouselContent className="px-4 flex gap-4">
          {data?.map((book) => (
            <BookCarouselItem
              key={book.id}
              book={book}
              renderDetails={renderDetails}
              linkTo={linkTo(book)}
            />
          ))}
        </CarouselContent>
        <CarouselPrevious />
        <CarouselNext />
      </Carousel>
    </div>
  );
};
