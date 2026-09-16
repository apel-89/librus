"use client";

import { Button } from "@/components/ui/button";
import { api } from "@/lib/api";
import { useApiAction } from "@/lib/use-api-action";
import type { BlockedReason } from "@/types/general";

const buttonMessage: Record<BlockedReason | "Other", string> = {
  HasOverdueLoans: "Du har försenade lån",
  LoanLimitReached: "Du har nått lånegränsen",
  Other: "Något gick fel",
};

export function BookActions({
  canBorrow,
  blockedReason,
  bookId,
  myActiveLoanId,
  copiesAvailable,
}: {
  bookId: number;
  myActiveLoanId: number | null;
  copiesAvailable: number;
  canBorrow: boolean;
  blockedReason: BlockedReason | null | undefined;
}) {
  const { run, error, pending } = useApiAction();

  const isBorrowed = myActiveLoanId !== null;
  const soldOut = copiesAvailable === 0;

  return (
    <div className="flex flex-col gap-2">
      {isBorrowed ? (
        <Button
          disabled={pending}
          onClick={() =>
            run(() =>
              api(`/api/loans/${myActiveLoanId}/return`, {
                method: "POST",
              }),
            )
          }
        >
          Lämna tillbaka
        </Button>
      ) : (
        <Button
          disabled={pending || soldOut || !canBorrow}
          onClick={() =>
            run(() =>
              api("/api/loans", {
                method: "POST",
                body: JSON.stringify({ bookId }),
              }),
            )
          }
        >
          {soldOut
            ? "Alla exemplar utlånade"
            : !canBorrow
              ? buttonMessage[blockedReason ?? "Other"]
              : "Låna"}
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
