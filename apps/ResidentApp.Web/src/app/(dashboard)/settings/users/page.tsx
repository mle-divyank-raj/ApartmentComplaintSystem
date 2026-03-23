"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { getAllUsers, deactivateUser, reactivateUser } from "@/lib/api/users";
import { ErrorCodes } from "@acls/error-codes";
import type { UserDto } from "@acls/api-contracts";
import { Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import { ErrorAlert } from "@/components/ui/ErrorAlert";
import axios from "axios";

export default function UsersPage() {
  const [users, setUsers] = useState<UserDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [processing, setProcessing] = useState<number | null>(null);

  function loadUsers() {
    setLoading(true);
    setError(null);
    getAllUsers()
      .then(setUsers)
      .catch((err) => {
        if (axios.isAxiosError(err)) {
          setError(err.response?.data?.detail ?? "Failed to load users.");
        } else {
          setError("Failed to load users.");
        }
      })
      .finally(() => setLoading(false));
  }

  useEffect(() => {
    loadUsers();
  }, []);

  async function handleToggleActive(user: UserDto) {
    setProcessing(user.userId);
    setActionError(null);
    try {
      if (user.isActive) {
        await deactivateUser(user.userId);
      } else {
        await reactivateUser(user.userId);
      }
      loadUsers();
    } catch (err) {
      if (axios.isAxiosError(err)) {
        const errorCode = err.response?.data?.errorCode;
        if (errorCode === ErrorCodes.User.CannotDeactivateSelf) {
          setActionError("You cannot deactivate your own account.");
        } else if (errorCode === ErrorCodes.User.AlreadyDeactivated) {
          setActionError("This user is already deactivated.");
        } else if (errorCode === ErrorCodes.User.AlreadyActive) {
          setActionError("This user is already active.");
        } else {
          setActionError(err.response?.data?.detail ?? "Action failed.");
        }
      } else {
        setActionError("Action failed.");
      }
    } finally {
      setProcessing(null);
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold text-gray-900">Users</h1>
        <Link href="/settings/users/invite">
          <Button>Invite Resident</Button>
        </Link>
      </div>

      {(error ?? actionError) && (
        <ErrorAlert message={(error ?? actionError) as string} />
      )}

      {loading ? (
        <LoadingSpinner />
      ) : (
        <div className="overflow-hidden rounded-lg border border-gray-200 bg-white">
          <table className="min-w-full divide-y divide-gray-200 text-sm">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500">
                  Name
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500">
                  Email
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500">
                  Role
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500">
                  Status
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500">
                  Joined
                </th>
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100 bg-white">
              {users.map((u) => (
                <tr key={u.userId} className="hover:bg-gray-50">
                  <td className="px-4 py-3 font-medium text-gray-900">
                    {u.firstName} {u.lastName}
                  </td>
                  <td className="px-4 py-3 text-gray-700">{u.email}</td>
                  <td className="px-4 py-3">
                    <Badge label={u.role} color="blue" />
                  </td>
                  <td className="px-4 py-3">
                    <Badge
                      label={u.isActive ? "Active" : "Inactive"}
                      color={u.isActive ? "green" : "gray"}
                    />
                  </td>
                  <td className="px-4 py-3 text-gray-500">{u.createdAt}</td>
                  <td className="px-4 py-3 text-right">
                    <Button
                      variant={u.isActive ? "danger" : "secondary"}
                      size="sm"
                      loading={processing === u.userId}
                      onClick={() => handleToggleActive(u)}
                    >
                      {u.isActive ? "Deactivate" : "Reactivate"}
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
