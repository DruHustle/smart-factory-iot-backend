-- Preserve source sensor identity and read health through the durable telemetry
-- outbox. Both fields are optional for non-sensor and legacy publishers.
ALTER TABLE "TelemetryRecords" ADD COLUMN IF NOT EXISTS "SensorType" varchar(64) NULL;
ALTER TABLE "TelemetryRecords" ADD COLUMN IF NOT EXISTS "SensorStatus" varchar(16) NULL;

DO $$
BEGIN
  ALTER TABLE "TelemetryRecords"
    ADD CONSTRAINT "CK_TelemetryRecords_SensorType_Length"
    CHECK ("SensorType" IS NULL OR char_length("SensorType") BETWEEN 1 AND 64);
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

DO $$
BEGIN
  ALTER TABLE "TelemetryRecords"
    ADD CONSTRAINT "CK_TelemetryRecords_SensorStatus"
    CHECK ("SensorStatus" IS NULL OR "SensorStatus" IN ('ok', 'read_error'));
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;
