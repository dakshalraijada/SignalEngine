import { Routes } from '@angular/router';
import { authGuard } from './auth/auth.guard';
import { CallbackComponent } from './auth/callback.component';
import { ShellComponent } from './layout/shell/shell.component';
import { SignalListComponent } from './features/signals/signal-list.component';
import { RulesListComponent } from './features/rules/rules-list.component';
import { AssetsListComponent } from './features/assets/assets-list.component';

export const routes: Routes = [
  { 
    path: '', 
    component: ShellComponent, 
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'signals', pathMatch: 'full' },
      { path: 'signals', component: SignalListComponent },
      { path: 'rules', component: RulesListComponent },
      { path: 'assets', component: AssetsListComponent }
    ]
  },
  { path: 'callback', component: CallbackComponent },
  { path: '**', redirectTo: '' }
];
