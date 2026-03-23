import { TicketStatus } from "@acls/shared-types";

interface StatusBadgeProps {
  status: string;
}

const config: Record<TicketStatus, { label: string; className: string }> = {
  [TicketStatus.OPEN]: { label: "Open", className: "bg-blue-50 text-blue-800" },
  [TicketStatus.ASSIGNED]: { label: "Assigned", className: "bg-purple-50 text-purple-800" },
  [TicketStatus.EN_ROUTE]: { label: "En Route", className: "bg-indigo-50 text-indigo-800" },
  [TicketStatus.IN_PROGRESS]: { label: "In Progress", className: "bg-amber-50 text-amber-800" },
  [TicketStatus.RESOLVED]: { label: "Resolved", className: "bg-green-50 text-green-800" },
  [TicketStatus.CLOSED]: { label: "Closed", className: "bg-gray-100 text-gray-600" },
};

export function StatusBadge({ status }: StatusBadgeProps) {
  const { label, className } = config[status as TicketStatus] ?? {
    label: status,
    className: "bg-gray-100 text-gray-600",
  };
  return (
    <span
      className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${className}`}
    >
      {label}
    </span>
  );
}

