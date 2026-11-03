import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { labels } from '../../../core/i18n/labels.he';

/** The requests page. For now only its frame; the list arrives in the next tasks. */
@Component({
  selector: 'app-requests-list-page',
  imports: [MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1 class="page-title">{{ labels.requestsTitle }}</h1>
    <section class="empty-state" aria-live="polite">
      <mat-icon class="empty-state__icon" aria-hidden="true">inbox</mat-icon>
      <h2 class="empty-state__title">{{ labels.requestsEmptyTitle }}</h2>
      <p class="empty-state__body">{{ labels.requestsEmptyBody }}</p>
    </section>
  `,
  styles: `
    .page-title {
      margin: 0 0 24px;
      font: var(--mat-sys-headline-small);
    }
    .empty-state {
      display: grid;
      justify-items: center;
      gap: 8px;
      padding: 64px 24px;
      border: 1px solid var(--mat-sys-outline-variant);
      border-radius: var(--app-radius);
      background: var(--mat-sys-surface);
      text-align: center;
    }
    .empty-state__icon {
      width: 48px;
      height: 48px;
      font-size: 48px;
      color: var(--mat-sys-primary);
    }
    .empty-state__title {
      margin: 0;
      font: var(--mat-sys-title-medium);
    }
    .empty-state__body {
      margin: 0;
      color: var(--mat-sys-on-surface-variant);
    }
  `,
})
export class RequestsListPage {
  protected readonly labels = labels;
}
