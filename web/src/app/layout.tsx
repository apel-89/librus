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
        <header className="h-(--header-height) flex items-center justify-center bg-amber-300 ">
          <span className="text-xl font-thin text-yellow-950 font-serif">
            Librus
          </span>
        </header>
        <Menu />
        <div className="flex flex-col flex-1 items-center justify-center bg-zinc-50 font-sans">
          <main className="flex flex-1 w-full max-w-3xl flex-col items-center justify-between py-32 px-16 bg-white sm:items-start">
            {children}
          </main>
        </div>
      </body>
    </html>
  );
}
