-- Analytics and notifications now read the dashboard database directly. No
-- runtime consumes telemetry AMQP events, so the durable outbox tracks only
-- delivery to the authenticated dashboard ingestion route. Retain the legacy
-- EventPublishedAt column to avoid destructive history loss during rollout.
DROP INDEX IF EXISTS "IX_TelemetryRecords_PendingDelivery";
CREATE INDEX "IX_TelemetryRecords_PendingDelivery"
  ON "TelemetryRecords" ("NextDeliveryAttemptAt", "Id")
  WHERE "IngestionId" IS NOT NULL AND "DashboardForwardedAt" IS NULL;
