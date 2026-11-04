import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { RouterLink } from '@angular/router';
import { PagedResult, RequestListItem, RequestQuery, SortField } from '../../../core/api/models';
import { labels } from '../../../core/i18n/labels.he';
import { PriorityChip, StatusChip } from '../../../core/ui/request-chips';
import { DEFAULTS, PAGE_SIZES, toParams } from './query-params';

/** One page of requests. Sorting and paging happen on the server, so the table only reports what the user asked for. */
@Component({
  selector: 'app-requests-table',
  imports: [
    DatePipe,
    MatPaginatorModule,
    MatSortModule,
    MatTableModule,
    PriorityChip,
    RouterLink,
    StatusChip,
  ],
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

  // Travels with the link to a request, so its page can go back to this exact view
  protected readonly listParams = computed(() => toParams(this.query()));

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
