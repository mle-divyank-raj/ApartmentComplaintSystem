import type { ActiveAssignmentDto } from "@acls/api-contracts";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { UrgencyBadge } from "@/components/ui/UrgencyBadge";
import { Card } from "@/components/ui/Card";
import Link from "next/link";

interface ActiveAssignmentsProps {
  assignments: ActiveAssignmentDto[];
}

export function ActiveAssignments({ assignments }: ActiveAssignmentsProps) {
  return (
    <Card padding="none">
      <div className="border-b border-gray-200 px-5 py-3">
        <h3 className="text-sm font-semibold text-gray-900">
          Active Assignments ({assignments.length})
        </h3>
      </div>
      {assignments.length === 0 ? (
        <p className="px-5 py-8 text-center text-sm text-gray-500">
          No active assignments.
        </p>
      ) : (
        <ul className="divide-y divide-gray-100">
          {assignments.map((a) => (
            <li key={a.complaintId} className="flex items-center gap-3 px-5 py-3">
              <div className="min-w-0 flex-1">
                <Link
                  href={`/complaints/${a.complaintId}`}
                  className="truncate text-sm font-medium text-primary hover:underline"
                >
                  {a.title}
                </Link>
                <p className="mt-0.5 text-xs text-gray-500">
                  {a.unitNumber} · {a.buildingName} · {a.assignedStaffMember.fullName}
                </p>
              </div>
              <div className="flex shrink-0 items-center gap-2">
                <UrgencyBadge urgency={a.urgency} />
                <StatusBadge status={a.status} />
              </div>
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}
