import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ConnectionStatusService } from '../http/connection-status.service';
import { labels } from '../i18n/labels.he';

/** One banner for the whole app when the API cannot be reached. */
@Component({
  selector: 'app-connection-banner',
  imports: [MatButtonModule, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (!connection.online()) {
      <div class="banner" role="alert">
        <mat-icon aria-hidden="true">cloud_off</mat-icon>
        <span class="banner__text">{{ labels.offlineMessage }}</span>
        <button mat-stroked-button type="button" (click)="connection.retry()">
          {{ labels.retry }}
        </button>
      </div>
    }
  `,
  styles: `
    .banner {
      display: flex;
      align-items: center;
      gap: 12px;
      /* Lines up with the page content, which has a 24px gutter (16px on phones) */
      width: calc(100% - 48px);
      max-width: calc(var(--app-content-width) - 48px);
      margin: 16px auto 0;
      padding: 12px 16px;
      box-sizing: border-box;
      border-radius: var(--app-radius);
      background: var(--mat-sys-error-container);
      color: var(--mat-sys-on-error-container);
    }
    .banner__text {
      flex: 1;
    }
    @media (max-width: 600px) {
      .banner {
        width: calc(100% - 32px);
      }
    }
  `,
})
export class ConnectionBanner {
  protected readonly connection = inject(ConnectionStatusService);
  protected readonly labels = labels;
}
