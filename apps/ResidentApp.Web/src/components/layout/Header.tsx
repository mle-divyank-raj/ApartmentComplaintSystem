"use client";

import { Bell, LogOut } from "lucide-react";
import { signOut, getSession } from "@/lib/auth";

export function Header() {
  const session = getSession();

  function handleSignOut() {
    signOut();
    window.location.href = "/login";
  }

  return (
    <header className="flex h-16 shrink-0 items-center justify-between border-b border-gray-200 bg-white px-6">
      <div className="flex items-center gap-2">
        <span className="text-lg font-bold text-primary">ACLS</span>
        <span className="text-sm text-muted">Manager Portal</span>
      </div>

      <div className="flex items-center gap-3">
        <button
          className="rounded-md p-2 text-gray-500 hover:bg-gray-100 hover:text-gray-900"
          aria-label="Notifications"
        >
          <Bell className="h-5 w-5" />
        </button>

        <div className="flex items-center gap-2">
          <span className="text-sm text-gray-700">
            {session?.role ?? "Manager"}
          </span>
          <button
            onClick={handleSignOut}
            className="rounded-md p-2 text-gray-500 hover:bg-gray-100 hover:text-gray-900"
            aria-label="Sign out"
          >
            <LogOut className="h-5 w-5" />
          </button>
        </div>
      </div>
    </header>
  );
}
