from pathlib import Path

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import FileResponse
from fastapi.staticfiles import StaticFiles

from api.v1.router import router as api_router
from config import get_settings
from middleware.rate_limit import RateLimitMiddleware
from websocket.tournament_ws import router as ws_router

settings = get_settings()
legal_dir = Path(__file__).resolve().parent / "static" / "legal"

app = FastAPI(
    title="Match IQ API",
    description="Production backend for Match IQ tile matching tournaments",
    version="2.0.0",
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


@app.get("/health")
def health():
    return {
        "status": "ok",
        "service": "match-iq-api",
        "version": "2.0.0",
        "legal_pages": {
            "privacy": "/legal/privacy.html",
            "terms": "/legal/terms.html",
            "ready": legal_dir.is_dir(),
        },
    }


if __name__ == "__main__":
    import uvicorn

    uvicorn.run("main:app", host=settings.api_host, port=settings.api_port, reload=True)
