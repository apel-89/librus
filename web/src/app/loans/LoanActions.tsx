"use client";

import { Button } from "@/components/ui/button";
import { api } from "@/lib/api";
import { useApiAction } from "@/lib/use-api-action";
import type { LoanItem } from "@/types/general";

export function LoanActions({ loan }: { loan: LoanItem }) {
  const { run, error, pending } = useApiAction();

  return (
    <div className="flex flex-col space-y-2">
      <div className="flex space-x-2">
        <Button
          variant="outline"
          disabled={!loan.canRenew || pending}
          onClick={() =>
            run(() => api(`/api/loans/${loan.id}/renew`, { method: "POST" }))
          }
        >
          Förläng
        </Button>

        <Button
          disabled={pending}
          onClick={() =>
            run(() => api(`/api/loans/${loan.id}/return`, { method: "POST" }))
          }
        >
          {pending ? "Skickar…" : "Lämna tillbaka"}
        </Button>
      </div>

      {error && (
        <p role="alert" className="text-sm text-destructive">
          {error}
        </p>
      )}
    </div>
  );
}
