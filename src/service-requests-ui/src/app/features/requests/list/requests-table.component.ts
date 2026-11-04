import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { RouterLink } from '@angular/router';
import { PagedResult, RequestListItem, RequestQuery, SortField } from '../../../core/api/models';
import { labels, priorityLabel, statusLabel } from '../../../core/i18n/labels.he';
import { priorityIcon, statusIcon, statusToken } from '../../../core/ui/status-presentation';
import { DEFAULTS, PAGE_SIZES } from './query-params';

interface Row extends RequestListItem {
  statusText: string;
  statusIcon: string;
  statusToken: string;
  priorityText: string;
  priorityIcon: string;
}

/** One page of requests. Sorting and paging happen on the server, so the table only reports what the user asked for. */
@Component({
  selector: 'app-requests-table',
  imports: [DatePipe, MatIconModule, MatPaginatorModule, MatSortModule, MatTableModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './requests-table.component.html',
  styleUrl: './requests-table.component.scss',
})
export class RequestsTable {
  readonly result = input.required<PagedResult<RequestListItem>>();
  readonly query = input.required<RequestQuery>();
  readonly sortChanged = output<Pick<RequestQuery, 'sortBy' | 'sortDirection'>>();
  readonly pageChanged = output<Pick<RequestQuery, 'page' | 'pageSize'>>();

  protected readonly labels = labels;
  protected readonly pageSizes = [...PAGE_SIZES];
  protected readonly columns = [
    'id',
    'title',
    'organizationName',
    'status',
    'priority',
    'assignedTo',
    'createdAt',
    'updatedAt',
  ];

  // The cell templates are untyped (let r), so each row arrives with its display values ready
  protected readonly rows = computed<Row[]>(() =>
    this.result().items.map((item) => ({
      ...item,
      statusText: statusLabel[item.status],
      statusIcon: statusIcon[item.status],
      statusToken: statusToken[item.status],
      priorityText: priorityLabel[item.priority],
      priorityIcon: priorityIcon[item.priority],
    })),
  );

  // MatSort speaks 'asc' / 'desc'; the API and the URL speak 'Asc' / 'Desc'
  protected readonly direction = computed(() =>
    (this.query().sortDirection ?? DEFAULTS.sortDirection) === 'Asc' ? 'asc' : 'desc',
  );

  protected sort({ active, direction }: Sort): void {
    this.sortChanged.emit({
      sortBy: active as SortField,
      sortDirection: direction === 'asc' ? 'Asc' : 'Desc',
    });
  }

  /** A new page size starts over at the first page; otherwise the page the user picked. */
  protected paged(event: PageEvent): void {
    const sizeChanged = event.pageSize !== this.result().pageSize;
    this.pageChanged.emit({
      page: sizeChanged ? 1 : event.pageIndex + 1,
      pageSize: event.pageSize,
    });
  }
}
