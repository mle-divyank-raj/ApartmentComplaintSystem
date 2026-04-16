import type { StaffAvailabilitySummaryDto } from "@acls/api-contracts";
import { Card } from "@/components/ui/Card";

interface StaffAvailabilitySummaryProps {
  staff: StaffAvailabilitySummaryDto[];
}

export function StaffAvailabilitySummary({
  staff,
}: StaffAvailabilitySummaryProps) {
  return (
    <Card padding="none">
      <div className="border-b border-gray-200 px-5 py-3">
        <h3 className="text-sm font-semibold text-gray-900">
          Staff Availability ({staff.length})
        </h3>
      </div>
      {staff.length === 0 ? (
        <p className="px-5 py-8 text-center text-sm text-gray-500">
          No staff data available.
        </p>
      ) : (
        <ul className="divide-y divide-gray-100">
          {staff.map((s) => (
            <li
              key={s.staffMemberId}
              className="flex items-center justify-between px-5 py-2.5"
            >
              <div>
                <p className="text-sm font-medium text-gray-900">{s.fullName}</p>
                {s.jobTitle && (
                  <p className="text-xs text-gray-500">{s.jobTitle}</p>
                )}
              </div>
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}
