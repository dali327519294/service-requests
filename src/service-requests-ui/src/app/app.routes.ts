import { Routes } from '@angular/router';
import { labels, pageTitle } from './core/i18n/labels.he';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'requests' },
  {
    path: 'requests',
    title: pageTitle(labels.requestsTitle),
    loadComponent: () =>
      import('./features/requests/list/requests-list-page').then((m) => m.RequestsListPage),
  },
  {
    path: 'requests/:id',
    title: pageTitle(labels.requestTitle),
    loadComponent: () =>
      import('./features/requests/details/request-details-page').then((m) => m.RequestDetailsPage),
  },
  { path: '**', redirectTo: 'requests' },
];
