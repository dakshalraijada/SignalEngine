import { Component, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { SignalService } from '../../core/services';
import { Signal } from '../../core/models';

/**
 * Displays a read-only list of signals for the current tenant.
 * Sorted by TriggeredAt descending (most recent first).
 */
@Component({
  selector: 'app-signal-list',
  standalone: true,
  imports: [CommonModule, DatePipe],
  templateUrl: './signal-list.component.html',
  styleUrl: './signal-list.component.scss'
})
export class SignalListComponent implements OnInit {
  signals: Signal[] = [];
  loading = true;
  error: string | null = null;

  constructor(private signalService: SignalService) {}

  ngOnInit(): void {
    this.loadSignals();
  }

  loadSignals(): void {
    this.loading = true;
    this.error = null;

    this.signalService.getSignals().subscribe({
      next: (signals) => {
        // Sort by triggeredAt descending
        this.signals = signals.sort((a, b) => 
          new Date(b.triggeredAt).getTime() - new Date(a.triggeredAt).getTime()
        );
        this.loading = false;
      },
      error: (err) => {
        this.error = err.message || 'Failed to load signals';
        this.loading = false;
      }
    });
  }

  /**
   * Formats the operator code to a readable symbol.
   */
  formatOperator(code: string): string {
    const operators: Record<string, string> = {
      'GT': '>',
      'GTE': '≥',
      'LT': '<',
      'LTE': '≤',
      'EQ': '='
    };
    return operators[code?.toUpperCase()] || code;
  }

  /**
   * Returns CSS class for severity badge.
   */
  getSeverityClass(severity: string): string {
    const map: Record<string, string> = {
      'CRITICAL': 'severity-critical',
      'HIGH': 'severity-high',
      'WARNING': 'severity-warning',
      'INFO': 'severity-info'
    };
    return map[severity?.toUpperCase()] || 'severity-default';
  }

  /**
   * Returns CSS class for status badge.
   */
  getStatusClass(statusCode: string): string {
    return statusCode?.toUpperCase() === 'OPEN' ? 'status-open' : 'status-resolved';
  }
}
