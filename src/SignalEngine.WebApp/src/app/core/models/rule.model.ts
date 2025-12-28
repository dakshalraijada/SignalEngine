/**
 * Rule model representing a threshold-based monitoring rule.
 */
export interface Rule {
  id: number;
  tenantId: number;
  assetId: number;
  name: string;
  description?: string;
  metricName: string;
  operatorCode: string;
  threshold: number;
  severityCode: string;
  evaluationFrequencyCode: string;
  consecutiveBreachesRequired: number;
  isActive: boolean;
  createdAt: string;
}
