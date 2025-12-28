import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { Asset } from '../models';

/**
 * Service for fetching assets from the SystemApi.
 * Assets are tenant-scoped via the API's authorization.
 */
@Injectable({
  providedIn: 'root'
})
export class AssetService extends ApiService {
  
  /**
   * Gets all assets for the current tenant.
   */
  getAssets(): Observable<Asset[]> {
    return this.get<Asset[]>('/assets');
  }

  /**
   * Gets a specific asset by ID.
   */
  getAsset(id: number): Observable<Asset> {
    return this.get<Asset>(`/assets/${id}`);
  }
}
