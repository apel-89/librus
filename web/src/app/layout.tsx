import { Menu } from "@/components/Menu";
import { cn } from "@/lib/utils";
import type { Metadata } from "next";
import { Geist, Instrument_Serif } from "next/font/google";
import "./globals.css";

const geist = Geist({ subsets: ["latin"], variable: "--font-sans" });
const logo = Instrument_Serif({
  subsets: ["latin"],
  weight: "400",
  variable: "--font-logo",
});

export const metadata: Metadata = {
  title: {
    default: "Librus",
    template: "%s · Librus",
  },
  description: "Låna böcker ur Librus katalog.",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html
      lang="sv"
      className={cn(
        "h-full antialiased",
        "font-sans",
        geist.variable,
        logo.variable,
      )}
    >
      <body className="relative min-h-full flex flex-col">
        <header className="fixed w-full h-(--header-height) flex items-center justify-center from-emerald-700 bg-linear-to-r bg-green-800 to-transparent z-20">
          <span className="font-logo text-2xl tracking-wide text-green-50">
            Librus
          </span>
        </header>
        <Menu />
        <div className="flex flex-col flex-1 justify-center font-sans ml-(--menu-width)">
          <main className="absolute flex flex-1 flex-col justify-between pt-2 px-12 top-(--header-height) w-[calc(100%-var(--menu-width))]">
            {children}
          </main>
        </div>
      </body>
    </html>
  );
}
