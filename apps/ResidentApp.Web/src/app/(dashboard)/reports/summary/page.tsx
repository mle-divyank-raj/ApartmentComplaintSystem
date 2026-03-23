"use client";

import { useEffect, useState } from "react";
import { getComplaintsSummary } from "@/lib/api/reports";
import type { ComplaintDto } from "@acls/api-contracts";
import { Card } from "@/components/ui/Card";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import { ErrorAlert } from "@/components/ui/ErrorAlert";
import { UrgencyBadge } from "@/components/ui/UrgencyBadge";
import { StatusBadge } from "@/components/ui/StatusBadge";
import type { Urgency, TicketStatus } from "@acls/shared-types";
import axios from "axios";

export default function ComplaintSummaryPage() {
  const [complaints, setComplaints] = useState<ComplaintDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getComplaintsSummary()
      .then(setComplaints)
      .catch((err) => {
        if (axios.isAxiosError(err)) {
          setError(
            err.response?.data?.detail ?? "Failed to load complaints summary."
          );
        } else {
          setError("Failed to load complaints summary.");
        }
      })
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-semibold text-gray-900">
        Complaints Summary
      </h1>

      {error && <ErrorAlert message={error} />}

      {loading ? (
        <LoadingSpinner />
      ) : complaints.length === 0 ? (
        <Card>
          <p className="text-center text-sm text-gray-500">
            No complaints to display.
          </p>
        </Card>
      ) : (
        <div className="overflow-hidden rounded-lg border border-gray-200 bg-white">
          <table className="min-w-full divide-y divide-gray-200 text-sm">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500">
                  ID
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500">
                  Title
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500">
                  Urgency
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500">
                  Status
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500">
                  Created
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100 bg-white">
              {complaints.map((c) => (
                <tr key={c.complaintId} className="hover:bg-gray-50">
                  <td className="px-4 py-3 text-xs font-mono text-gray-600">
                    #{c.complaintId}
                  </td>
                  <td className="px-4 py-3 font-medium text-gray-900">
                    {c.title}
                  </td>
                  <td className="px-4 py-3">
                    <UrgencyBadge urgency={c.urgency as Urgency} />
                  </td>
                  <td className="px-4 py-3">
                    <StatusBadge status={c.status as TicketStatus} />
                  </td>
                  <td className="px-4 py-3 text-gray-500">{c.createdAt}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
