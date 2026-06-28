import time

from starlette.middleware.base import BaseHTTPMiddleware
from starlette.requests import Request
from starlette.responses import JSONResponse

from core.redis_client import rate_limit_check


class RateLimitMiddleware(BaseHTTPMiddleware):
    async def dispatch(self, request: Request, call_next):
        if request.url.path in {"/health", "/docs", "/openapi.json", "/redoc", "/legal/terms.html", "/legal/privacy.html"}:
            return await call_next(request)

        client_ip = request.headers.get("x-forwarded-for", request.client.host if request.client else "unknown")
        client_ip = client_ip.split(",")[0].strip()
        bucket = f"rl:{client_ip}:{int(time.time() // 60)}"
        if not rate_limit_check(bucket, limit=240, window_seconds=60):
            return JSONResponse(status_code=429, content={"detail": "Rate limit exceeded"})

        return await call_next(request)
