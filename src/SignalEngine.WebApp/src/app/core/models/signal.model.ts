/**
 * Signal model representing a triggered alert from rule evaluation.
 */
export interface Signal {
  id: number;
  tenantId: number;
  ruleId: number;
  assetId: number;
  statusCode: string;
  title: string;
  description?: string;
  triggerValue: number;
  thresholdValue: number;
  triggeredAt: string;
  resolution?: SignalResolution;
}

export interface SignalResolution {
  id: number;
  resolvedAt: string;
  resolvedByUserId: number;
  notes?: string;
}
