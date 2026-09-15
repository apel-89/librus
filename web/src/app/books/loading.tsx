export default function Loading() {
  return (
    <div className="space-y-2">
      {Array.from({ length: 8 }).map((_, i) => (
        <div key={i} className="h-16 animate-pulse rounded bg-muted" />
      ))}
    </div>
  );
}
