"use client";

import { useState } from "react";
import { getUnitHistory } from "@/lib/api/reports";
import type { ComplaintDto } from "@acls/api-contracts";
import { Card } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import { ErrorAlert } from "@/components/ui/ErrorAlert";
import { UrgencyBadge } from "@/components/ui/UrgencyBadge";
import { StatusBadge } from "@/components/ui/StatusBadge";
import type { Urgency, TicketStatus } from "@acls/shared-types";
import axios from "axios";

export default function UnitHistoryPage() {
  const [unitId, setUnitId] = useState("");
  const [complaints, setComplaints] = useState<ComplaintDto[] | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSearch() {
    const id = Number(unitId);
    if (!id || id < 1) return;
    setLoading(true);
    setError(null);
    try {
      const data = await getUnitHistory(id);
      setComplaints(data);
    } catch (err) {
      if (axios.isAxiosError(err)) {
        setError(err.response?.data?.detail ?? "Failed to load unit history.");
      } else {
        setError("Failed to load unit history.");
      }
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-semibold text-gray-900">Unit History</h1>

      <div className="flex items-end gap-3">
        <div>
          <label
            htmlFor="unit-id"
            className="block text-sm font-medium text-gray-700"
          >
            Unit ID
          </label>
          <input
            id="unit-id"
            type="number"
            min={1}
            value={unitId}
            onChange={(e) => setUnitId(e.target.value)}
            className="mt-1 block w-40 rounded-md border border-gray-300 px-3 py-2 text-sm shadow-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
          />
        </div>
        <Button onClick={handleSearch} loading={loading}>
          Load History
        </Button>
      </div>

      {error && <ErrorAlert message={error} />}

      {loading && <LoadingSpinner />}

      {!loading && complaints !== null && complaints.length === 0 && (
        <Card>
          <p className="text-center text-sm text-gray-500">
            No complaints found for this unit.
          </p>
        </Card>
      )}

      {!loading && complaints !== null && complaints.length > 0 && (
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
