"use client";

import { TicketStatus, Urgency } from "@acls/shared-types";

interface ComplaintFiltersProps {
  status: string;
  urgency: string;
  category: string;
  search: string;
  dateFrom: string;
  dateTo: string;
  onStatusChange: (value: string) => void;
  onUrgencyChange: (value: string) => void;
  onCategoryChange: (value: string) => void;
  onSearchChange: (value: string) => void;
  onDateFromChange: (value: string) => void;
  onDateToChange: (value: string) => void;
}

export function ComplaintFilters({
  status,
  urgency,
  category,
  search,
  dateFrom,
  dateTo,
  onStatusChange,
  onUrgencyChange,
  onCategoryChange,
  onSearchChange,
  onDateFromChange,
  onDateToChange,
}: ComplaintFiltersProps) {
  const selectClass =
    "rounded-md border border-gray-300 bg-white px-3 py-1.5 text-sm shadow-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary";
  const inputClass =
    "rounded-md border border-gray-300 bg-white px-3 py-1.5 text-sm shadow-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary";

  return (
    <div className="flex flex-wrap items-end gap-3">
      <div className="flex flex-col gap-1">
        <label htmlFor="filter-search" className="text-xs font-medium text-gray-500">
          Search
        </label>
        <input
          id="filter-search"
          type="text"
          placeholder="Title or description..."
          value={search}
          onChange={(e) => onSearchChange(e.target.value)}
          className={`${inputClass} w-52`}
        />
      </div>

      <div className="flex flex-col gap-1">
        <label htmlFor="filter-status" className="text-xs font-medium text-gray-500">
          Status
        </label>
        <select
          id="filter-status"
          value={status}
          onChange={(e) => onStatusChange(e.target.value)}
          className={selectClass}
        >
          <option value="">All</option>
          {Object.values(TicketStatus).map((s) => (
            <option key={s} value={s}>
              {s.replace(/_/g, " ")}
            </option>
          ))}
        </select>
      </div>

      <div className="flex flex-col gap-1">
        <label htmlFor="filter-urgency" className="text-xs font-medium text-gray-500">
          Urgency
        </label>
        <select
          id="filter-urgency"
          value={urgency}
          onChange={(e) => onUrgencyChange(e.target.value)}
          className={selectClass}
        >
          <option value="">All</option>
          {Object.values(Urgency).map((u) => (
            <option key={u} value={u}>
              {u.replace(/_/g, " ")}
            </option>
          ))}
        </select>
      </div>

      <div className="flex flex-col gap-1">
        <label htmlFor="filter-category" className="text-xs font-medium text-gray-500">
          Category
        </label>
        <input
          id="filter-category"
          type="text"
          placeholder="e.g. Plumbing"
          value={category}
          onChange={(e) => onCategoryChange(e.target.value)}
          className={`${inputClass} w-36`}
        />
      </div>

      <div className="flex flex-col gap-1">
        <label htmlFor="filter-date-from" className="text-xs font-medium text-gray-500">
          From
        </label>
        <input
          id="filter-date-from"
          type="date"
          value={dateFrom}
          onChange={(e) => onDateFromChange(e.target.value)}
          className={inputClass}
        />
      </div>

      <div className="flex flex-col gap-1">
        <label htmlFor="filter-date-to" className="text-xs font-medium text-gray-500">
          To
        </label>
        <input
          id="filter-date-to"
          type="date"
          value={dateTo}
          onChange={(e) => onDateToChange(e.target.value)}
          className={inputClass}
        />
      </div>
    </div>
  );
}

