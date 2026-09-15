"use client";

import { DataTableFeatures } from "@/components/DataTable/data-table-features";
import { ROUTES } from "@/lib/routes";
import { LoanItem } from "@/types/general";
import { createColumnHelper } from "@tanstack/react-table";
import Link from "next/link";

const toDateString = (date?: string) =>
  date ? new Date(date).toLocaleDateString("sv-SE") : "-";

const helper = createColumnHelper<DataTableFeatures, LoanItem>();

export const loanColumns = helper.columns([
  helper.accessor("title", {
    header: "Titel",
    cell: (info) => (
      <Link
        href={`${ROUTES.BOOKS}/${info.row.original.bookId}`}
        className="hover:underline"
      >
        {info.getValue()}
      </Link>
    ),
  }),
  helper.accessor("author", { header: "Författare" }),
  helper.accessor("borrowedAt", {
    header: "Lånad",
    cell: (info) => toDateString(info.getValue()),
  }),
  helper.accessor("dueAt", {
    header: "Förfallodatum",
    cell: (info) => toDateString(info.getValue()),
  }),
  helper.accessor("returnedAt", {
    header: "Återlämnad",
    cell: (info) => toDateString(info.getValue()),
  }),
]);
