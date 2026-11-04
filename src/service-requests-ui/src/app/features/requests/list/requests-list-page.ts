import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { labels } from '../../../core/i18n/labels.he';
import { RequestsSearchState } from './requests-search.state';

const counts = new Intl.NumberFormat('he-IL');

/** The requests page: its state lives in the URL, through RequestsSearchState. */
@Component({
  selector: 'app-requests-list-page',
  imports: [MatButtonModule, MatIconModule, MatProgressBarModule],
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
}
