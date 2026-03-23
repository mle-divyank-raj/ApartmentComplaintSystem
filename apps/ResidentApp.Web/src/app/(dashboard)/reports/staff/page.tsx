"use client";

import { useEffect, useState } from "react";
import { getStaffPerformance } from "@/lib/api/reports";
import type { StaffPerformanceSummaryDto } from "@acls/api-contracts";
import { Card } from "@/components/ui/Card";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import { ErrorAlert } from "@/components/ui/ErrorAlert";
import axios from "axios";

export default function StaffPerformancePage() {
  const [performance, setPerformance] = useState<StaffPerformanceSummaryDto[]>(
    []
  );
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getStaffPerformance()
      .then(setPerformance)
      .catch((err) => {
        if (axios.isAxiosError(err)) {
          setError(err.response?.data?.detail ?? "Failed to load staff performance.");
        } else {
          setError("Failed to load staff performance.");
        }
      })
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-semibold text-gray-900">
        Staff Performance
      </h1>

      {error && <ErrorAlert message={error} />}

      {loading ? (
        <LoadingSpinner />
      ) : performance.length === 0 ? (
        <Card>
          <p className="text-center text-sm text-gray-500">
            No performance data available.
          </p>
        </Card>
      ) : (
        <div className="overflow-hidden rounded-lg border border-gray-200 bg-white">
          <table className="min-w-full divide-y divide-gray-200 text-sm">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500">
                  Staff Member
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500">
                  Job Title
                </th>
                <th className="px-4 py-3 text-right text-xs font-medium uppercase tracking-wider text-gray-500">
                  Total Resolved
                </th>
                <th className="px-4 py-3 text-right text-xs font-medium uppercase tracking-wider text-gray-500">
                  Avg. Rating
                </th>
                <th className="px-4 py-3 text-right text-xs font-medium uppercase tracking-wider text-gray-500">
                  Avg. TAT (min)
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100 bg-white">
              {performance.map((s) => (
                <tr key={s.staffMemberId} className="hover:bg-gray-50">
                  <td className="px-4 py-3 font-medium text-gray-900">
                    {s.fullName}
                  </td>
                  <td className="px-4 py-3 text-gray-700">
                    {s.jobTitle ?? (
                      <span className="italic text-gray-400">&mdash;</span>
                    )}
                  </td>
                  <td className="px-4 py-3 text-right text-gray-700">
                    {s.totalResolved}
                  </td>
                  <td className="px-4 py-3 text-right text-gray-700">
                    {s.averageRating != null ? (
                      s.averageRating.toFixed(1)
                    ) : (
                      <span className="italic text-gray-400">&mdash;</span>
                    )}
                  </td>
                  <td className="px-4 py-3 text-right text-gray-700">
                    {s.averageTatMinutes != null ? (
                      s.averageTatMinutes.toFixed(0)
                    ) : (
                      <span className="italic text-gray-400">&mdash;</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
