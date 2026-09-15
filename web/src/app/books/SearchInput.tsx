"use client";

import { Input } from "@/components/ui/input";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { useEffect, useRef, useState } from "react";

export function SearchInput({ initial }: { initial: string }) {
  const [value, setValue] = useState(initial);
  const router = useRouter();
  const pathname = usePathname();
  const params = useSearchParams();

  const mounted = useRef(false);

  useEffect(() => {
    if (!mounted.current) {
      mounted.current = true;
      return;
    }

    const timer = setTimeout(() => {
      const next = new URLSearchParams(params);

      if (value.trim()) next.set("search", value.trim());
      else next.delete("search");

      next.delete("page");

      router.replace(`${pathname}?${next}`);
    }, 350);

    return () => clearTimeout(timer);
  }, [value]);

  return (
    <Input
      id="search"
      type="search"
      value={value}
      onChange={(e) => setValue(e.target.value)}
      className="w-2xl h-12 px-4"
      placeholder="Hitta din nästa läsupplevelse"
    />
  );
}
