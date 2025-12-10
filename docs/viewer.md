# Viewer BFF kiến trúc mới

Kiến trúc viewer được tách rời khỏi Modules/ECM để giữ ranh giới domain rõ ràng:

- **ECM (Modules)** chỉ cung cấp metadata, ACL và stream file gốc qua `GET /api/ecm/files/preview/{versionId}`.
- **AppGateway.Api** đóng vai trò BFF cho viewer: nhận request `/api/viewer/**` từ UI, lấy file từ ECM và gắn các Syncfusion server libs (v31.x) để convert/render. UI Next.js và viewer API cùng host trong AppGateway nên không cần proxy/CORS.
- **Syncfusion** là concern UI: PdfViewer server-backed, DocumentEditor (SFDT) và Spreadsheet (JSON workbook). Việc đặt chúng ở BFF giúp UI không phải tải file gốc.

## Luồng tổng quan

1. UI gọi `GET /api/viewer/{versionId}` để lấy descriptor (viewerType, URL preview/download/thumbnail, serviceUrl cho PDF, sfdt/json cho Word/Excel).
2. Khi cần xem:
   - PDF: FE trỏ `serviceUrl` tới `/api/viewer/pdf/{versionId}` (PdfRenderer) và gửi `documentPath` = `versionId`.
   - Word: FE gọi `/api/viewer/word/{versionId}` để lấy SFDT (được cache tại BFF). Nếu chưa có cache, AppGateway tải DOCX từ ECM → convert SFDT → lưu cache.
   - Excel: FE gọi `/api/viewer/excel/{versionId}` để lấy JSON workbook (cache tương tự Word).
3. AppGateway vẫn forward token đến ECM, nhưng UI chỉ thao tác với viewer endpoints của BFF.

## Endpoint BFF

- `GET /api/viewer/{versionId}`: trả về descriptor (viewerType, preview/download/thumbnail, `pdfServiceUrl`, `sfdtUrl`, `excelJsonUrl`).
- `POST /api/viewer/pdf/{versionId}/load|render|render-text|render-thumbnails|bookmarks|annotations|print|download|unload`: PdfRenderer endpoints theo Syncfusion server-backed.
- `GET /api/viewer/word/{versionId}`: trả SFDT đã convert (application/json).
- `GET /api/viewer/excel/{versionId}`: trả JSON Spreadsheet đã convert.

## Ưu điểm

- Ranh giới rõ: Modules xử lý domain/storage, AppGateway xử lý viewer/UI concerns.
- Giảm CORS/proxy: UI và viewer API cùng host tại AppGateway.
- Tương thích ngược: preview/download vẫn giữ nguyên, viewer URLs mới được bổ sung trong descriptor.
