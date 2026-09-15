export { cn } from "cn";

export const getCoverUrl = (
  coverId: number | null,
  size: "S" | "M" | "L" = "M",
) =>
  coverId ? `https://covers.openlibrary.org/b/id/${coverId}-${size}.jpg` : null;
