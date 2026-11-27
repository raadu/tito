import { Routes } from '@angular/router';
import { requireEmployee } from './core/attendance-session';

// URL -> page. loadComponent = lazy loading: each page's code downloads only when visited.
export const routes: Routes = [
  { path: '', title: 'TITO', loadComponent: () => import('./features/home/home').then((m) => m.Home) },
  {
    path: 'checkin',
    title: 'TITO · Check in',
    canActivate: [requireEmployee],
    loadComponent: () => import('./features/checkin/checkin').then((m) => m.Checkin),
  },
  { path: '**', redirectTo: '' }, // unknown URL -> home
];
