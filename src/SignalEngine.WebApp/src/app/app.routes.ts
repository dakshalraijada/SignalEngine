import { Routes } from '@angular/router';
import { authGuard } from './auth/auth.guard';
import { HomeComponent } from './home/home.component';
import { CallbackComponent } from './auth/callback.component';

export const routes: Routes = [
  { path: '', component: HomeComponent, canActivate: [authGuard] },
  { path: 'callback', component: CallbackComponent },
  { path: '**', redirectTo: '' }
];
