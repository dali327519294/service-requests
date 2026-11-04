import { registerLocaleData } from '@angular/common';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import localeHe from '@angular/common/locales/he';
import {
  ApplicationConfig,
  inject,
  LOCALE_ID,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { MAT_DATE_LOCALE, provideNativeDateAdapter } from '@angular/material/core';
import { MatIconRegistry } from '@angular/material/icon';
import { MatPaginatorIntl } from '@angular/material/paginator';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { connectionInterceptor } from './core/http/connection.interceptor';
import { errorInterceptor } from './core/http/error.interceptor';
import { userHeaderInterceptor } from './core/http/user-header.interceptor';
import { HebrewPaginatorIntl } from './core/i18n/paginator-intl.he';

registerLocaleData(localeHe);

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    // Order matters: errors are mapped last (outermost), after the connection retries gave up
    provideHttpClient(
      withFetch(),
      withInterceptors([errorInterceptor, connectionInterceptor, userHeaderInterceptor]),
    ),
    // Dates and numbers in pipes, and the date picker, follow Israeli conventions
    { provide: LOCALE_ID, useValue: 'he' },
    { provide: MAT_DATE_LOCALE, useValue: 'he-IL' },
    provideNativeDateAdapter(),
    { provide: MatPaginatorIntl, useClass: HebrewPaginatorIntl },
    // <mat-icon> uses the self-hosted rounded Material Symbols
    provideAppInitializer(() => {
      inject(MatIconRegistry).setDefaultFontSetClass('material-symbols-rounded');
    }),
  ],
};
