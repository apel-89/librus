import { LucideIcon, Search, ShelvingUnit, Telescope } from "lucide-react";

type Routes = (typeof ROUTES)[keyof typeof ROUTES];

export const ROUTES = {
  EXPLORE: "/explore",
  BOOKS: "/books",
  LOANS: "/loans",
};

export const RouteIcons: Record<Routes, LucideIcon> = {
  [ROUTES.EXPLORE]: Telescope,
  [ROUTES.BOOKS]: Search,
  [ROUTES.LOANS]: ShelvingUnit,
};
