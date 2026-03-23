import { Card } from "@/components/ui/Card";

interface MetricsCardProps {
  label: string;
  value: number;
  accent?: "default" | "warning" | "danger" | "success";
}

const accentClasses = {
  default: "text-gray-900",
  warning: "text-yellow-700",
  danger: "text-red-600",
  success: "text-green-700",
};

export function MetricsCard({
  label,
  value,
  accent = "default",
}: MetricsCardProps) {
  return (
    <Card>
      <p className="text-xs font-medium uppercase tracking-wide text-gray-500">
        {label}
      </p>
      <p className={["mt-2 text-3xl font-bold", accentClasses[accent]].join(" ")}>
        {value}
      </p>
    </Card>
  );
}
