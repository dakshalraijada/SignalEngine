import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { Signal } from '../models';

/**
 * Service for fetching signals from the SystemApi.
 * Signals are tenant-scoped via the API's authorization.
 */
@Injectable({
  providedIn: 'root'
})
export class SignalService extends ApiService {
  
  /**
   * Gets all signals for the current tenant.
   * @param openOnly If true, returns only unresolved signals.
   */
  getSignals(openOnly: boolean = false): Observable<Signal[]> {
    const params = openOnly ? '?openOnly=true' : '';
    return this.get<Signal[]>(`/signals${params}`);
  }

  /**
   * Gets a specific signal by ID.
   */
  getSignal(id: number): Observable<Signal> {
    return this.get<Signal>(`/signals/${id}`);
  }
}
