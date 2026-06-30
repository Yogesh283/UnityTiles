import asyncio
from contextlib import asynccontextmanager
from pathlib import Path

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import FileResponse
from fastapi.staticfiles import StaticFiles

from api.v1.router import router as api_router
from config import get_settings
from middleware.rate_limit import RateLimitMiddleware
from payments.google_play import get_google_play_verifier
from websocket.tournament_ws import router as ws_router

settings = get_settings()
legal_dir = Path(__file__).resolve().parent / "static" / "legal"


@asynccontextmanager
async def lifespan(app: FastAPI):
    from tournament.room_scheduler import room_scheduler_loop

    task = asyncio.create_task(room_scheduler_loop())
    yield
    task.cancel()


app = FastAPI(
    title="Match IQ API",
    description="Production backend for Match IQ tile matching tournaments",
    version="2.0.0",
    lifespan=lifespan,
)

app.add_middleware(RateLimitMiddleware)
app.add_middleware(
    CORSMiddleware,
    allow_origins=settings.cors_origin_list,
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

app.include_router(api_router)
app.include_router(ws_router)

if legal_dir.is_dir():
    @app.get("/legal/terms.html", include_in_schema=False)
    def legal_terms():
        return FileResponse(legal_dir / "terms.html", media_type="text/html")

    @app.get("/legal/privacy.html", include_in_schema=False)
    def legal_privacy():
        return FileResponse(legal_dir / "privacy.html", media_type="text/html")

    app.mount("/legal", StaticFiles(directory=str(legal_dir), html=True), name="legal")


@app.on_event("startup")
def log_google_play_status() -> None:
    verifier = get_google_play_verifier()
    if verifier.is_configured:
        print("[Match IQ] Google Play billing: configured")
    else:
        print(f"[Match IQ] Google Play billing: NOT configured — {verifier.config_error}")


@app.get("/health")
def health():
    verifier = get_google_play_verifier()
    return {
        "status": "ok",
        "service": "match-iq-api",
        "version": "2.0.0",
        "google_play_billing": verifier.is_configured,
        "billing_active": verifier.is_configured,
        "legal_pages": {
            "privacy": "/legal/privacy.html",
            "terms": "/legal/terms.html",
            "ready": legal_dir.is_dir(),
        },
    }


if __name__ == "__main__":
    import uvicorn

    uvicorn.run("main:app", host=settings.api_host, port=settings.api_port, reload=True)
