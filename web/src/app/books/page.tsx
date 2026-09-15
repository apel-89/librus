import { bookColumns } from "@/app/books/columns";
import { SearchInput } from "@/app/books/SearchInput";
import { DataTable } from "@/components/DataTable/DataTable";
import { Pagination } from "@/components/DataTable/Pagination";
import { api } from "@/lib/api";
import { BookListItem, PagedResult } from "@/types/general";

export const PAGE_SIZES = [5, 10, 20, 100];
const DEFAULT_PAGE_SIZE = 20;

interface ExploreProps {
  searchParams: Promise<{ page?: string; pageSize?: string; search?: string }>;
}

export default async function Explore({ searchParams }: ExploreProps) {
  const { page, pageSize, search } = await searchParams;
  const currentPage = Math.max(1, Number(page) || 1);
  const requested = Number(pageSize);
  const size = PAGE_SIZES.includes(requested) ? requested : DEFAULT_PAGE_SIZE;

  const query = new URLSearchParams({
    page: String(currentPage),
    pageSize: String(size),
  });
  if (search) query.set("search", search);

  const data = await api<PagedResult<BookListItem>>(`/api/books?${query}`);

  if (search) query.set("search", search);

  return (
    <div className="flex flex-col space-y-4 h-[calc(100vh-var(--header-height)-6rem)] w-[calc(100vw-var(--menu-width)-6rem)]">
      <div className="w-full flex justify-center items-center py-10 shrink-0">
        <SearchInput initial={search ?? ""} />
      </div>

      <div className="rounded-md border overflow-auto min-h-0">
        <DataTable columns={bookColumns} data={data.items} />
      </div>

      <Pagination
        page={data.page}
        pageSize={data.pageSize}
        total={data.total}
      />
    </div>
  );
}
