-- Apply once before deploying telemetry builds that persist GatewayId.
-- Idempotent so it can also repair a partially applied rollout.
ALTER TABLE "TelemetryRecords"
  ADD COLUMN IF NOT EXISTS "GatewayId" text NULL;
