from fastapi import FastAPI

app = FastAPI(title="ECM OCR Service", version="0.1.0")


@app.get("/health")
async def health() -> dict[str, str]:
    """Return a simple readiness status."""
    return {"status": "ok"}


@app.post("/api/ocr/extract")
async def extract_text() -> dict[str, list[str]]:
    """Placeholder OCR extraction endpoint."""
    # TODO: Integrate Tesseract or PaddleOCR pipeline.
    return {"tokens": []}
