"use client";

import { Button } from "@/components/ui/button";
import { api } from "@/lib/api";
import { useApiAction } from "@/lib/use-api-action";
import type { BookDetail } from "@/types/general";

export function BookActions({ book }: { book: BookDetail }) {
  const { run, error, pending } = useApiAction();

  const isBorrowed = book.myActiveLoanId !== null;
  const soldOut = book.copiesAvailable === 0;

  return (
    <div className="flex flex-col gap-2">
      {isBorrowed ? (
        <Button
          disabled={pending}
          onClick={() =>
            run(() =>
              api(`/api/loans/${book.myActiveLoanId}/return`, {
                method: "POST",
              }),
            )
          }
        >
          Lämna tillbaka
        </Button>
      ) : (
        <Button
          disabled={pending || soldOut}
          onClick={() =>
            run(() =>
              api("/api/loans", {
                method: "POST",
                body: JSON.stringify({ bookId: book.id }),
              }),
            )
          }
        >
          {soldOut ? "Alla exemplar utlånade" : "Låna"}
        </Button>
      )}

      {error && (
        <p role="alert" className="text-sm text-destructive">
          {error}
        </p>
      )}
    </div>
  );
}
