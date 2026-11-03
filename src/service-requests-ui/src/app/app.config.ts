import { registerLocaleData } from '@angular/common';
import localeHe from '@angular/common/locales/he';
import {
  ApplicationConfig,
  inject,
  LOCALE_ID,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { MAT_DATE_LOCALE } from '@angular/material/core';
import { MatIconRegistry } from '@angular/material/icon';
import { MatPaginatorIntl } from '@angular/material/paginator';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { HebrewPaginatorIntl } from './core/i18n/paginator-intl.he';

registerLocaleData(localeHe);

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    // Dates and numbers in pipes, and the date picker, follow Israeli conventions
    { provide: LOCALE_ID, useValue: 'he' },
    { provide: MAT_DATE_LOCALE, useValue: 'he-IL' },
    { provide: MatPaginatorIntl, useClass: HebrewPaginatorIntl },
    // <mat-icon> uses the self-hosted rounded Material Symbols
    provideAppInitializer(() => {
      inject(MatIconRegistry).setDefaultFontSetClass('material-symbols-rounded');
    }),
  ],
};
