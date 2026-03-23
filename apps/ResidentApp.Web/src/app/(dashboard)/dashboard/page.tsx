"use client";

import { useEffect, useState } from "react";
import { getDashboardMetrics } from "@/lib/api/reports";
import type { DashboardMetricsDto } from "@acls/api-contracts";
import { MetricsCard } from "@/components/dashboard/MetricsCard";
import { ActiveAssignments } from "@/components/dashboard/ActiveAssignments";
import { StaffAvailabilitySummary } from "@/components/dashboard/StaffAvailabilitySummary";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import { ErrorAlert } from "@/components/ui/ErrorAlert";
import axios from "axios";

export default function DashboardPage() {
  const [metrics, setMetrics] = useState<DashboardMetricsDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getDashboardMetrics()
      .then(setMetrics)
      .catch((err) => {
        if (axios.isAxiosError(err)) {
          setError(err.response?.data?.detail ?? "Failed to load dashboard.");
        } else {
          setError("Failed to load dashboard.");
        }
      })
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <LoadingSpinner label="Loading dashboard..." />;
  if (error) return <ErrorAlert message={error} />;
  if (!metrics) return null;

  return (
    <div className="space-y-8">
      <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>

      <section>
        <h2 className="mb-4 text-sm font-semibold uppercase tracking-wide text-gray-500">
          Ticket Summary
        </h2>
        <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-6">
          <MetricsCard label="Open" value={metrics.openCount} accent="warning" />
          <MetricsCard label="Assigned" value={metrics.assignedCount} />
          <MetricsCard label="In Progress" value={metrics.inProgressCount} accent="warning" />
          <MetricsCard label="Resolved" value={metrics.resolvedCount} accent="success" />
          <MetricsCard label="Closed" value={metrics.closedCount} />
          <MetricsCard
            label="SOS Active"
            value={metrics.sosActiveCount}
            accent={metrics.sosActiveCount > 0 ? "danger" : "default"}
          />
        </div>
      </section>

      <div className="grid gap-6 lg:grid-cols-2">
        <ActiveAssignments assignments={metrics.activeAssignments} />
        <StaffAvailabilitySummary staff={metrics.staffAvailabilitySummary} />
      </div>
    </div>
  );
}
