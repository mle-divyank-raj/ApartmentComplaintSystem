"use client";

import { useEffect, useState } from "react";
import type { StaffScoreDto } from "@acls/api-contracts";
import { ErrorCodes } from "@acls/error-codes";
import { Button } from "@/components/ui/Button";
import { ErrorAlert } from "@/components/ui/ErrorAlert";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import { getDispatchRecommendations, assignComplaint, reassignComplaint } from "@/lib/api";
import axios from "axios";

interface AssignDialogProps {
  complaintId: number;
  isReassign?: boolean;
  onAssigned: () => void;
  onCancel: () => void;
}

const ORDINALS = ["1st", "2nd", "3rd", "4th", "5th"];

function getOrdinal(index: number): string {
  return ORDINALS[index] ?? `${index + 1}th`;
}

function getApiErrorCode(err: unknown): string | null {
  if (axios.isAxiosError(err)) {
    return err.response?.data?.errorCode ?? null;
  }
  return null;
}

function formatAssignError(errorCode: string | null): string {
  if (errorCode === ErrorCodes.Complaint.StaffNotAvailable) {
    return "This staff member is no longer available.";
  }
  if (errorCode === ErrorCodes.Complaint.InvalidStatusTransition) {
    return "This complaint cannot be assigned in its current state.";
  }
  return "Failed to assign complaint.";
}

export function AssignDialog({
  complaintId,
  isReassign = false,
  onAssigned,
  onCancel,
}: AssignDialogProps) {
  const [recommendations, setRecommendations] = useState<StaffScoreDto[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [assigning, setAssigning] = useState(false);
  const [error, setError] = useState<string | null>(null);
  // Confirmation step: holds the staff member pending confirmation
  const [pendingStaff, setPendingStaff] = useState<StaffScoreDto | null>(null);

  // Auto-load recommendations on mount (manager_assign_flow §3)
  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);
    getDispatchRecommendations(complaintId)
      .then((data) => {
        if (!cancelled) setRecommendations(data.recommendations);
      })
      .catch((err) => {
        if (!cancelled) {
          const code = getApiErrorCode(err);
          if (code === ErrorCodes.Dispatch.NoStaffAvailable) {
            setRecommendations([]);
          } else {
            setError("Failed to load recommendations.");
          }
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => { cancelled = true; };
  }, [complaintId]);

  async function handleConfirmAssign() {
    if (!pendingStaff) return;
    setAssigning(true);
    setError(null);
    try {
      if (isReassign) {
        await reassignComplaint(complaintId, { staffMemberId: pendingStaff.staffMemberId });
      } else {
        await assignComplaint(complaintId, { staffMemberId: pendingStaff.staffMemberId });
      }
      onAssigned();
    } catch (err) {
      const code = getApiErrorCode(err);
      setError(formatAssignError(code));
      setPendingStaff(null);
    } finally {
      setAssigning(false);
    }
  }

  // Confirmation dialog
  if (pendingStaff) {
    return (
      <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
        <div className="w-full max-w-sm rounded-lg bg-white shadow-xl">
          <div className="px-5 py-6 text-center">
            <p className="text-base font-semibold text-gray-900">
              Assign {pendingStaff.fullName} to this complaint?
            </p>
            <p className="mt-1 text-sm text-gray-500">
              {pendingStaff.jobTitle ?? "Staff"} &middot; {pendingStaff.availability}
            </p>
          </div>
          {error && (
            <div className="px-5 pb-3">
              <ErrorAlert message={error} />
            </div>
          )}
          <div className="flex justify-end gap-2 border-t border-gray-200 px-5 py-3">
            <Button variant="secondary" onClick={() => setPendingStaff(null)} disabled={assigning}>
              Cancel
            </Button>
            <Button loading={assigning} onClick={handleConfirmAssign}>
              Confirm
            </Button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
      <div className="w-full max-w-lg rounded-lg bg-white shadow-xl">
        <div className="flex items-center justify-between border-b border-gray-200 px-5 py-4">
          <h2 className="text-base font-semibold text-gray-900">
            {isReassign ? "Reassign Complaint" : "Assign Complaint"} #{complaintId}
          </h2>
          <button
            onClick={onCancel}
            className="text-gray-400 hover:text-gray-600"
            aria-label="Close"
          >
            ✕
          </button>
        </div>

        <div className="p-5">
          {error && <ErrorAlert message={error} />}

          {loading && <LoadingSpinner label="Loading recommendations..." />}

          {!loading && recommendations !== null && recommendations.length === 0 && (
            <p className="text-center text-sm text-gray-500">
              No available staff found for this complaint.
            </p>
          )}

          {!loading && recommendations !== null && recommendations.length > 0 && (
            <ul className="divide-y divide-gray-100">
              {recommendations.map((rec, index) => (
                <li
                  key={rec.staffMemberId}
                  className="flex items-center justify-between py-3"
                >
                  <div>
                    <div className="flex items-center gap-2">
                      <span className="text-xs font-semibold text-primary">
                        {getOrdinal(index)}
                      </span>
                      <p className="text-sm font-medium text-gray-900">
                        {rec.fullName}
                      </p>
                    </div>
                    <p className="mt-0.5 text-xs text-gray-500">
                      {rec.jobTitle ?? "Staff"} &middot; {rec.availability} &middot; Skills:{" "}
                      {rec.skills.join(", ") || "—"}
                    </p>
                  </div>
                  <Button
                    size="sm"
                    onClick={() => setPendingStaff(rec)}
                  >
                    Assign
                  </Button>
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="flex justify-end gap-2 border-t border-gray-200 px-5 py-3">
          <Button variant="secondary" onClick={onCancel}>
            Cancel
          </Button>
        </div>
      </div>
    </div>
  );
}

