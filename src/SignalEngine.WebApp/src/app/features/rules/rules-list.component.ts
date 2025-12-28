import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RuleService } from '../../core/services';
import { Rule } from '../../core/models';

@Component({
  selector: 'app-rules-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './rules-list.component.html',
  styleUrl: './rules-list.component.scss'
})
export class RulesListComponent implements OnInit {
  private ruleService = inject(RuleService);

  rules: Rule[] = [];
  loading = true;
  error: string | null = null;
  disablingRuleId: number | null = null;

  ngOnInit(): void {
    this.loadRules();
  }

  loadRules(): void {
    this.loading = true;
    this.error = null;

    this.ruleService.getRules().subscribe({
      next: (rules) => {
        this.rules = rules.sort((a, b) => a.name.localeCompare(b.name));
        this.loading = false;
      },
      error: (err) => {
        this.error = err.message || 'Failed to load rules';
        this.loading = false;
      }
    });
  }

  disableRule(rule: Rule): void {
    if (this.disablingRuleId || !rule.isActive) {
      return;
    }

    this.disablingRuleId = rule.id;

    this.ruleService.disableRule(rule.id).subscribe({
      next: () => {
        // Reload to get updated status
        this.disablingRuleId = null;
        this.loadRules();
      },
      error: (err) => {
        this.error = err.message || 'Failed to disable rule';
        this.disablingRuleId = null;
      }
    });
  }

  formatCondition(rule: Rule): string {
    return `${rule.metricName} ${this.formatOperator(rule.operatorCode)} ${rule.threshold}`;
  }

  formatOperator(code: string): string {
    switch (code) {
      case 'GT': return '>';
      case 'GTE': return '≥';
      case 'LT': return '<';
      case 'LTE': return '≤';
      case 'EQ': return '=';
      case 'NEQ': return '≠';
      default: return code;
    }
  }

  getStatusText(isActive: boolean): string {
    return isActive ? 'Active' : 'Disabled';
  }

  getStatusClass(isActive: boolean): string {
    return isActive ? 'status-active' : 'status-disabled';
  }
}
