"use client";

import { useEffect, useState } from "react";
import { getAllOutages } from "@/lib/api/outages";
import type { OutageDto } from "@acls/api-contracts";
import { Card } from "@/components/ui/Card";
import { Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import { ErrorAlert } from "@/components/ui/ErrorAlert";
import Link from "next/link";
import axios from "axios";

export default function OutagesPage() {
  const [outages, setOutages] = useState<OutageDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getAllOutages()
      .then(setOutages)
      .catch((err) => {
        if (axios.isAxiosError(err)) {
          setError(err.response?.data?.detail ?? "Failed to load outages.");
        } else {
          setError("Failed to load outages.");
        }
      })
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Outages</h1>
        <Link href="/outages/new">
          <Button>Declare Outage</Button>
        </Link>
      </div>

      {error && <ErrorAlert message={error} />}

      {loading ? (
        <LoadingSpinner />
      ) : outages.length === 0 ? (
        <p className="py-8 text-center text-sm text-gray-500">
          No outages declared.
        </p>
      ) : (
        <div className="space-y-3">
          {outages.map((o) => (
            <Card key={o.outageId}>
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div>
                  <h2 className="text-sm font-semibold text-gray-900">
                    {o.title}
                  </h2>
                  <p className="mt-0.5 text-xs text-gray-500">
                    {o.description}
                  </p>
                </div>
                <Badge label={o.outageType} color="orange" />
              </div>
              <div className="mt-3 flex flex-wrap gap-4 text-xs text-gray-500">
                <span>Start: {o.startTime}</span>
                {o.endTime && <span>End: {o.endTime}</span>}
                <span>Declared: {o.declaredAt}</span>
                {o.notificationSentAt && (
                  <span>Notified: {o.notificationSentAt}</span>
                )}
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
