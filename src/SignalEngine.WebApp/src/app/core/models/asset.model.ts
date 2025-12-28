/**
 * Asset model representing a monitored resource (e.g., crypto pair, server).
 */
export interface Asset {
  id: number;
  tenantId: number;
  name: string;
  identifier: string;
  assetTypeId: number;
  dataSourceId: number;
  description?: string;
  metadata?: string;
  isActive: boolean;
  createdAt: string;
}
