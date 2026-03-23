import type { ComplaintDto } from "@acls/api-contracts";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { UrgencyBadge } from "@/components/ui/UrgencyBadge";
import Link from "next/link";

interface ComplaintTableProps {
  complaints: ComplaintDto[];
}

export function ComplaintTable({ complaints }: ComplaintTableProps) {
  if (complaints.length === 0) {
    return (
      <p className="py-8 text-center text-sm text-gray-500">
        No complaints found.
      </p>
    );
  }

  return (
    <div className="overflow-x-auto">
      <table className="min-w-full divide-y divide-gray-200 text-sm">
        <thead className="bg-gray-50">
          <tr>
            <th className="px-4 py-3 text-left font-medium text-gray-500 uppercase tracking-wider text-xs">
              #
            </th>
            <th className="px-4 py-3 text-left font-medium text-gray-500 uppercase tracking-wider text-xs">
              Title
            </th>
            <th className="px-4 py-3 text-left font-medium text-gray-500 uppercase tracking-wider text-xs">
              Unit
            </th>
            <th className="px-4 py-3 text-left font-medium text-gray-500 uppercase tracking-wider text-xs">
              Category
            </th>
            <th className="px-4 py-3 text-left font-medium text-gray-500 uppercase tracking-wider text-xs">
              Urgency
            </th>
            <th className="px-4 py-3 text-left font-medium text-gray-500 uppercase tracking-wider text-xs">
              Status
            </th>
            <th className="px-4 py-3 text-left font-medium text-gray-500 uppercase tracking-wider text-xs">
              Assigned To
            </th>
            <th className="px-4 py-3 text-left font-medium text-gray-500 uppercase tracking-wider text-xs">
              Submitted
            </th>
          </tr>
        </thead>
        <tbody className="divide-y divide-gray-100 bg-white">
          {complaints.map((c) => (
            <tr key={c.complaintId} className="hover:bg-gray-50">
              <td className="px-4 py-3 text-gray-500">#{c.complaintId}</td>
              <td className="px-4 py-3">
                <Link
                  href={`/complaints/${c.complaintId}`}
                  className="font-medium text-primary hover:underline"
                >
                  {c.title}
                </Link>
                <p className="mt-0.5 text-xs text-gray-500">{c.residentName}</p>
              </td>
              <td className="px-4 py-3 text-gray-700">
                {c.unitNumber}
                <span className="ml-1 text-xs text-gray-400">
                  {c.buildingName}
                </span>
              </td>
              <td className="px-4 py-3 text-gray-700">{c.category}</td>
              <td className="px-4 py-3">
                <UrgencyBadge urgency={c.urgency} />
              </td>
              <td className="px-4 py-3">
                <StatusBadge status={c.status} />
              </td>
              <td className="px-4 py-3 text-gray-700">
                {c.assignedStaffMember?.fullName ?? (
                  <span className="text-gray-400 italic">Unassigned</span>
                )}
              </td>
              <td className="px-4 py-3 text-gray-500">{c.createdAt}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
