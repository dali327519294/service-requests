import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { RequestStatus } from '../../../core/api/models';
import { labels } from '../../../core/i18n/labels.he';
import { ActiveFilterChips } from './active-filter-chips.component';
import { RequestsSearchState } from './requests-search.state';
import { RequestFilters } from './request-filters.component';
import { SummaryCards } from './summary-cards.component';

const counts = new Intl.NumberFormat('he-IL');

/** The requests page: its state lives in the URL, through RequestsSearchState. */
@Component({
  selector: 'app-requests-list-page',
  imports: [
    ActiveFilterChips,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    RequestFilters,
    SummaryCards,
  ],
  providers: [RequestsSearchState],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './requests-list-page.html',
  styleUrl: './requests-list-page.scss',
})
export class RequestsListPage {
  protected readonly state = inject(RequestsSearchState);
  protected readonly labels = labels;
  protected readonly skeletonRows = [1, 2, 3, 4, 5, 6];
  protected readonly countText = computed(() => {
    const page = this.state.page();
    return page ? `נמצאו ${counts.format(page.totalCount)} פניות` : '';
  });

  /** A card adds its status to the filter, or takes it out if it is already there. */
  protected toggleStatus(status: RequestStatus): void {
    const current = this.state.query().status ?? [];
    const next = current.includes(status)
      ? current.filter((s) => s !== status)
      : [...current, status];
    void this.state.update({ status: next });
  }
}
