import { HttpClient, HttpParameterCodec, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  BulkUpdateBody,
  BulkUpdateResult,
  PagedResult,
  RequestDetails,
  RequestListItem,
  RequestQuery,
  RequestSummary,
  StatusHistoryEntry,
  UpdateStatusBody,
} from './models';

// Angular's default codec leaves "+" as is, and the server reads "+" in a query as a space
const strictCodec: HttpParameterCodec = {
  encodeKey: (key) => encodeURIComponent(key),
  encodeValue: (value) => encodeURIComponent(value),
  decodeKey: (key) => decodeURIComponent(key),
  decodeValue: (value) => decodeURIComponent(value),
};

const base = '/api/requests';

/** The only place that knows the API's addresses. */
@Injectable({ providedIn: 'root' })
export class RequestsApiService {
  private readonly http = inject(HttpClient);

  search(query: RequestQuery): Observable<PagedResult<RequestListItem>> {
    return this.http.get<PagedResult<RequestListItem>>(base, { params: toParams(query) });
  }

  getSummary(query: RequestQuery): Observable<RequestSummary> {
    return this.http.get<RequestSummary>(`${base}/summary`, { params: toParams(query) });
  }

  getById(id: number): Observable<RequestDetails> {
    return this.http.get<RequestDetails>(`${base}/${id}`);
  }

  getHistory(id: number): Observable<StatusHistoryEntry[]> {
    return this.http.get<StatusHistoryEntry[]>(`${base}/${id}/history`);
  }

  updateStatus(id: number, body: UpdateStatusBody): Observable<RequestDetails> {
    return this.http.patch<RequestDetails>(`${base}/${id}/status`, body);
  }

  bulkUpdate(body: BulkUpdateBody): Observable<BulkUpdateResult> {
    return this.http.post<BulkUpdateResult>(`${base}/bulk-status`, body);
  }
}

function toParams(query: RequestQuery): HttpParams {
  let params = new HttpParams({ encoder: strictCodec });
  for (const [key, value] of Object.entries(query)) {
    for (const item of Array.isArray(value) ? value : [value]) {
      if (item === undefined || item === null || item === '') {
        continue;
      }

      // Dates go as UTC instants ("...Z"), so the server never has to guess a time zone
      params = params.append(key, item instanceof Date ? item.toISOString() : String(item));
    }
  }

  return params;
}
