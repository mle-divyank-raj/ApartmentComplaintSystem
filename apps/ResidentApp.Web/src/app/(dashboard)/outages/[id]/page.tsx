"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { getOutageById } from "@/lib/api/outages";
import type { OutageDto } from "@acls/api-contracts";
import { Card } from "@/components/ui/Card";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import { ErrorAlert } from "@/components/ui/ErrorAlert";
import axios from "axios";

export default function OutageDetailPage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();
  const outageId = Number(params.id);

  const [outage, setOutage] = useState<OutageDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!outageId || outageId < 1) {
      setError("Invalid outage ID.");
      setLoading(false);
      return;
    }
    getOutageById(outageId)
      .then(setOutage)
      .catch((err) => {
        if (axios.isAxiosError(err)) {
          setError(err.response?.data?.detail ?? "Failed to load outage.");
        } else {
          setError("Failed to load outage.");
        }
      })
      .finally(() => setLoading(false));
  }, [outageId]);

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <button
          onClick={() => router.push("/outages")}
          className="text-sm text-gray-500 hover:text-gray-800"
        >
          &larr; Back to Outages
        </button>
        <h1 className="text-2xl font-semibold text-gray-900">
          Outage Details
        </h1>
      </div>

      {error && <ErrorAlert message={error} />}

      {loading && <LoadingSpinner />}

      {!loading && outage && (
        <>
          {/* Notification status banner (manager_assign_flow §4) */}
          {outage.notificationSentAt === null ? (
            <div className="rounded-md border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
              Notifications are being sent to all residents...
            </div>
          ) : (
            <div className="rounded-md border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-700">
              Notifications sent at {outage.notificationSentAt}
            </div>
          )}

          <Card>
            <dl className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div>
                <dt className="text-sm font-medium text-gray-500">Title</dt>
                <dd className="mt-1 text-sm text-gray-900">{outage.title}</dd>
              </div>
              <div>
                <dt className="text-sm font-medium text-gray-500">Type</dt>
                <dd className="mt-1 text-sm text-gray-900">
                  {outage.outageType}
                </dd>
              </div>
              <div className="sm:col-span-2">
                <dt className="text-sm font-medium text-gray-500">
                  Description
                </dt>
                <dd className="mt-1 text-sm text-gray-900">
                  {outage.description}
                </dd>
              </div>
              <div>
                <dt className="text-sm font-medium text-gray-500">
                  Start Time
                </dt>
                <dd className="mt-1 text-sm text-gray-900">
                  {outage.startTime}
                </dd>
              </div>
              <div>
                <dt className="text-sm font-medium text-gray-500">End Time</dt>
                <dd className="mt-1 text-sm text-gray-900">
                  {outage.endTime ?? (
                    <span className="italic text-gray-400">Not set</span>
                  )}
                </dd>
              </div>
              <div>
                <dt className="text-sm font-medium text-gray-500">
                  Declared At
                </dt>
                <dd className="mt-1 text-sm text-gray-900">
                  {outage.declaredAt}
                </dd>
              </div>
            </dl>
          </Card>
        </>
      )}
    </div>
  );
}
