#!/usr/bin/env python3
"""Run DeviceService create/update/conflict/delete checks against local BaSyx API 3.2."""
from __future__ import annotations
import base64, json, os, socket, subprocess, sys, time, urllib.error, urllib.parse, urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
REPO = os.getenv("AAS_REPOSITORY_URL", "http://127.0.0.1:8081")
SUBMODEL_REPO = os.getenv("AAS_SUBMODEL_REPOSITORY_URL", "http://127.0.0.1:8083")
REGISTRY = os.getenv("AAS_REGISTRY_URL", "http://127.0.0.1:8082")
BASE = os.getenv("DEVICE_SERVICE_TEST_URL", "http://127.0.0.1:5198")
TOKEN = os.getenv("AAS_PROVISIONING_TOKEN", "local-live-test-token-that-is-long-enough-32")

def request(url: str, method: str = "GET", payload: object | None = None, token: str | None = None):
    body = None if payload is None else json.dumps(payload).encode()
    headers = {"Accept": "application/json"}
    if body is not None: headers["Content-Type"] = "application/json"
    if token: headers["X-AAS-Provisioning-Token"] = token
    req = urllib.request.Request(url, data=body, headers=headers, method=method)
    try: response = urllib.request.urlopen(req, timeout=8)
    except urllib.error.HTTPError as error: response = error
    raw = response.read().decode("utf-8", "replace")
    content_type = response.headers.get("Content-Type", "")
    return response.status, content_type, json.loads(raw) if raw and "json" in content_type else None

def require(condition: bool, message: str):
    if not condition: raise RuntimeError(message)

def encoded(value: str) -> str:
    return base64.urlsafe_b64encode(value.encode()).decode().rstrip("=")

def main() -> int:
    for root, expected in ((REPO, "AssetAdministrationShellRepositoryServiceSpecification"), (SUBMODEL_REPO, "SubmodelRepositoryServiceSpecification"), (REGISTRY, "AssetAdministrationShellRegistryServiceSpecification")):
        status, media, data = request(root.rstrip("/") + "/description")
        require(status == 200 and media.startswith("application/json"), f"{root}/description returned unexpected status/media type")
        profiles = data.get("profiles", []) if isinstance(data, dict) else []
        require(any(expected in item and "/3/2/" in item for item in profiles), f"{root} does not advertise the expected API 3.2 profile")

    address = urllib.parse.urlparse(BASE)
    with socket.socket() as sock: sock.bind((address.hostname or "127.0.0.1", address.port or 80))
    password = os.getenv("REDIS_PASSWORD", "")
    redis_url = os.getenv("DEVICE_SERVICE_REDIS_URL") or os.getenv("REDIS_URL")
    if not redis_url:
        auth = ":" + urllib.parse.quote(password, safe="") + "@" if password else ""
        redis_url = f"redis://{auth}127.0.0.1:6379"
    env = os.environ.copy()
    env.update({
        "ASPNETCORE_ENVIRONMENT": "Development", "ASPNETCORE_URLS": BASE,
        "JWT_SECRET": os.getenv("JWT_SECRET", "local-test-jwt-secret-with-more-than-32-bytes"),
        "AAS_PROVISIONING_TOKEN": TOKEN, "REDIS_URL": redis_url,
        "AAS_REPOSITORY_URL": REPO, "AAS_SUBMODEL_REPOSITORY_URL": SUBMODEL_REPO,
        "AAS_CONCEPT_DESCRIPTION_REPOSITORY_URL": os.getenv("AAS_CONCEPT_DESCRIPTION_REPOSITORY_URL", "http://127.0.0.1:8085"),
        "AAS_REGISTRY_URL": REGISTRY, "AAS_SUBMODEL_REGISTRY_URL": os.getenv("AAS_SUBMODEL_REGISTRY_URL", "http://127.0.0.1:8084"),
        "AASX_FILE_SERVER_URL": os.getenv("AASX_FILE_SERVER_URL", "http://127.0.0.1:8086"),
        "AAS_ALLOW_UNAUTHENTICATED_LOCAL": "true", "AAS_REPOSITORY_REGISTRY_INTEGRATION": "true",
    })
    log = open("/tmp/smart-factory-device-service-live.log", "wb")
    process = subprocess.Popen(["dotnet", "run", "--no-launch-profile", "--project", str(ROOT / "src/Services/DeviceService/DeviceService.csproj")], cwd=ROOT, env=env, stdout=log, stderr=subprocess.STDOUT)
    asset_id = f"urn:smart-factory:live-test:{int(time.time())}"
    try:
        deadline = time.monotonic() + 60
        while time.monotonic() < deadline:
            if process.poll() is not None: raise RuntimeError(f"DeviceService exited; inspect /tmp/smart-factory-device-service-live.log")
            try:
                status, _, _ = request(BASE + "/health/ready")
                if status == 200: break
            except Exception: pass
            time.sleep(1)
        else: raise RuntimeError("DeviceService did not become ready; inspect /tmp/smart-factory-device-service-live.log")

        asset = {
            "assetId": asset_id, "name": "Live Integration Compressor", "assetType": "compressor",
            "manufacturer": "Integration Test Works", "model": "IT-1", "manufacturerStreet": "Test Road 1",
            "manufacturerZipcode": "10115", "manufacturerCityTown": "Berlin", "manufacturerNationalCode": "DE",
            "manufacturerArticleNumber": "IT-ARTICLE-1", "orderCodeOfManufacturer": "IT-ORDER-1", "aasVersion": 1,
        }
        unauth, _, _ = request(BASE + "/api/assets", "POST", asset, "bad-token")
        require(unauth == 401, f"Invalid provisioning token returned HTTP {unauth}, expected 401")
        status, media, created = request(BASE + "/api/assets", "POST", asset, TOKEN)
        require(status == 200 and media.startswith("application/json"), f"DeviceService create returned HTTP {status} / {media}")
        require(isinstance(created, dict) and created.get("repositoryRegistered") is True, "DeviceService did not confirm repository registration")
        duplicate, _, _ = request(BASE + "/api/assets", "POST", asset, TOKEN)
        require(duplicate == 409, f"Duplicate asset creation returned HTTP {duplicate}, expected 409")
        shell_path = REPO.rstrip("/") + "/shells/" + encoded(asset_id)
        status, media, shell = request(shell_path)
        require(status == 200 and media.startswith("application/json"), f"AAS Repository GET returned HTTP {status} / {media}")
        require(shell.get("administration", {}).get("version") == "1", "Created AAS shell revision was not preserved")
        submodel_id = asset_id + "/submodels/Nameplate"
        status, _, submodel = request(SUBMODEL_REPO.rstrip("/") + "/submodels/" + encoded(submodel_id))
        require(status == 200 and submodel.get("id") == submodel_id, "Created Nameplate was not readable from the Submodel Repository")
        status, media, descriptors = request(REGISTRY.rstrip("/") + "/shell-descriptors")
        require(status == 200 and media.startswith("application/json"), f"AAS Registry GET returned HTTP {status} / {media}")
        rows = descriptors.get("result", []) if isinstance(descriptors, dict) else []
        require(any(row.get("id") == asset_id for row in rows), "Repository did not auto-register the AAS shell descriptor")

        updated = dict(asset, aasVersion=2, expectedAasVersion=1, manufacturerStreet="Updated Test Road 2")
        status, _, _ = request(BASE + "/api/assets", "PUT", updated, TOKEN)
        require(status == 200, f"DeviceService revision update returned HTTP {status}")
        status, _, shell = request(shell_path)
        require(status == 200 and shell.get("administration", {}).get("version") == "2", "AAS Repository did not retain revision 2")
        stale, _, _ = request(BASE + "/api/assets", "PUT", updated, TOKEN)
        require(stale == 409, f"Stale editor received HTTP {stale}, expected 409")
        status, _, _ = request(BASE + "/api/assets", "DELETE", asset, TOKEN)
        require(status == 200, f"DeviceService cleanup returned HTTP {status}")
        print("PASS: API 3.2 profiles, service-token auth, form create, repository/submodel reads, automatic registry registration, duplicate rejection, revision update/conflict, cleanup")
        return 0
    finally:
        process.terminate()
        try: process.wait(timeout=10)
        except subprocess.TimeoutExpired: process.kill(); process.wait(timeout=3)
        log.close()

if __name__ == "__main__":
    try: raise SystemExit(main())
    except Exception as error:
        print(f"FAIL: {error}", file=sys.stderr)
        raise SystemExit(1)
