import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';

@Component({
  selector: 'app-callback',
  standalone: true,
  templateUrl: './callback.component.html'
})
export class CallbackComponent implements OnInit {
  constructor(
    private oidcSecurityService: OidcSecurityService,
    private router: Router
  ) {}

  ngOnInit(): void {
    // checkAuth() will process the callback URL and complete the authentication
    this.oidcSecurityService.checkAuth().subscribe({
      next: ({ isAuthenticated }) => {
        console.log('Callback - isAuthenticated:', isAuthenticated);
        // Only navigate after auth check completes
        // Small delay to ensure state is fully updated
        setTimeout(() => {
          this.router.navigate(['/']);
        }, 100);
      },
      error: (err) => {
        console.error('Callback error:', err);
        this.router.navigate(['/']);
      }
    });
  }
}
