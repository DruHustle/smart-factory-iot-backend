#!/bin/sh
set -eu

# This legacy multi-service image is retained for local diagnostics only. Keep
# the environment-controlled selector strictly allowlisted and replace the
# shell with the service process so signals and exit codes propagate correctly.
case "${SERVICE_NAME:-}" in
  DeviceService|IdentityService|NotificationService|AnalyticsService|TelemetryService)
    ;;
  *)
    echo "Unsupported SERVICE_NAME" >&2
    exit 64
    ;;
esac

exec dotnet "/app/${SERVICE_NAME}/${SERVICE_NAME}.dll"
