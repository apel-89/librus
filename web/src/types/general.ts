export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export interface BookBase {
  id: number;
  title: string;
  author: string;
  coverId: number | null;
}

export interface BookListItem extends BookBase {
  averageScore: number | null;
  borrowedByMe: boolean;
  copiesAvailable: number;
  copiesTotal: number;
  genre: string;
  pages: number;
  publishedYear: number;
}

export interface LoanItem extends BookBase {
  barcode: string;
  bookId: number;
  borrowedAt: string;
  canRenew: boolean;
  dueAt: string;
  isOverdue: boolean;
  renewalCount: number;
  returnedAt: string | null;
}

export interface RecommendedBook extends BookBase {
  averageScore: number | null;
  copiesAvailable: number;
  copiesTotal: number;
  sharedBorrowers: number;
}

export interface PopularBook extends BookBase {
  averageScore: number | null;
  copiesAvailable: number;
  copiesTotal: number;
  coverId: number;
  genre: string;
  loanCount: number;
  rank: number;
}

export interface BookDetail extends BookBase {
  description: string | null;
  feedbackCount: number;
  myActiveLoanId: number | null;
  readingTime: ReadingTimeEstimate | null;
  reviews: Review[];
  averageScore: number | null;
  copiesAvailable: number;
  copiesTotal: number;
  genre: string;
  pages: number;
  publishedYear: number;
}

export interface ReadingTimeEstimate {
  minutes: number;
  source: ReadingTimeSource;
  sampleSize: number;
}

type ReadingTimeSource = "ReportedForBook" | "EstimatedFromPages";

export interface Review {
  by: string;
  createdAt: string;
  review: string;
  score: number;
}

export interface Me {
  activeLoanCount: number;
  blockedReason: BlockedReason | null;
  canBorrow: boolean;
  id: number;
  maxActiveLoans: number;
  name: string;
  overdueCount: number;
}

export type BlockedReason = "HasOverdueLoans" | "LoanLimitReached";
