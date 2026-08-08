"""End-to-end API check for the WXO referral endpoints.

Registers a sponsor, registers a downline using the sponsor's code, then reads
the sponsor's referral profile back through the HTTP API.
"""

import time

import httpx

BASE = "http://127.0.0.1:8000/api/v1"
stamp = int(time.time())


def register(email: str, name: str, referral_code: str | None = None) -> dict:
    payload = {"email": email, "password": "temple123", "display_name": name}
    if referral_code:
        payload["referral_code"] = referral_code
    res = httpx.post(f"{BASE}/auth/register", json=payload, timeout=30)
    res.raise_for_status()
    return res.json()


def auth_get(path: str, token: str) -> dict:
    res = httpx.get(f"{BASE}{path}", headers={"Authorization": f"Bearer {token}"}, timeout=30)
    res.raise_for_status()
    return res.json()


sponsor = register(f"sponsor.{stamp}@matchiq.fun", "Sponsor One")
print("sponsor user_id:", sponsor["user_id"])

sponsor_profile = auth_get("/referral/me", sponsor["access_token"])
code = sponsor_profile["referral_code"]
print("sponsor code:", code, "| link:", sponsor_profile["referral_link"])

downline = register(f"down.{stamp}@matchiq.fun", "Downline One", referral_code=code)
print("downline user_id:", downline["user_id"])

after = auth_get("/referral/me", sponsor["access_token"])
print("team_size:", after["team_size"])
print("team_counts:", after["team_counts"])
print("income today/total:", after["income"]["today"], "/", after["income"]["total"])

team = auth_get("/referral/team", sponsor["access_token"])
for row in team["levels"]:
    print(f"  L{row['level']} {row['percent']}% members={row['members']} points={row['points']}")

income = auth_get("/referral/income?period=today", sponsor["access_token"])
print("income endpoint:", income["period"], income["points"])

me = auth_get("/auth/me", downline["access_token"])
print("downline referral_code from /auth/me:", me["referral_code"])
