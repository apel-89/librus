"use client";

import { useRouter } from "next/navigation";
import { useState, useTransition } from "react";
import { ApiError } from "./api";

export function useApiAction() {
  const [error, setError] = useState<string | null>(null);
  const [isSending, setIsSending] = useState(false);
  const [isRefreshing, startTransition] = useTransition();
  const router = useRouter();

  async function run(action: () => Promise<unknown>) {
    setError(null);
    setIsSending(true);
    try {
      await action();
      startTransition(() => router.refresh());
    } catch (e) {
      setError(e instanceof ApiError ? e.message : "Något gick fel");
    } finally {
      setIsSending(false);
    }
  }

  return { run, error, pending: isSending || isRefreshing };
}
