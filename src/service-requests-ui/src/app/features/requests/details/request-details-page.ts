import { DatePipe, Location } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute, Params, Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { RequestDetails, RequestStatus, StatusHistoryEntry } from '../../../core/api/models';
import { RequestsApiService } from '../../../core/api/requests-api.service';
import { ApiError } from '../../../core/http/api-error';
import { labels, pageTitle, statusLabel } from '../../../core/i18n/labels.he';
import { PriorityChip, StatusChip } from '../../../core/ui/request-chips';
import { statusIcon } from '../../../core/ui/status-presentation';
import { StatusHistory } from './status-history.component';

/** One request: its details, the statuses it may move to, and its history. */
@Component({
  selector: 'app-request-details-page',
  imports: [
    DatePipe,
    MatButtonModule,
    MatIconModule,
    PriorityChip,
    RouterLink,
    StatusChip,
    StatusHistory,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './request-details-page.html',
  styleUrl: './request-details-page.scss',
})
export class RequestDetailsPage {
  private readonly api = inject(RequestsApiService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly title = inject(Title);
  private readonly destroyRef = inject(DestroyRef);
  private readonly id = Number(inject(ActivatedRoute).snapshot.paramMap.get('id'));

  protected readonly labels = labels;
  protected readonly statusLabel = statusLabel;
  protected readonly statusIcon = statusIcon;
  protected readonly request = signal<RequestDetails | null>(null);
  protected readonly history = signal<readonly StatusHistoryEntry[]>([]);
  protected readonly loadError = signal<ApiError | null>(null);
  protected readonly saving = signal(false);

  // The list's filters travel with the link from the table; the browser keeps them on refresh.
  // A page opened from an address of its own goes back to the plain list
  protected readonly backParams: Params =
    (
      (inject(Router).currentNavigation()?.extras.state ?? inject(Location).getState()) as {
        listQuery?: Params;
      } | null
    )?.listQuery ?? {};

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loadError.set(null);
    forkJoin([this.api.getById(this.id), this.api.getHistory(this.id)])
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ([request, history]) => {
          this.request.set(request);
          this.history.set(history);
          this.title.setTitle(pageTitle(request.title));
        },
        error: (error: ApiError) => this.loadError.set(error),
      });
  }

  /** Sends the version the user saw; the server answers 409 if someone changed the request since. */
  protected changeStatus(status: RequestStatus): void {
    const current = this.request()!;
    this.saving.set(true);
    this.api
      .updateStatus(current.id, { status, rowVersion: current.rowVersion })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (updated) => {
          this.saving.set(false);
          this.request.set(updated);
          this.snackBar.open(`${labels.statusUpdated}: ${statusLabel[updated.status]}`, undefined, {
            duration: 4000,
          });
          this.api
            .getHistory(updated.id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe((history) => this.history.set(history));
        },
        error: (error: ApiError) => {
          this.saving.set(false);
          // A conflict gets its own dialog in the next part; until then every failure is a message
          this.snackBar.open(error.message, labels.close, { duration: 8000 });
        },
      });
  }
}
