"use client";

import { useEffect, useState } from "react";
import { getAllStaff } from "@/lib/api/staff";
import type { StaffMemberDto } from "@acls/api-contracts";
import { StaffTable } from "@/components/staff/StaffTable";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import { ErrorAlert } from "@/components/ui/ErrorAlert";
import axios from "axios";

export default function StaffPage() {
  const [staffList, setStaffList] = useState<StaffMemberDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getAllStaff()
      .then(setStaffList)
      .catch((err) => {
        if (axios.isAxiosError(err)) {
          setError(err.response?.data?.detail ?? "Failed to load staff.");
        } else {
          setError("Failed to load staff.");
        }
      })
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-gray-900">Staff</h1>

      {error && <ErrorAlert message={error} />}

      {loading ? (
        <LoadingSpinner />
      ) : (
        <div className="overflow-hidden rounded-lg border border-gray-200 bg-white">
          <StaffTable staffList={staffList} />
        </div>
      )}
    </div>
  );
}
