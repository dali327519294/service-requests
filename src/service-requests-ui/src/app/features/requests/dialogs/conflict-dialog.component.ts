import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { RequestDetails, RequestStatus, StatusHistoryEntry } from '../../../core/api/models';
import { labels, statusLabel } from '../../../core/i18n/labels.he';
import { StatusChip } from '../../../core/ui/request-chips';

export interface ConflictData {
  attempted: RequestStatus;
  current: RequestDetails;
  lastChange?: StatusHistoryEntry;
}

export type ConflictChoice = 'reload' | 'retry';

/** Someone changed the request first: shows what it is now and lets the user decide again. */
@Component({
  selector: 'app-conflict-dialog',
  imports: [DatePipe, MatButtonModule, MatDialogModule, StatusChip],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>{{ labels.conflictTitle }}</h2>
    <mat-dialog-content>
      <p class="body">{{ labels.conflictBody }}</p>
      <dl class="compare">
        <div>
          <dt>{{ labels.conflictNow }}</dt>
          <dd>
            <app-status-chip [status]="data.current.status" />
            @if (data.lastChange; as change) {
              <span class="by">
                {{ labels.conflictChangedBy }} {{ change.changedBy }},
                <span dir="ltr">{{ change.changedAt | date: 'dd/MM/yy HH:mm' }}</span>
              </span>
            }
          </dd>
        </div>
        <div>
          <dt>{{ labels.conflictYouChose }}</dt>
          <dd><app-status-chip [status]="data.attempted" /></dd>
        </div>
      </dl>
      @if (!canRetry) {
        <p class="note">{{ labels.conflictNoLongerAllowed(attemptedLabel) }}</p>
      }
    </mat-dialog-content>
    <mat-dialog-actions>
      <button mat-stroked-button type="button" [mat-dialog-close]="'reload'" cdkFocusInitial>
        {{ labels.conflictShowCurrent }}
      </button>
      @if (canRetry) {
        <button mat-flat-button type="button" [mat-dialog-close]="'retry'">
          {{ labels.conflictRetry }}: {{ attemptedLabel }}
        </button>
      }
    </mat-dialog-actions>
  `,
  styles: `
    .body {
      margin-top: 0;
    }

    .compare {
      display: grid;
      gap: 12px;
      margin: 0;

      dt {
        color: var(--mat-sys-on-surface-variant);
        font: var(--mat-sys-label-medium);
      }

      dd {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: 8px;
        margin: 4px 0 0;
      }
    }

    .by {
      color: var(--mat-sys-on-surface-variant);
      font: var(--mat-sys-body-small);
    }

    .note {
      margin-bottom: 0;
      color: var(--mat-sys-on-surface-variant);
    }
  `,
})
export class ConflictDialog {
  protected readonly data = inject<ConflictData>(MAT_DIALOG_DATA);
  protected readonly labels = labels;
  protected readonly attemptedLabel = statusLabel[this.data.attempted];
  // Offered only when the new status still allows the move the user chose
  protected readonly canRetry = this.data.current.allowedTransitions.includes(this.data.attempted);
}
