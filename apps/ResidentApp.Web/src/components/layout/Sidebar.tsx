"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useState } from "react";
import {
  AlertTriangle,
  BarChart2,
  ChevronDown,
  ChevronRight,
  LayoutDashboard,
  LogOut,
  MessageSquare,
  Settings,
  UserCog,
  Users,
} from "lucide-react";
import { signOut } from "@/lib/auth";

interface NavItem {
  href: string;
  label: string;
  Icon: React.ComponentType<{ className?: string }>;
  children?: { href: string; label: string; Icon: React.ComponentType<{ className?: string }> }[];
}

const navItems: NavItem[] = [
  { href: "/dashboard", label: "Dashboard", Icon: LayoutDashboard },
  { href: "/complaints", label: "Complaints", Icon: MessageSquare },
  { href: "/staff", label: "Staff", Icon: Users },
  { href: "/outages", label: "Outages", Icon: AlertTriangle },
  { href: "/reports/staff", label: "Reports", Icon: BarChart2 },
  {
    href: "/settings",
    label: "Settings",
    Icon: Settings,
    children: [{ href: "/settings/users", label: "Users", Icon: UserCog }],
  },
];

export function Sidebar() {
  const pathname = usePathname();
  const [settingsOpen, setSettingsOpen] = useState(
    pathname.startsWith("/settings")
  );

  function handleSignOut() {
    signOut();
    window.location.href = "/login";
  }

  return (
    <aside className="flex h-full w-60 flex-col border-r border-gray-200 bg-white">
      <nav className="flex-1 overflow-y-auto px-3 py-4">
        <ul className="space-y-1">
          {navItems.map(({ href, label, Icon, children }) => {
            if (children) {
              const isParentActive = pathname.startsWith(href);
              return (
                <li key={href}>
                  <button
                    onClick={() => setSettingsOpen((v) => !v)}
                    className={[
                      "flex w-full items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors",
                      isParentActive
                        ? "bg-blue-50 text-primary"
                        : "text-gray-600 hover:bg-gray-100 hover:text-gray-900",
                    ].join(" ")}
                  >
                    <Icon className="h-4 w-4 shrink-0" />
                    <span className="flex-1 text-left">{label}</span>
                    {settingsOpen ? (
                      <ChevronDown className="h-4 w-4" />
                    ) : (
                      <ChevronRight className="h-4 w-4" />
                    )}
                  </button>
                  {settingsOpen && (
                    <ul className="ml-7 mt-1 space-y-1">
                      {children.map(({ href: childHref, label: childLabel, Icon: ChildIcon }) => {
                        const isActive = pathname === childHref || pathname.startsWith(childHref + "/");
                        return (
                          <li key={childHref}>
                            <Link
                              href={childHref}
                              className={[
                                "flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors",
                                isActive
                                  ? "bg-blue-50 text-primary"
                                  : "text-gray-600 hover:bg-gray-100 hover:text-gray-900",
                              ].join(" ")}
                            >
                              <ChildIcon className="h-4 w-4 shrink-0" />
                              {childLabel}
                            </Link>
                          </li>
                        );
                      })}
                    </ul>
                  )}
                </li>
              );
            }

            const isActive =
              pathname === href || pathname.startsWith(href + "/");
            return (
              <li key={href}>
                <Link
                  href={href}
                  className={[
                    "flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors",
                    isActive
                      ? "bg-blue-50 text-primary"
                      : "text-gray-600 hover:bg-gray-100 hover:text-gray-900",
                  ].join(" ")}
                >
                  <Icon className="h-4 w-4 shrink-0" />
                  {label}
                </Link>
              </li>
            );
          })}
        </ul>
      </nav>

      <div className="border-t border-gray-200 p-3">
        <button
          onClick={handleSignOut}
          className="flex w-full items-center gap-3 rounded-md px-3 py-2 text-sm font-medium text-gray-600 hover:bg-gray-100 hover:text-gray-900"
        >
          <LogOut className="h-4 w-4 shrink-0" />
          Sign out
        </button>
      </div>
    </aside>
  );
}

