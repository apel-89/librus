import { ReadingTimeEstimate } from "@/types/general";

export function ReadingTime({ estimate }: { estimate: ReadingTimeEstimate }) {
  const hours = Math.floor(estimate.minutes / 60);
  const minutes = estimate.minutes % 60;

  const label =
    hours === 0
      ? `${minutes} min`
      : minutes === 0
        ? `${hours} h`
        : `${hours} h ${minutes} min`;

  return (
    <p className="text-sm">
      <span className="font-medium">Uppskattad lästid: ca {label}</span>
      <span className="block text-gray-600">
        {estimate.source === "ReportedForBook"
          ? `Baserat på ${estimate.sampleSize} låntagares rapporterade lästid`
          : "Uppskattat från sidantal och bibliotekets medianlästakt"}
      </span>
    </p>
  );
}
