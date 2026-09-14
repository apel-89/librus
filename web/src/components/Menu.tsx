"use client";

import { Drawer, DrawerContent, DrawerFooter } from "@/components/ui/drawer";
import Link from "next/link";
import { usePathname } from "next/navigation";

const items = [
  { href: "/", label: "Upptäck" },
  { href: "/books", label: "Sök" },
  { href: "/loans", label: "Min bokhylla" },
];

export const Menu = () => {
  const pathname = usePathname();

  return (
    <Drawer
      defaultOpen
      swipeDirection="left"
      modal={false}
      disablePointerDismissal
    >
      <DrawerContent className="mt-(--header-height) w-(--menu-width)">
        <nav className="p-4 flex gap-4 flex-col">
          {items.map(({ href, label }) => (
            <Link
              key={href}
              href={href}
              aria-current={pathname === href ? "page" : undefined}
              className={pathname === href ? "font-semibold" : undefined}
            >
              {label}
            </Link>
          ))}
        </nav>
        <DrawerFooter>Profile</DrawerFooter>
      </DrawerContent>
    </Drawer>
  );
};
