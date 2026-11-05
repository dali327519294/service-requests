import { DatePipe, Location } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute, Params, Router, RouterLink } from '@angular/router';
import { catchError, forkJoin, of, switchMap } from 'rxjs';
import { RequestDetails, RequestStatus, StatusHistoryEntry } from '../../../core/api/models';
import { RequestsApiService } from '../../../core/api/requests-api.service';
import { ApiError } from '../../../core/http/api-error';
import { labels, pageTitle, statusLabel } from '../../../core/i18n/labels.he';
import { PriorityChip, StatusChip } from '../../../core/ui/request-chips';
import { statusIcon } from '../../../core/ui/status-presentation';
import { ConflictChoice, ConflictData, ConflictDialog } from '../dialogs/conflict-dialog.component';
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
  private readonly dialog = inject(MatDialog);
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
  // The status buttons change with the status, so the button that was used is gone afterwards;
  // focus goes to the section title instead of falling back to the top of the page
  private readonly changeTitle = viewChild<ElementRef<HTMLElement>>('changeTitle');

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
          this.focusChangeTitle();
          this.snackBar.open(`${labels.statusUpdated}: ${statusLabel[updated.status]}`, undefined, {
            duration: 4000,
          });
          this.api
            .getHistory(updated.id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
              next: (history) => this.history.set(history),
              // The change is saved; only the list of changes is out of date, so it stays and the user is told
              error: () =>
                this.snackBar.open(labels.historyRefreshFailed, labels.close, { duration: 8000 }),
            });
        },
        error: (error: ApiError) => {
          this.saving.set(false);
          if (error.kind === 'conflict' && error.current) {
            this.resolveConflict(status, error.current);
          } else {
            this.snackBar.open(error.message, labels.close, { duration: 8000 });
          }
        },
      });
  }

  /**
   * The page shows the current state at once, so nothing stale stays behind the dialog.
   * The history says who changed it; if it fails to load, the dialog opens without that line.
   */
  private resolveConflict(attempted: RequestStatus, current: RequestDetails): void {
    this.request.set(current);
    this.api
      .getHistory(current.id)
      .pipe(
        catchError(() => of(null)),
        switchMap((history) => {
          if (history) {
            this.history.set(history);
          }
          return this.dialog
            .open<ConflictDialog, ConflictData, ConflictChoice>(ConflictDialog, {
              data: { attempted, current, lastChange: history?.[0] },
              width: '440px',
              restoreFocus: false, // the button that opened it may no longer exist
            })
            .afterClosed();
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((choice) => {
        if (choice === 'retry') {
          this.changeStatus(attempted); // now with the current rowVersion
        } else {
          this.focusChangeTitle();
        }
      });
  }

  private focusChangeTitle(): void {
    this.changeTitle()?.nativeElement.focus();
  }
}
