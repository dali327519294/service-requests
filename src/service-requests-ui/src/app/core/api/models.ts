import { RequestPriority, RequestStatus } from './request-enums';

export type { RequestPriority, RequestStatus };
export type SortField = 'CreatedAt' | 'UpdatedAt' | 'Priority' | 'Status' | 'OrganizationName';
export type SortDirection = 'Asc' | 'Desc';

/** Search and filter parameters; every field is optional and an empty one is not sent. */
export interface RequestQuery {
  search?: string;
  status?: readonly RequestStatus[];
  priority?: readonly RequestPriority[];
  organizationName?: string;
  assignedTo?: string;
  createdFrom?: Date;
  createdTo?: Date;
  sortBy?: SortField;
  sortDirection?: SortDirection;
  page?: number;
  pageSize?: number;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface RequestListItem {
  id: number;
  title: string;
  organizationName: string;
  status: RequestStatus;
  priority: RequestPriority;
  assignedTo: string | null;
  createdAt: string;
  updatedAt: string;
  rowVersion: string;
}

export interface RequestDetails extends RequestListItem {
  allowedTransitions: RequestStatus[];
}

export interface StatusHistoryEntry {
  previousStatus: RequestStatus;
  newStatus: RequestStatus;
  changedAt: string;
  changedBy: string;
}

export interface RequestSummary {
  total: number;
  byStatus: Record<RequestStatus, number>;
  byPriority: Record<RequestPriority, number>;
}

export interface UpdateStatusBody {
  status: RequestStatus;
  rowVersion: string;
}

export interface BulkUpdateBody {
  status: RequestStatus;
  items: { id: number; rowVersion: string }[];
}

export type BulkOutcome = 'Updated' | 'NotFound' | 'Conflict' | 'InvalidTransition';

export interface BulkUpdateResult {
  requested: number;
  succeeded: number;
  failed: number;
  results: { id: number; outcome: BulkOutcome; error: string | null; rowVersion: string | null }[];
}
