import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { Rule } from '../models';

/**
 * Service for fetching and managing rules from the SystemApi.
 * Rules are tenant-scoped via the API's authorization.
 */
@Injectable({
  providedIn: 'root'
})
export class RuleService extends ApiService {
  
  /**
   * Gets all rules for the current tenant.
   * @param activeOnly If true, returns only active rules.
   */
  getRules(activeOnly: boolean = false): Observable<Rule[]> {
    const params = activeOnly ? '?activeOnly=true' : '';
    return this.get<Rule[]>(`/rules${params}`);
  }

  /**
   * Disables a rule by ID.
   */
  disableRule(id: number): Observable<void> {
    return this.put<void>(`/rules/${id}/disable`);
  }
}
