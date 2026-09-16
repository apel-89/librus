import { api, ApiError } from "@/lib/api";
import type { BookDetail } from "@/types/general";
import { cache } from "react";

export const getBook = cache(async (id: string) =>
  api<BookDetail>(`/api/books/${id}`).catch((e) => {
    if (e instanceof ApiError && e.status === 404) return null;
    throw e;
  }),
);
