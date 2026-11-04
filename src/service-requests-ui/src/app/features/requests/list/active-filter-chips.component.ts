import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { RequestQuery } from '../../../core/api/models';
import { labels, priorityLabel, statusLabel } from '../../../core/i18n/labels.he';
import { addDays } from './query-params';

interface Chip {
  label: string;
  patch: Partial<RequestQuery>;
}

// In a Hebrew line a date range would be laid out right to left ("31/10–01/10");
// the Unicode isolate marks keep it in reading order
const leftToRight = (text: string) => `⁦${text}⁩`;
const pad = (n: number) => String(n).padStart(2, '0');
const day = (d: Date) =>
  `${pad(d.getDate())}/${pad(d.getMonth() + 1)}/${String(d.getFullYear()).slice(2)}`;

/** One chip per active filter; removing a chip removes only that filter. */
@Component({
  selector: 'app-active-filter-chips',
  imports: [MatButtonModule, MatChipsModule, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (chips().length) {
      <div class="active">
        <mat-chip-set [attr.aria-label]="labels.activeFilters">
          @for (chip of chips(); track chip.label) {
            <mat-chip (removed)="removed.emit(chip.patch)">
              {{ chip.label }}
              <button matChipRemove [attr.aria-label]="labels.removeFilter + ': ' + chip.label">
                <mat-icon>close</mat-icon>
              </button>
            </mat-chip>
          }
        </mat-chip-set>
        <button mat-button type="button" (click)="cleared.emit()">{{ labels.clearAll }}</button>
      </div>
    }
  `,
  styles: `
    .active {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 8px;
      margin-bottom: 16px;
    }
  `,
})
export class ActiveFilterChips {
  readonly query = input.required<RequestQuery>();
  readonly removed = output<Partial<RequestQuery>>();
  readonly cleared = output<void>();
  protected readonly labels = labels;

  protected readonly chips = computed<Chip[]>(() => {
    const q = this.query();
    const chips: Chip[] = [];
    if (q.search) {
      chips.push({ label: `${labels.searchChip}: ${q.search}`, patch: { search: undefined } });
    }

    for (const s of q.status ?? []) {
      chips.push({
        label: `${labels.statusChip}: ${statusLabel[s]}`,
        patch: { status: q.status!.filter((x) => x !== s) },
      });
    }

    for (const p of q.priority ?? []) {
      chips.push({
        label: `${labels.priority}: ${priorityLabel[p]}`,
        patch: { priority: q.priority!.filter((x) => x !== p) },
      });
    }

    if (q.organizationName) {
      chips.push({
        label: `${labels.organizationChip}: ${q.organizationName}`,
        patch: { organizationName: undefined },
      });
    }

    if (q.assignedTo) {
      chips.push({
        label: `${labels.assignedTo}: ${q.assignedTo}`,
        patch: { assignedTo: undefined },
      });
    }

    if (q.createdFrom || q.createdTo) {
      // The query holds the first moment after the range; people think of the last day they chose
      const from = q.createdFrom ? day(q.createdFrom) : '…';
      const to = q.createdTo ? day(addDays(q.createdTo, -1)) : '…';
      chips.push({
        label: `${labels.createdChip}: ${leftToRight(`${from}–${to}`)}`,
        patch: { createdFrom: undefined, createdTo: undefined },
      });
    }

    return chips;
  });
}
