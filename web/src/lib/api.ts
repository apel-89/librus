const BASE = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5092";

export class ApiError extends Error {
  constructor(
    public status: number,
    public title: string,
    public detail: string,
  ) {
    super(detail || title);
  }
}

export async function api<T>(path: string, init: RequestInit = {}): Promise<T> {
  const res = await fetch(`${BASE}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...init.headers,
    },
    cache: "no-store",
  });

  if (!res.ok) {
    const problem = await res.json().catch(() => null);
    throw new ApiError(
      res.status,
      problem?.title ?? "Något gick fel",
      problem?.detail ?? "",
    );
  }

  return res.status === 204 ? (undefined as T) : res.json();
}
