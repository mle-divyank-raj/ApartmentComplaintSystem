"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { getComplaintById } from "@/lib/api/complaints";
import type { ComplaintDto } from "@acls/api-contracts";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { UrgencyBadge } from "@/components/ui/UrgencyBadge";
import { Card } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import { ErrorAlert } from "@/components/ui/ErrorAlert";
import { AssignDialog } from "@/components/complaints/AssignDialog";
import { TicketStatus } from "@acls/shared-types";
import axios from "axios";

export default function ComplaintDetailPage() {
  const params = useParams();
  const router = useRouter();
  const complaintId = Number(params.id);

  const [complaint, setComplaint] = useState<ComplaintDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showAssignDialog, setShowAssignDialog] = useState(false);
  const [isReassign, setIsReassign] = useState(false);

  function loadComplaint() {
    setLoading(true);
    setError(null);
    getComplaintById(complaintId)
      .then(setComplaint)
      .catch((err) => {
        if (axios.isAxiosError(err)) {
          setError(err.response?.data?.detail ?? "Failed to load complaint.");
        } else {
          setError("Failed to load complaint.");
        }
      })
      .finally(() => setLoading(false));
  }

  useEffect(() => {
    loadComplaint();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [complaintId]);

  if (loading) return <LoadingSpinner />;
  if (error) return <ErrorAlert message={error} />;
  if (!complaint) return null;

  const isAssigned = complaint.status !== TicketStatus.OPEN;
  const canAssign = complaint.status === TicketStatus.OPEN;
  const canReassign =
    complaint.status === TicketStatus.ASSIGNED ||
    complaint.status === TicketStatus.EN_ROUTE;

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <button
          onClick={() => router.back()}
          className="text-sm text-gray-500 hover:text-gray-800"
        >
          ← Back
        </button>
        <h1 className="text-2xl font-bold text-gray-900">
          Complaint #{complaint.complaintId}
        </h1>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Main details */}
        <div className="space-y-4 lg:col-span-2">
          <Card>
            <div className="mb-4 flex flex-wrap items-start justify-between gap-3">
              <div>
                <h2 className="text-lg font-semibold text-gray-900">
                  {complaint.title}
                </h2>
                <p className="mt-0.5 text-sm text-gray-500">
                  {complaint.category} · {complaint.residentName}
                </p>
              </div>
              <div className="flex items-center gap-2">
                <UrgencyBadge urgency={complaint.urgency} />
                <StatusBadge status={complaint.status} />
              </div>
            </div>
            <p className="text-sm text-gray-700">{complaint.description}</p>

            <div className="mt-4 grid grid-cols-2 gap-4 text-sm">
              <div>
                <span className="font-medium text-gray-600">Unit:</span>{" "}
                <span className="text-gray-800">
                  {complaint.unitNumber} · {complaint.buildingName}
                </span>
              </div>
              <div>
                <span className="font-medium text-gray-600">
                  Permission to enter:
                </span>{" "}
                <span className="text-gray-800">
                  {complaint.permissionToEnter ? "Yes" : "No"}
                </span>
              </div>
              <div>
                <span className="font-medium text-gray-600">Submitted:</span>{" "}
                <span className="text-gray-800">{complaint.createdAt}</span>
              </div>
              {complaint.eta && (
                <div>
                  <span className="font-medium text-gray-600">ETA:</span>{" "}
                  <span className="text-gray-800">{complaint.eta}</span>
                </div>
              )}
              {complaint.resolvedAt && (
                <div>
                  <span className="font-medium text-gray-600">Resolved at:</span>{" "}
                  <span className="text-gray-800">{complaint.resolvedAt}</span>
                </div>
              )}
              {complaint.tat != null && (
                <div>
                  <span className="font-medium text-gray-600">TAT (min):</span>{" "}
                  <span className="text-gray-800">{complaint.tat}</span>
                </div>
              )}
            </div>
          </Card>

          {/* Work Notes */}
          {complaint.workNotes.length > 0 && (
            <Card padding="none">
              <div className="border-b border-gray-200 px-5 py-3">
                <h3 className="text-sm font-semibold text-gray-900">
                  Work Notes ({complaint.workNotes.length})
                </h3>
              </div>
              <ul className="divide-y divide-gray-100">
                {complaint.workNotes.map((note) => (
                  <li key={note.workNoteId} className="px-5 py-3">
                    <p className="text-sm text-gray-700">{note.note}</p>
                    <p className="mt-1 text-xs text-gray-400">
                      {note.createdAt}
                    </p>
                  </li>
                ))}
              </ul>
            </Card>
          )}

          {/* Media attachments */}
          {complaint.media.length > 0 && (
            <Card>
              <h3 className="mb-3 text-sm font-semibold text-gray-900">
                Attachments ({complaint.media.length})
              </h3>
              <div className="flex flex-wrap gap-3">
                {complaint.media.map((m) => (
                  <a
                    key={m.mediaId}
                    href={m.url}
                    target="_blank"
                    rel="noreferrer noopener"
                    className="rounded border border-gray-200 px-3 py-1.5 text-xs text-primary hover:bg-gray-50 hover:underline"
                  >
                    {m.type}
                  </a>
                ))}
              </div>
            </Card>
          )}

          {/* Resident Feedback */}
          {complaint.residentRating != null && (
            <Card>
              <h3 className="mb-2 text-sm font-semibold text-gray-900">
                Resident Feedback
              </h3>
              <p className="text-sm text-gray-700">
                Rating: {complaint.residentRating} / 5
              </p>
              {complaint.residentFeedbackComment && (
                <p className="mt-1 text-sm text-gray-600 italic">
                  "{complaint.residentFeedbackComment}"
                </p>
              )}
            </Card>
          )}
        </div>

        {/* Sidebar */}
        <div className="space-y-4">
          <Card>
            <h3 className="mb-3 text-sm font-semibold text-gray-900">
              Assignment
            </h3>
            {isAssigned && complaint.assignedStaffMember ? (
              <p className="text-sm text-gray-700">
                {complaint.assignedStaffMember.fullName}
                {complaint.assignedStaffMember.jobTitle && (
                  <span className="ml-1 text-xs text-gray-500">
                    ({complaint.assignedStaffMember.jobTitle})
                  </span>
                )}
              </p>
            ) : (
              <p className="text-sm text-gray-400 italic">Unassigned</p>
            )}

            <div className="mt-4 flex flex-col gap-2">
              {canAssign && (
                <Button
                  size="sm"
                  onClick={() => {
                    setIsReassign(false);
                    setShowAssignDialog(true);
                  }}
                >
                  Assign
                </Button>
              )}
              {canReassign && (
                <Button
                  size="sm"
                  variant="secondary"
                  onClick={() => {
                    setIsReassign(true);
                    setShowAssignDialog(true);
                  }}
                >
                  Reassign
                </Button>
              )}
            </div>
          </Card>
        </div>
      </div>

      {showAssignDialog && (
        <AssignDialog
          complaintId={complaint.complaintId}
          isReassign={isReassign}
          onAssigned={() => {
            setShowAssignDialog(false);
            loadComplaint();
          }}
          onCancel={() => setShowAssignDialog(false)}
        />
      )}
    </div>
  );
}
