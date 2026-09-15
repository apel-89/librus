"use client";

import { Drawer, DrawerContent, DrawerFooter } from "@/components/ui/drawer";
import { RouteIcons, ROUTES } from "@/lib/routes";
import { cn } from "cn";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { createElement } from "react";

const items = [
  { href: ROUTES.EXPLORE, label: "Upptäck" },
  { href: ROUTES.BOOKS, label: "Sök" },
  { href: ROUTES.LOANS, label: "Min bokhylla" },
];

export const Menu = () => {
  const pathname = usePathname();

  return (
    <div>
      <Drawer
        defaultOpen
        open={true}
        modal={false}
        swipeDirection="left"
        disablePointerDismissal
      >
        <DrawerContent className="fixed mt-(--header-height) w-(--menu-width)">
          <nav className="p-4 flex gap-4 flex-col">
            {items.map(({ href, label }) => (
              <Link
                key={href}
                href={href}
                aria-current={pathname === href ? "page" : undefined}
                className={cn(
                  pathname === href ? "font-semibold" : undefined,
                  "flex items-center",
                )}
              >
                {createElement(RouteIcons[href], {
                  className: "inline-block mr-2 h-4 w-4",
                })}
                {label}
              </Link>
            ))}
          </nav>
          <DrawerFooter>Profile</DrawerFooter>
        </DrawerContent>
      </Drawer>
    </div>
  );
};
