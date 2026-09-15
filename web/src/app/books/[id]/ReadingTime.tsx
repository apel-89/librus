import { ReadingTimeEstimate } from "@/types/general";

export function ReadingTime({ estimate }: { estimate: ReadingTimeEstimate }) {
  const hours = Math.round(estimate.minutes / 60);

  return (
    <p className="text-sm">
      <span className="font-medium">Uppskattad lästid: ca {hours} timmar</span>
      <span className="block text-gray-600">
        {estimate.source === "ReportedForBook"
          ? `Baserat på ${estimate.sampleSize} låntagares rapporterade lästid`
          : "Uppskattat från sidantal och bibliotekets genomsnittliga lästakt"}
      </span>
    </p>
  );
}
