import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AssetService } from '../../core/services';
import { Asset } from '../../core/models';

@Component({
  selector: 'app-assets-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './assets-list.component.html',
  styleUrl: './assets-list.component.scss'
})
export class AssetsListComponent implements OnInit {
  private assetService = inject(AssetService);

  assets: Asset[] = [];
  loading = true;
  error: string | null = null;

  ngOnInit(): void {
    this.loadAssets();
  }

  loadAssets(): void {
    this.loading = true;
    this.error = null;

    this.assetService.getAssets().subscribe({
      next: (assets) => {
        this.assets = assets.sort((a, b) => a.identifier.localeCompare(b.identifier));
        this.loading = false;
      },
      error: (err) => {
        this.error = err.message || 'Failed to load assets';
        this.loading = false;
      }
    });
  }

  getStatusText(isActive: boolean): string {
    return isActive ? 'Active' : 'Inactive';
  }

  getStatusClass(isActive: boolean): string {
    return isActive ? 'status-active' : 'status-disabled';
  }
}
