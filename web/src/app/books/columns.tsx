"use client";

import { DataTableFeatures } from "@/components/DataTable/data-table-features";
import { ROUTES } from "@/lib/routes";
import { getCoverUrl } from "@/lib/utils";
import { BookListItem } from "@/types/general";
import { createColumnHelper } from "@tanstack/react-table";
import Image from "next/image";
import Link from "next/link";

const helper = createColumnHelper<DataTableFeatures, BookListItem>();

export const bookColumns = helper.columns([
  helper.accessor("coverId", {
    header: "",
    cell: (info) => {
      const url = getCoverUrl(info.getValue(), "S");
      return url ? (
        <Image
          src={url}
          alt=""
          width={24}
          height={32}
          className="object-cover"
        />
      ) : (
        <div className="h-16 w-12 rounded bg-muted" />
      );
    },
  }),
  helper.accessor("title", {
    header: "Titel",
    cell: (info) => (
      <Link
        href={`${ROUTES.BOOKS}/${info.row.original.id}`}
        className="hover:underline"
      >
        {info.getValue()}
      </Link>
    ),
  }),
  helper.accessor("author", { header: "Författare" }),
  helper.accessor("publishedYear", { header: "Utgivningsår" }),
  helper.accessor("genre", { header: "Genre" }),
  helper.accessor("averageScore", {
    header: "Betyg",
    cell: (info) => info.getValue()?.toFixed(1) ?? "—",
  }),
  helper.accessor("copiesAvailable", {
    header: "Tillgängliga",
    cell: (info) => `${info.getValue()} av ${info.row.original.copiesTotal}`,
  }),
]);
