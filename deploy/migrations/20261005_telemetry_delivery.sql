-- Run after telemetry-schema.sql and before deploying the telemetry worker.
-- Existing records are retained; only new normalized messages enter the outbox.
ALTER TABLE "TelemetryRecords" ADD COLUMN IF NOT EXISTS "AssetSignalsJson" text NULL;
ALTER TABLE "TelemetryRecords" ADD COLUMN IF NOT EXISTS "IngestionId" text NULL;
ALTER TABLE "TelemetryRecords" ADD COLUMN IF NOT EXISTS "EventPublishedAt" timestamptz NULL;
ALTER TABLE "TelemetryRecords" ADD COLUMN IF NOT EXISTS "DashboardForwardedAt" timestamptz NULL;
ALTER TABLE "TelemetryRecords" ADD COLUMN IF NOT EXISTS "NextDeliveryAttemptAt" timestamptz NULL;
ALTER TABLE "TelemetryRecords" ADD COLUMN IF NOT EXISTS "DeliveryAttempts" integer NOT NULL DEFAULT 0;
CREATE UNIQUE INDEX IF NOT EXISTS "IX_TelemetryRecords_IngestionId" ON "TelemetryRecords" ("IngestionId");
CREATE INDEX IF NOT EXISTS "IX_TelemetryRecords_PendingDelivery" ON "TelemetryRecords" ("NextDeliveryAttemptAt", "Id")
  WHERE "IngestionId" IS NOT NULL AND ("EventPublishedAt" IS NULL OR "DashboardForwardedAt" IS NULL);

CREATE INDEX IF NOT EXISTS "IX_TelemetryRecords_PendingDelivery"
  ON "TelemetryRecords" ("NextDeliveryAttemptAt", "Id")
  WHERE "IngestionId" IS NOT NULL AND ("EventPublishedAt" IS NULL OR "DashboardForwardedAt" IS NULL);
