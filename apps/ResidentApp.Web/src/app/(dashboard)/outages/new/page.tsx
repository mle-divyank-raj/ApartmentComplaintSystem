"use client";

import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { declareOutage } from "@/lib/api/outages";
import { OutageType } from "@acls/shared-types";
import { ErrorCodes } from "@acls/error-codes";
import { Button } from "@/components/ui/Button";
import { ErrorAlert } from "@/components/ui/ErrorAlert";
import axios from "axios";

export default function DeclareOutagePage() {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [title, setTitle] = useState("");
  const [outageType, setOutageType] = useState<string>(OutageType.Electricity);
  const [description, setDescription] = useState("");
  const [startTime, setStartTime] = useState("");
  const [endTime, setEndTime] = useState("");

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setLoading(true);
    setError(null);
    try {
      const outage = await declareOutage({
        title,
        outageType,
        description,
        startTime,
        endTime: endTime || undefined,
      });
      router.push(`/outages/${outage.outageId}`);
    } catch (err) {
      if (axios.isAxiosError(err)) {
        const errorCode = err.response?.data?.errorCode;
        if (errorCode === ErrorCodes.Outage.EndTimeBeforeStartTime) {
          setError("End time must be after start time.");
        } else if (errorCode === ErrorCodes.Outage.StartTimeInPast) {
          setError("Start time cannot be in the past.");
        } else if (errorCode === ErrorCodes.Validation.Failed) {
          setError(err.response?.data?.detail ?? "Validation failed.");
        } else {
          setError("Failed to declare outage.");
        }
      } else {
        setError("Failed to declare outage.");
      }
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <button
          onClick={() => router.back()}
          className="text-sm text-gray-500 hover:text-gray-800"
        >
          ← Back
        </button>
        <h1 className="text-2xl font-bold text-gray-900">Declare Outage</h1>
      </div>

      <div className="max-w-lg">
        {error && (
          <div className="mb-4">
            <ErrorAlert message={error} />
          </div>
        )}

        <div className="rounded-lg border border-gray-200 bg-white p-6 shadow-sm">
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label
                htmlFor="outage-title"
                className="block text-sm font-medium text-gray-700"
              >
                Title
              </label>
              <input
                id="outage-title"
                type="text"
                required
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 text-sm shadow-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
              />
            </div>

            <div>
              <label
                htmlFor="outage-type"
                className="block text-sm font-medium text-gray-700"
              >
                Type
              </label>
              <select
                id="outage-type"
                required
                value={outageType}
                onChange={(e) => setOutageType(e.target.value)}
                className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 text-sm shadow-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
              >
                {Object.values(OutageType).map((t) => (
                  <option key={t} value={t}>
                    {t}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label
                htmlFor="outage-description"
                className="block text-sm font-medium text-gray-700"
              >
                Description
              </label>
              <textarea
                id="outage-description"
                required
                rows={3}
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 text-sm shadow-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
              />
            </div>

            <div>
              <label
                htmlFor="outage-start"
                className="block text-sm font-medium text-gray-700"
              >
                Start Time
              </label>
              <input
                id="outage-start"
                type="datetime-local"
                required
                value={startTime}
                onChange={(e) => setStartTime(e.target.value)}
                className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 text-sm shadow-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
              />
            </div>

            <div>
              <label
                htmlFor="outage-end"
                className="block text-sm font-medium text-gray-700"
              >
                End Time{" "}
                <span className="font-normal text-gray-400">(optional)</span>
              </label>
              <input
                id="outage-end"
                type="datetime-local"
                value={endTime}
                onChange={(e) => setEndTime(e.target.value)}
                className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 text-sm shadow-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
              />
            </div>

            <div className="flex gap-3 pt-2">
              <Button type="submit" loading={loading}>
                Declare Outage
              </Button>
              <Button
                type="button"
                variant="secondary"
                disabled={loading}
                onClick={() => router.back()}
              >
                Cancel
              </Button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}
