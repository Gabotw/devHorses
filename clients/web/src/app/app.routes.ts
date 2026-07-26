import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./pages/login/login').then((m) => m.Login),
  },
  {
    path: '',
    loadComponent: () => import('./layout/shell/shell').then((m) => m.Shell),
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'checkin' },
      {
        path: 'checkin',
        loadComponent: () => import('./pages/checkin/checkin').then((m) => m.CheckInPage),
      },
      {
        path: 'members',
        loadComponent: () => import('./pages/members/members-list').then((m) => m.MembersList),
      },
      {
        path: 'plans',
        loadComponent: () => import('./pages/plans/plans-list').then((m) => m.PlansList),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
