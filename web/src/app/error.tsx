"use client";

export default function ErrorPage() {
  return (
    <div className="flex flex-col items-center justify-center min-h-screen">
      <h1 className="text-4xl font-bold">Något gick fel</h1>
      <p className="mt-4 text-lg">Försök igen senare.</p>
    </div>
  );
}
