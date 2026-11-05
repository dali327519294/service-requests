import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { RequestStatus, RequestSummary } from '../../../core/api/models';
import { REQUEST_STATUSES } from '../../../core/api/request-enums';
import { labels, statusPluralLabel } from '../../../core/i18n/labels.he';
import { statusIcon, statusToken } from '../../../core/ui/status-presentation';

const counts = new Intl.NumberFormat('he-IL');

/** One card per status: shows how many requests each status would give, and toggles it as a filter. */
@Component({
  selector: 'app-summary-cards',
  imports: [MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="cards"
      role="group"
      [attr.aria-label]="labels.statusFilterGroup"
      [attr.aria-busy]="!summary() && !failed()"
    >
      @for (status of statuses; track status) {
        <button
          type="button"
          class="card"
          [attr.data-status]="token[status]"
          [class.card--selected]="isSelected(status)"
          [attr.aria-pressed]="isSelected(status)"
          (click)="statusToggled.emit(status)"
        >
          <span class="card__label">
            <mat-icon class="card__icon" aria-hidden="true">{{ icon[status] }}</mat-icon>
            {{ plural[status] }}
            @if (isSelected(status)) {
              <mat-icon class="card__check" aria-hidden="true">check</mat-icon>
            }
          </span>
          @if (summary(); as s) {
            <span class="card__count">{{ format(s.byStatus[status]) }}</span>
          } @else if (failed()) {
            <span class="card__count card__count--missing" [attr.aria-label]="labels.countMissing"
              >—</span
            >
          } @else {
            <span class="card__placeholder" aria-hidden="true"></span>
          }
        </button>
      }
    </div>
  `,
  styleUrl: './summary-cards.component.scss',
})
export class SummaryCards {
  readonly summary = input<RequestSummary | null>(null);
  readonly failed = input(false);
  readonly selected = input<readonly RequestStatus[] | undefined>(undefined);
  readonly statusToggled = output<RequestStatus>();

  protected readonly statuses = REQUEST_STATUSES;
  protected readonly icon = statusIcon;
  protected readonly token = statusToken;
  protected readonly plural = statusPluralLabel;
  protected readonly labels = labels;

  protected isSelected(status: RequestStatus): boolean {
    return this.selected()?.includes(status) ?? false;
  }

  protected format(count: number | undefined): string {
    return counts.format(count ?? 0);
  }
}
