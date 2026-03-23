"use client";

import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { inviteResident } from "@/lib/api/users";
import { ErrorCodes } from "@acls/error-codes";
import { Button } from "@/components/ui/Button";
import { ErrorAlert } from "@/components/ui/ErrorAlert";
import axios from "axios";

export default function InviteResidentPage() {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [unitId, setUnitId] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setLoading(true);
    setError(null);
    setSuccess(null);
    try {
      const inv = await inviteResident({
        email,
        unitId: Number(unitId),
      });
      setSuccess(
        `Invitation sent to ${inv.email}. Link expires in 72 hours.`
      );
      setEmail("");
      setUnitId("");
    } catch (err) {
      if (axios.isAxiosError(err)) {
        const errorCode = err.response?.data?.errorCode;
        if (errorCode === ErrorCodes.Auth.EmailAlreadyRegistered) {
          setError("An account with this email already exists.");
        } else if (errorCode === ErrorCodes.Validation.Failed) {
          setError(err.response?.data?.detail ?? "Validation failed.");
        } else {
          setError("Failed to send invitation.");
        }
      } else {
        setError("Failed to send invitation.");
      }
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <button
          onClick={() => router.push("/settings/users")}
          className="text-sm text-gray-500 hover:text-gray-800"
        >
          &larr; Back to Users
        </button>
        <h1 className="text-2xl font-semibold text-gray-900">
          Invite Resident
        </h1>
      </div>

      {success && (
        <div className="rounded-md border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-700">
          {success}
        </div>
      )}

      {error && <ErrorAlert message={error} />}

      <div className="max-w-lg">
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label
              htmlFor="invite-email"
              className="block text-sm font-medium text-gray-700"
            >
              Email Address
            </label>
            <input
              id="invite-email"
              type="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 text-sm shadow-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
            />
          </div>

          <div>
            <label
              htmlFor="invite-unit"
              className="block text-sm font-medium text-gray-700"
            >
              Unit ID
            </label>
            <input
              id="invite-unit"
              type="number"
              required
              min={1}
              value={unitId}
              onChange={(e) => setUnitId(e.target.value)}
              className="mt-1 block w-40 rounded-md border border-gray-300 px-3 py-2 text-sm shadow-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
            />
          </div>

          <Button type="submit" loading={loading}>
            Send Invitation
          </Button>
        </form>
      </div>
    </div>
  );
}
