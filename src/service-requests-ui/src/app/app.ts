import { ChangeDetectionStrategy, Component, ElementRef, viewChild } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { RouterOutlet } from '@angular/router';
import { labels } from './core/i18n/labels.he';
import { ConnectionBanner } from './core/ui/connection-banner.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, MatIconModule, ConnectionBanner],
  templateUrl: './app.html',
  styleUrl: './app.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  protected readonly labels = labels;
  private readonly main = viewChild.required<ElementRef<HTMLElement>>('main');

  // href="#main" resolves against <base href="/">: following it would reload the app at "/"
  // and drop the page and its query. Moving the focus keeps the user where they are.
  protected skipToMain(event: Event): void {
    event.preventDefault();
    this.main().nativeElement.focus();
  }
}
