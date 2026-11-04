import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { StatusHistoryEntry } from '../../../core/api/models';
import { labels } from '../../../core/i18n/labels.he';
import { StatusChip } from '../../../core/ui/request-chips';

/** Status changes, newest first, in the order the server sends them. */
@Component({
  selector: 'app-status-history',
  imports: [DatePipe, MatIconModule, StatusChip],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (entries().length) {
      <ol class="history">
        @for (entry of entries(); track $index) {
          <li class="history__item">
            <span class="history__when" dir="ltr">{{
              entry.changedAt | date: 'dd/MM/yy HH:mm'
            }}</span>
            <span class="history__change">
              <app-status-chip [status]="entry.previousStatus" />
              <mat-icon class="history__arrow" aria-hidden="true">arrow_back</mat-icon>
              <span class="visually-hidden">{{ labels.changedTo }}</span>
              <app-status-chip [status]="entry.newStatus" />
            </span>
            <span class="history__who">{{ entry.changedBy }}</span>
          </li>
        }
      </ol>
    } @else {
      <p class="muted">{{ labels.noHistory }}</p>
    }
  `,
  styles: `
    .history {
      margin: 0;
      padding: 0;
      list-style: none;
    }

    // One line per change: when, from → to, who
    .history__item {
      display: grid;
      grid-template-columns: 120px auto 1fr;
      align-items: center;
      gap: 16px;
      padding: 10px 0;
      border-top: 1px solid var(--mat-sys-outline-variant);

      &:first-child {
        border-top: 0;
      }
    }

    .history__when {
      font-variant-numeric: tabular-nums;
      color: var(--mat-sys-on-surface-variant);
    }

    .history__change {
      display: flex;
      align-items: center;
      gap: 6px;
    }

    .history__arrow {
      width: 18px;
      height: 18px;
      font-size: 18px;
      color: var(--mat-sys-outline);
    }

    .history__who {
      color: var(--mat-sys-on-surface-variant);
    }

    .muted {
      margin: 0;
      color: var(--mat-sys-on-surface-variant);
    }

    @media (max-width: 599px) {
      .history__item {
        grid-template-columns: 1fr;
        gap: 4px;
      }
    }
  `,
})
export class StatusHistory {
  readonly entries = input.required<readonly StatusHistoryEntry[]>();
  protected readonly labels = labels;
}
