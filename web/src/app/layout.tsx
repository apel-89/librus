import { Menu } from "@/components/Menu";
import { cn } from "@/lib/utils";
import { Geist } from "next/font/google";
import "./globals.css";

const geist = Geist({ subsets: ["latin"], variable: "--font-sans" });

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html
      lang="en"
      className={cn("h-full antialiased", "font-sans", geist.variable)}
    >
      <body className="relative min-h-full flex flex-col">
        <header className="fixed w-full h-(--header-height) flex items-center justify-center bg-yellow-200 bg-linear-to-r from-amber-200 to-transparent z-20">
          <span className="text-xl font-thin text-yellow-950 font-serif bg-white/60 rounded-lg px-8 shadow-sm">
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
