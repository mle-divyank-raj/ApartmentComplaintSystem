import type { StaffMemberDto } from "@acls/api-contracts";
import { Badge } from "@/components/ui/Badge";
import { StaffState } from "@acls/shared-types";
import Link from "next/link";

interface StaffTableProps {
  staffList: StaffMemberDto[];
}

const availabilityConfig: Record<string, { label: string; color: "gray" | "blue" | "green" | "yellow" | "orange" | "red" | "purple" }> = {
  [StaffState.AVAILABLE]: { label: "Available", color: "green" },
  [StaffState.BUSY]: { label: "Busy", color: "orange" },
  [StaffState.ON_BREAK]: { label: "On Break", color: "yellow" },
  [StaffState.OFF_DUTY]: { label: "Off Duty", color: "gray" },
};

export function StaffTable({ staffList }: StaffTableProps) {
  if (staffList.length === 0) {
    return (
      <p className="py-8 text-center text-sm text-gray-500">
        No staff members found.
      </p>
    );
  }

  return (
    <div className="overflow-x-auto">
      <table className="min-w-full divide-y divide-gray-200 text-sm">
        <thead className="bg-gray-50">
          <tr>
            <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500">
              Name
            </th>
            <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500">
              Job Title
            </th>
            <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500">
              Skills
            </th>
            <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500">
              Availability
            </th>
            <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500">
              Avg. Rating
            </th>
            <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500">
              Last Assigned
            </th>
          </tr>
        </thead>
        <tbody className="divide-y divide-gray-100 bg-white">
          {staffList.map((s) => {
            const avail =
              availabilityConfig[s.availability] ?? { label: s.availability, color: "gray" as const };
            return (
              <tr key={s.staffMemberId} className="hover:bg-gray-50">
                <td className="px-4 py-3">
                  <Link
                    href={`/staff/${s.staffMemberId}`}
                    className="font-medium text-primary hover:underline"
                  >
                    {s.fullName}
                  </Link>
                </td>
                <td className="px-4 py-3 text-gray-700">
                  {s.jobTitle ?? <span className="italic text-gray-400">—</span>}
                </td>
                <td className="px-4 py-3">
                  <div className="flex flex-wrap gap-1">
                    {s.skills.length > 0
                      ? s.skills.map((skill) => (
                          <Badge key={skill} label={skill} color="blue" />
                        ))
                      : <span className="text-gray-400 italic text-xs">None</span>}
                  </div>
                </td>
                <td className="px-4 py-3">
                  <Badge label={avail.label} color={avail.color} />
                </td>
                <td className="px-4 py-3 text-gray-700">
                  {s.averageRating != null
                    ? s.averageRating.toFixed(1)
                    : <span className="italic text-gray-400">—</span>}
                </td>
                <td className="px-4 py-3 text-gray-500">
                  {s.lastAssignedAt ?? <span className="italic text-gray-400">Never</span>}
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
