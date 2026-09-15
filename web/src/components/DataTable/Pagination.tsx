"use client";

import { PAGE_SIZES } from "@/app/books/page";
import { buttonVariants } from "@/components/ui/button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { cn } from "cn";
import Link from "next/link";
import { usePathname, useRouter, useSearchParams } from "next/navigation";

export function Pagination({
  page,
  pageSize,
  total,
}: {
  page: number;
  pageSize: number;
  total: number;
}) {
  const pathname = usePathname();
  const params = useSearchParams();
  const lastPage = Math.max(1, Math.ceil(total / pageSize));
  const router = useRouter();

  const isOnFirstPage = page <= 1;
  const isOnLastPage = page >= lastPage;

  const hrefWith = (updates: Record<string, string>) => {
    const next = new URLSearchParams(params);
    for (const [key, value] of Object.entries(updates)) next.set(key, value);
    return `${pathname}?${next}`;
  };

  const changePageSize = (value: string | null) => {
    if (!value) return;
    router.push(hrefWith({ pageSize: value, page: "1" }));
  };

  const linkClass = (disabled: boolean) =>
    cn(
      buttonVariants({ variant: "outline", size: "sm" }),
      "cursor-default",
      disabled &&
        "text-muted-foreground hover:bg-transparent hover:text-muted-foreground",
    );

  return (
    <div className="flex items-center justify-end gap-2 py-4">
      <span className="text-sm text-muted-foreground mr-auto">
        Sida {page} av {lastPage} · {total} böcker
      </span>

      <Select value={String(pageSize)} onValueChange={changePageSize}>
        <SelectTrigger size="sm" className="w-18" aria-label="Böcker per sida">
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          {PAGE_SIZES.map((size) => (
            <SelectItem key={size} value={String(size)}>
              {size}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      <Link
        href={isOnFirstPage ? "#" : hrefWith({ page: String(page - 1) })}
        className={linkClass(isOnFirstPage)}
        aria-disabled={isOnFirstPage}
        prefetch={false}
      >
        Föregående
      </Link>

      <Link
        href={isOnLastPage ? "#" : hrefWith({ page: String(page + 1) })}
        className={linkClass(isOnLastPage)}
        aria-disabled={isOnLastPage}
        prefetch={false}
      >
        Nästa
      </Link>
    </div>
  );
}
