# Design System

**Document:** `docs/08_UX/design_system.md`  
**Version:** 1.0  
**Status:** Approved  
**Project:** Apartment Complaint Logging System (ACLS)

---

> [!IMPORTANT]
> This document defines the visual and component conventions for `ResidentApp.Web` (the Manager Dashboard). All React components generated for Phase 4a must follow these conventions. No component library, colour, or layout decision not described here may be introduced without updating this document first.

---

## 1. Technology Stack

| Concern | Choice | Notes |
|---|---|---|
| Framework | Next.js 14 (App Router) | TypeScript strict mode |
| Styling | Tailwind CSS (utility classes only) | No custom CSS files except global reset |
| Component library | shadcn/ui (Radix UI primitives) | Components copied into `src/components/ui/` — not imported from node_modules |
| Icons | `lucide-react` | Consistent icon set, tree-shakeable |
| Data fetching | TanStack Query v5 (`@tanstack/react-query`) | Server state management — no Redux |
| Forms | React Hook Form + Zod | Client-side field validation only (business validation is server-side) |
| Charts | Recharts | Dashboard metrics visualisation |
| Date formatting | `date-fns` | UTC-aware date formatting |
| HTTP client | Axios (via `packages/sdk`) | JWT bearer token auto-attached |

---

## 2. Colour Palette

The Manager Dashboard uses a neutral professional palette. All colours are defined as Tailwind CSS custom tokens in `tailwind.config.ts`.

| Token | Hex (light) | Usage |
|---|---|---|
| `primary` | `#185FA5` | Primary actions, active nav items, links |
| `primary-foreground` | `#FFFFFF` | Text on primary backgrounds |
| `destructive` | `#A32D2D` | Delete actions, error states |
| `warning` | `#854F0B` | High urgency, warnings |
| `success` | `#3B6D11` | Resolved status, success states |
| `muted` | `#888780` | Secondary text, placeholders |
| `border` | `#D3D1C7` | Card borders, dividers |
| `background` | `#F1EFE8` | Page background |
| `card` | `#FFFFFF` | Card surfaces |

### Urgency Colour Mapping

These colours are used consistently across badges, table rows, and status indicators:

| Urgency | Badge background | Badge text | Tailwind classes |
|---|---|---|---|
| `LOW` | `#EAF3DE` | `#3B6D11` | `bg-green-50 text-green-800` |
| `MEDIUM` | `#FAEEDA` | `#854F0B` | `bg-amber-50 text-amber-800` |
| `HIGH` | `#FAECE7` | `#993C1D` | `bg-orange-50 text-orange-800` |
| `SOS_EMERGENCY` | `#FCEBEB` | `#A32D2D` | `bg-red-50 text-red-800 font-semibold` |

### Status Colour Mapping

| Status | Colour | Tailwind classes |
|---|---|---|
| `OPEN` | Blue | `bg-blue-50 text-blue-800` |
| `ASSIGNED` | Purple | `bg-purple-50 text-purple-800` |
| `EN_ROUTE` | Indigo | `bg-indigo-50 text-indigo-800` |
| `IN_PROGRESS` | Amber | `bg-amber-50 text-amber-800` |
| `RESOLVED` | Green | `bg-green-50 text-green-800` |
| `CLOSED` | Gray | `bg-gray-100 text-gray-600` |

---

## 3. Typography

| Element | Tailwind class | Usage |
|---|---|---|
| Page heading | `text-2xl font-semibold text-gray-900` | H1 per page |
| Section heading | `text-lg font-medium text-gray-900` | Card titles, section titles |
| Body text | `text-sm text-gray-700` | Table cells, descriptions |
| Muted text | `text-sm text-gray-500` | Timestamps, secondary info |
| Monospace | `text-xs font-mono text-gray-600` | Complaint IDs |

---

## 4. Layout

### Shell

The Manager Dashboard uses a persistent sidebar + main content area layout:

```
┌─────────────────────────────────────────────────────┐
│  Header (64px) — Logo + User menu + Notifications   │
├──────────────┬──────────────────────────────────────┤
│              │                                      │
│  Sidebar     │  Main content area                   │
│  (240px)     │  (fluid width)                       │
│              │                                      │
│  Navigation  │  Page-specific content               │
│  items       │                                      │
│              │                                      │
└──────────────┴──────────────────────────────────────┘
```

- Sidebar is fixed on desktop (≥1024px), collapsible on tablet, hidden on mobile
- Main content area has `px-6 py-6` padding
- Max content width: `max-w-7xl mx-auto`

### Sidebar Navigation Items

```
Dashboard        (icon: LayoutDashboard)
Complaints       (icon: MessageSquare)
Staff            (icon: Users)
Outages          (icon: AlertTriangle)
Reports          (icon: BarChart2)
Settings         (icon: Settings)
  └── Users      (icon: UserCog)
```

---

## 5. Component Conventions

### 5.1 Badges

Used for `Urgency` and `TicketStatus` display everywhere. Never render raw enum strings.

```tsx
// src/components/ui/UrgencyBadge.tsx
interface UrgencyBadgeProps {
  urgency: Urgency;
}

export function UrgencyBadge({ urgency }: UrgencyBadgeProps) {
  const config: Record<Urgency, { label: string; className: string }> = {
    LOW: { label: 'Low', className: 'bg-green-50 text-green-800' },
    MEDIUM: { label: 'Medium', className: 'bg-amber-50 text-amber-800' },
    HIGH: { label: 'High', className: 'bg-orange-50 text-orange-800' },
    SOS_EMERGENCY: {
      label: 'EMERGENCY',
      className: 'bg-red-50 text-red-800 font-semibold',
    },
  };
  const { label, className } = config[urgency];
  return (
    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs ${className}`}>
      {label}
    </span>
  );
}
```

A matching `StatusBadge` component follows the same pattern using the status colour mapping from Section 2.

### 5.2 Data Tables

All data tables use `@tanstack/react-table` for column definitions. Tables are never sorted client-side — sorting parameters are passed as query parameters to the API.

```tsx
// Column definition pattern
const columns: ColumnDef<ComplaintDto>[] = [
  {
    accessorKey: 'complaintId',
    header: 'ID',
    cell: ({ row }) => (
      <span className="text-xs font-mono text-gray-600">
        #{row.original.complaintId}
      </span>
    ),
  },
  {
    accessorKey: 'urgency',
    header: 'Urgency',
    cell: ({ row }) => <UrgencyBadge urgency={row.original.urgency} />,
    // Sorting via API: pass sortBy=urgency&sortDirection=asc to GET /complaints
  },
];
```

### 5.3 Forms

All forms use React Hook Form with Zod schemas for field-level validation only. Business rule validation (e.g. is this staff member available?) is never performed client-side — it is enforced by the API.

```tsx
const assignSchema = z.object({
  staffMemberId: z.number({ required_error: 'Please select a staff member' }),
});

type AssignFormValues = z.infer<typeof assignSchema>;
```

### 5.4 Loading States

Every data-fetching component shows a skeleton loader using `shadcn/ui`'s `Skeleton` component while TanStack Query is in the loading state. Never show a blank page or a spinner alone.

### 5.5 Error States

Every data-fetching component handles errors from TanStack Query using the `errorCode` field from the API response. Error messages reference the catalogue in `docs/05_API/error_codes.md`.

---

## 6. Page Inventory

Every page in the Manager Dashboard:

| Route | Page component | Data source |
|---|---|---|
| `/dashboard` | `DashboardPage` | `GET /reports/dashboard` |
| `/complaints` | `ComplaintsPage` | `GET /complaints` (paginated, filterable) |
| `/complaints/[id]` | `ComplaintDetailPage` | `GET /complaints/{id}` + `GET /dispatch/recommendations/{id}` |
| `/staff` | `StaffPage` | `GET /staff` |
| `/outages` | `OutagesPage` | `GET /outages` |
| `/outages/new` | `DeclareOutagePage` | `POST /outages` |
| `/reports/staff` | `StaffPerformancePage` | `GET /reports/staff-performance` |
| `/reports/units` | `UnitHistoryPage` | `GET /reports/unit-history/{unitId}` |
| `/reports/summary` | `ComplaintSummaryPage` | `GET /reports/complaints-summary` |
| `/settings/users` | `UsersPage` | `GET /users` |
| `/settings/users/invite` | `InviteResidentPage` | `POST /users/invite` |

---

## 7. Responsive Breakpoints

The Manager Dashboard is desktop-first. Mobile support is secondary.

| Breakpoint | Tailwind prefix | Behaviour |
|---|---|---|
| Mobile (< 768px) | default | Sidebar hidden, hamburger menu, single-column layout |
| Tablet (768px–1023px) | `md:` | Sidebar collapsible, two-column layout where applicable |
| Desktop (≥ 1024px) | `lg:` | Full sidebar visible, multi-column layout |

---

*End of Design System v1.0*
