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
  coverId: number;
}

export interface BookListItem extends BookBase {
  averageScore: number;
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
  returnedAt: string;
}

export interface RecommendedBook extends BookBase {
  averageScore: number;
  copiesAvailable: number;
  copiesTotal: number;
  sharedBorrowers: number;
}

export interface BookDetail extends BookListItem {
  author: string;
  averageScore: number;
  copiesAvailable: number;
  copiesTotal: number;
  coverId: number;
  description: string;
  feedbackCount: number;
  genre: string;
  id: number;
  myActiveLoanId: number;
  pages: number;
  publishedYear: number;
  readingTime: ReadingTimeEstimate | null;
  reviews: Review[];
}

export interface ReadingTimeEstimate {
  minutes: number;
  source: "ReportedForBook" | "EstimatedFromPages";
  sampleSize: number;
}

export interface Review {
  by: string;
  createdAt: string;
  review: string;
  score: number;
}
