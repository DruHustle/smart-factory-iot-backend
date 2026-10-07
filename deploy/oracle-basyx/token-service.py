import hmac
import json
import os
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import parse_qs


CLIENT_ID = os.environ["OAUTH_CLIENT_ID"]
CLIENT_SECRET = os.environ["OAUTH_CLIENT_SECRET"]
GATEWAY_TOKEN = os.environ["BASYX_GATEWAY_TOKEN"]


class TokenHandler(BaseHTTPRequestHandler):
    server_version = "SmartFactoryTokenService/1.0"

    def do_POST(self):
        if self.path != "/oauth/token":
            self.send_error(404)
            return

        try:
            length = min(int(self.headers.get("Content-Length", "0")), 8192)
        except ValueError:
            self.send_error(400)
            return

        form = parse_qs(self.rfile.read(length).decode("utf-8"), keep_blank_values=True)
        valid = (
            hmac.compare_digest(form.get("grant_type", [""])[0], "client_credentials")
            and hmac.compare_digest(form.get("client_id", [""])[0], CLIENT_ID)
            and hmac.compare_digest(form.get("client_secret", [""])[0], CLIENT_SECRET)
        )
        if not valid:
            self._json(401, {"error": "invalid_client"})
            return

        self._json(200, {
            "access_token": GATEWAY_TOKEN,
            "token_type": "Bearer",
            "expires_in": 300,
        })

    def do_GET(self):
        self.send_error(405)

    def log_message(self, format, *args):
        return

    def _json(self, status, payload):
        body = json.dumps(payload, separators=(",", ":")).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json")
        self.send_header("Cache-Control", "no-store")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)


if __name__ == "__main__":
    ThreadingHTTPServer(("0.0.0.0", 9000), TokenHandler).serve_forever()
