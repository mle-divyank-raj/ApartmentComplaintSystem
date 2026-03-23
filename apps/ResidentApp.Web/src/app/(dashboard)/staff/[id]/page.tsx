"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { getStaffById } from "@/lib/api/staff";
import type { StaffMemberWithAssignmentsDto } from "@acls/api-contracts";
import { Card } from "@/components/ui/Card";
import { Badge } from "@/components/ui/Badge";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import { ErrorAlert } from "@/components/ui/ErrorAlert";
import { ComplaintTable } from "@/components/complaints/ComplaintTable";
import { StaffState } from "@acls/shared-types";
import axios from "axios";

const availabilityColor: Record<string, "green" | "orange" | "yellow" | "gray"> = {
  [StaffState.AVAILABLE]: "green",
  [StaffState.BUSY]: "orange",
  [StaffState.ON_BREAK]: "yellow",
  [StaffState.OFF_DUTY]: "gray",
};

export default function StaffDetailPage() {
  const params = useParams();
  const router = useRouter();
  const staffMemberId = Number(params.id);

  const [staff, setStaff] = useState<StaffMemberWithAssignmentsDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getStaffById(staffMemberId)
      .then(setStaff)
      .catch((err) => {
        if (axios.isAxiosError(err)) {
          setError(err.response?.data?.detail ?? "Failed to load staff member.");
        } else {
          setError("Failed to load staff member.");
        }
      })
      .finally(() => setLoading(false));
  }, [staffMemberId]);

  if (loading) return <LoadingSpinner />;
  if (error) return <ErrorAlert message={error} />;
  if (!staff) return null;

  const color = availabilityColor[staff.availability] ?? "gray";

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <button
          onClick={() => router.back()}
          className="text-sm text-gray-500 hover:text-gray-800"
        >
          ← Back
        </button>
        <h1 className="text-2xl font-bold text-gray-900">{staff.fullName}</h1>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        <Card className="lg:col-span-1">
          <div className="space-y-3 text-sm">
            {staff.jobTitle && (
              <div>
                <span className="font-medium text-gray-600">Job Title:</span>{" "}
                <span className="text-gray-800">{staff.jobTitle}</span>
              </div>
            )}
            <div className="flex items-center gap-2">
              <span className="font-medium text-gray-600">Availability:</span>
              <Badge label={staff.availability.replace(/_/g, " ")} color={color} />
            </div>
            {staff.averageRating != null && (
              <div>
                <span className="font-medium text-gray-600">Avg. Rating:</span>{" "}
                <span className="text-gray-800">
                  {staff.averageRating.toFixed(1)} / 5
                </span>
              </div>
            )}
            {staff.lastAssignedAt && (
              <div>
                <span className="font-medium text-gray-600">Last Assigned:</span>{" "}
                <span className="text-gray-800">{staff.lastAssignedAt}</span>
              </div>
            )}
            {staff.skills.length > 0 && (
              <div>
                <span className="font-medium text-gray-600">Skills:</span>
                <div className="mt-1.5 flex flex-wrap gap-1.5">
                  {staff.skills.map((skill) => (
                    <Badge key={skill} label={skill} color="blue" />
                  ))}
                </div>
              </div>
            )}
          </div>
        </Card>

        <div className="lg:col-span-2 space-y-4">
          <h2 className="text-base font-semibold text-gray-900">
            Active Assignments ({staff.activeAssignments.length})
          </h2>
          <div className="overflow-hidden rounded-lg border border-gray-200 bg-white">
            <ComplaintTable complaints={staff.activeAssignments} />
          </div>
        </div>
      </div>
    </div>
  );
}
