import { LoanActions } from "@/app/loans/LoanActions";
import { BookCarousel } from "@/components/BookCarousel/BookCarousel";
import { DataTable } from "@/components/DataTable/DataTable";
import { api } from "@/lib/api";
import { ROUTES } from "@/lib/routes";
import { LoanItem } from "@/types/general";
import { loanColumns } from "./loanColumns";

export default async function Loans() {
  const data = await api<LoanItem[]>("/api/me/loans");
  const activeLoans = data.filter((loan) => !loan.returnedAt);
  const inactiveLoans = data.filter((loan) => loan.returnedAt);

  return (
    <div className="flex flex-col space-y-4 h-[calc(100vh-var(--header-height)-6rem)] w-[calc(100vw-var(--menu-width)-6rem)]">
      <div className="text-lg pt-4 font-bold">Aktiva lån</div>
      {activeLoans.length === 0 && <span>Inga aktiva lån</span>}
      {activeLoans.length > 0 && (
        <BookCarousel
          data={activeLoans}
          linkTo={(loan) => `${ROUTES.BOOKS}/${loan.bookId}`}
          renderDetails={(loan) => (
            <div className="flex flex-col space-y-2">
              <span className="text-sm">
                Återlämnas {new Date(loan.dueAt).toLocaleDateString("sv-SE")}
              </span>
              <LoanActions loan={loan} />
            </div>
          )}
        />
      )}

      <div className="text-lg pt-4 font-bold">Tidigare lån</div>
      <div className="rounded-md border overflow-auto min-h-0">
        <DataTable columns={loanColumns} data={inactiveLoans} />
      </div>
    </div>
  );
}
