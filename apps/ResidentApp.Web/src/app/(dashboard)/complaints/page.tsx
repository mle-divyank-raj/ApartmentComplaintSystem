"use client";

import { useEffect, useState, useCallback } from "react";
import { getAllComplaints } from "@/lib/api/complaints";
import type { ComplaintsPage } from "@acls/api-contracts";
import { ComplaintTable } from "@/components/complaints/ComplaintTable";
import { ComplaintFilters } from "@/components/complaints/ComplaintFilters";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import { ErrorAlert } from "@/components/ui/ErrorAlert";
import { Button } from "@/components/ui/Button";
import axios from "axios";

const PAGE_SIZE = 20;

export default function ComplaintsPage() {
  const [data, setData] = useState<ComplaintsPage | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState("");
  const [urgencyFilter, setUrgencyFilter] = useState("");
  const [categoryFilter, setCategoryFilter] = useState("");
  const [searchFilter, setSearchFilter] = useState("");
  const [dateFromFilter, setDateFromFilter] = useState("");
  const [dateToFilter, setDateToFilter] = useState("");
  const [page, setPage] = useState(1);

  const fetchComplaints = useCallback(
    (
      currentPage: number,
      status: string,
      urgency: string,
      category: string,
      search: string,
      dateFrom: string,
      dateTo: string
    ) => {
      setLoading(true);
      setError(null);
      getAllComplaints({
        status: status || undefined,
        urgency: urgency || undefined,
        category: category || undefined,
        search: search || undefined,
        dateFrom: dateFrom || undefined,
        dateTo: dateTo || undefined,
        page: currentPage,
        pageSize: PAGE_SIZE,
      })
        .then(setData)
        .catch((err) => {
          if (axios.isAxiosError(err)) {
            setError(err.response?.data?.detail ?? "Failed to load complaints.");
          } else {
            setError("Failed to load complaints.");
          }
        })
        .finally(() => setLoading(false));
    },
    []
  );

  useEffect(() => {
    fetchComplaints(
      page,
      statusFilter,
      urgencyFilter,
      categoryFilter,
      searchFilter,
      dateFromFilter,
      dateToFilter
    );
  }, [
    fetchComplaints,
    page,
    statusFilter,
    urgencyFilter,
    categoryFilter,
    searchFilter,
    dateFromFilter,
    dateToFilter,
  ]);

  function handleStatusChange(value: string) {
    setStatusFilter(value);
    setPage(1);
  }

  function handleUrgencyChange(value: string) {
    setUrgencyFilter(value);
    setPage(1);
  }

  function handleCategoryChange(value: string) {
    setCategoryFilter(value);
    setPage(1);
  }

  function handleSearchChange(value: string) {
    setSearchFilter(value);
    setPage(1);
  }

  function handleDateFromChange(value: string) {
    setDateFromFilter(value);
    setPage(1);
  }

  function handleDateToChange(value: string) {
    setDateToFilter(value);
    setPage(1);
  }

  const totalPages = data ? Math.ceil(data.totalCount / PAGE_SIZE) : 1;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Complaints</h1>
        {data && (
          <p className="text-sm text-gray-500">
            {data.totalCount} total
          </p>
        )}
      </div>

      <ComplaintFilters
        status={statusFilter}
        urgency={urgencyFilter}
        category={categoryFilter}
        search={searchFilter}
        dateFrom={dateFromFilter}
        dateTo={dateToFilter}
        onStatusChange={handleStatusChange}
        onUrgencyChange={handleUrgencyChange}
        onCategoryChange={handleCategoryChange}
        onSearchChange={handleSearchChange}
        onDateFromChange={handleDateFromChange}
        onDateToChange={handleDateToChange}
      />

      {error && <ErrorAlert message={error} />}

      {loading ? (
        <LoadingSpinner />
      ) : (
        <div className="overflow-hidden rounded-lg border border-gray-200 bg-white">
          <ComplaintTable complaints={data?.items ?? []} />
        </div>
      )}

      {data && totalPages > 1 && (
        <div className="flex items-center justify-between">
          <Button
            variant="secondary"
            size="sm"
            disabled={page <= 1}
            onClick={() => setPage((p) => p - 1)}
          >
            Previous
          </Button>
          <span className="text-sm text-gray-600">
            Page {page} of {totalPages}
          </span>
          <Button
            variant="secondary"
            size="sm"
            disabled={page >= totalPages}
            onClick={() => setPage((p) => p + 1)}
          >
            Next
          </Button>
        </div>
      )}
    </div>
  );
}
