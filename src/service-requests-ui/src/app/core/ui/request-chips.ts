import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { RequestPriority, RequestStatus } from '../api/request-enums';
import { priorityLabel, statusLabel } from '../i18n/labels.he';
import { priorityIcon, statusIcon, statusToken } from './status-presentation';

/** A status as a chip: fixed width, icon at the start, the same look in every screen (styles.scss). */
@Component({
  selector: 'app-status-chip',
  imports: [MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'chip chip--status', '[attr.data-status]': 'token()' },
  template: `<mat-icon aria-hidden="true">{{ icon() }}</mat-icon
    >{{ text() }}`,
})
export class StatusChip {
  readonly status = input.required<RequestStatus>();
  protected readonly token = computed(() => statusToken[this.status()]);
  protected readonly icon = computed(() => statusIcon[this.status()]);
  protected readonly text = computed(() => statusLabel[this.status()]);
}

/** A priority as a chip; a high priority stands out in color. */
@Component({
  selector: 'app-priority-chip',
  imports: [MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'chip chip--priority', '[attr.data-priority]': 'priority()' },
  template: `<mat-icon aria-hidden="true">{{ icon() }}</mat-icon
    >{{ text() }}`,
})
export class PriorityChip {
  readonly priority = input.required<RequestPriority>();
  protected readonly icon = computed(() => priorityIcon[this.priority()]);
  protected readonly text = computed(() => priorityLabel[this.priority()]);
}
