import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  input,
  output,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';

import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { debounceTime, distinctUntilChanged, map } from 'rxjs';
import { RequestPriority, RequestQuery } from '../../../core/api/models';
import { REQUEST_PRIORITIES } from '../../../core/api/request-enums';
import { labels, priorityLabel } from '../../../core/i18n/labels.he';
import { addDays } from './query-params';

/** Search and the "more filters" panel. Reports changes; the page puts them in the URL. */
@Component({
  selector: 'app-request-filters',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatSelectModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './request-filters.component.html',
  styleUrl: './request-filters.component.scss',
})
export class RequestFilters {
  readonly query = input.required<RequestQuery>();
  readonly changed = output<Partial<RequestQuery>>();

  protected readonly labels = labels;
  protected readonly priorities = REQUEST_PRIORITIES;
  protected readonly priorityLabel = priorityLabel;
  protected readonly open = signal(false);
  protected readonly form = new FormGroup({
    search: new FormControl('', { nonNullable: true }),
    organizationName: new FormControl('', { nonNullable: true }),
    assignedTo: new FormControl('', { nonNullable: true }),
    priority: new FormControl<RequestPriority[]>([], { nonNullable: true }),
    from: new FormControl<Date | null>(null),
    to: new FormControl<Date | null>(null),
  });

  /** Filters inside the panel that are set, shown on its button. */
  protected readonly advancedCount = computed(() => {
    const q = this.query();
    return [
      q.priority?.length,
      q.organizationName,
      q.assignedTo,
      q.createdFrom || q.createdTo,
    ].filter(Boolean).length;
  });

  constructor() {
    // The URL may change from outside (Back, a link): show it without sending it back
    effect(() => {
      const q = this.query();
      this.form.setValue(
        {
          search: q.search ?? '',
          organizationName: q.organizationName ?? '',
          assignedTo: q.assignedTo ?? '',
          priority: [...(q.priority ?? [])],
          from: q.createdFrom ?? null,
          to: q.createdTo ? addDays(q.createdTo, -1) : null,
        },
        { emitEvent: false },
      );
    });

    // Typing waits for a pause; a click is a whole decision and goes at once
    for (const key of ['search', 'organizationName', 'assignedTo'] as const) {
      this.form.controls[key].valueChanges
        .pipe(
          debounceTime(300),
          map((value) => value.trim()),
          distinctUntilChanged(),
          takeUntilDestroyed(),
        )
        .subscribe((value) => this.changed.emit({ [key]: value || undefined }));
    }

    this.form.controls.priority.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe((priority) => this.changed.emit({ priority }));
  }

  /** The picker shows the last day; the API gets the first moment after it. */
  protected datesChanged(): void {
    const { from, to } = this.form.getRawValue();
    this.changed.emit({
      createdFrom: from ?? undefined,
      createdTo: to ? addDays(to, 1) : undefined,
    });
  }
}
