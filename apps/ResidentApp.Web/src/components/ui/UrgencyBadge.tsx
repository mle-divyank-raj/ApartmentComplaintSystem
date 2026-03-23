import { Urgency } from "@acls/shared-types";

interface UrgencyBadgeProps {
  urgency: string;
}

const config: Record<Urgency, { label: string; className: string }> = {
  [Urgency.LOW]: { label: "Low", className: "bg-green-50 text-green-800" },
  [Urgency.MEDIUM]: { label: "Medium", className: "bg-amber-50 text-amber-800" },
  [Urgency.HIGH]: { label: "High", className: "bg-orange-50 text-orange-800" },
  [Urgency.SOS_EMERGENCY]: {
    label: "EMERGENCY",
    className: "bg-red-50 text-red-800 font-semibold",
  },
};

export function UrgencyBadge({ urgency }: UrgencyBadgeProps) {
  const { label, className } = config[urgency as Urgency] ?? {
    label: urgency,
    className: "bg-gray-100 text-gray-600",
  };
  return (
    <span
      className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs ${className}`}
    >
      {label}
    </span>
  );
}

