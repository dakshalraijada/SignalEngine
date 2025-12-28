import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';

/**
 * Main layout shell component providing navigation and user context.
 * Displays top navbar with user info and left sidebar navigation.
 */
@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss'
})
export class ShellComponent implements OnInit {
  userEmail: string = '';
  userName: string = '';

  constructor(private oidcSecurityService: OidcSecurityService) {}

  ngOnInit(): void {
    this.oidcSecurityService.userData$.subscribe(({ userData }) => {
      if (userData) {
        this.userEmail = userData.email || '';
        this.userName = userData.name || userData.preferred_username || '';
      }
    });
  }

  logout(): void {
    this.oidcSecurityService.logoff().subscribe();
  }
}
