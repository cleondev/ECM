# Naming.md – Quy tắc đặt tên theo Clean Architecture

Mục tiêu: Giữ kiến trúc rõ ràng, tránh trùng tên và “chảy máu tầng” giữa Domain / Application / Infrastructure / API.

---

## 1. Domain Layer – Luật nghiệp vụ thuần

- **Entity / AggregateRoot**: danh từ miền, không hậu tố công nghệ  
  - `Document`, `Account`, `LoanApplication`
- **Value Object**: tên giá trị nghiệp vụ, ID dùng hậu tố `Id`  
  - `DocumentId`, `Money`, `EmailAddress`
- **Domain Service**: hậu tố `Service`, hành vi đa-aggregate  
  - `MoneyTransferService`, `ScoringService`
- **Policy / Rule**: hậu tố `Policy` / `Rule`  
  - `OverdraftPolicy`, `RetentionRule`
- **Domain Event**: hậu tố `DomainEvent`, thì quá khứ  
  - `DocumentIndexedDomainEvent`
- **Exception**: hậu tố `DomainException` hoặc cụ thể  
  - `InsufficientBalanceException`
- **Enum / Smart Enum**: hậu tố `Status`, `Type`, `Kind`  
  - `DocumentStatus`, `LoanType`

❌ Không EF attribute, không IO, không `IConfiguration`.

---

## 2. Application Layer – Use Case & CQRS

- **Command / Query**: hậu tố `Command` / `Query`  
  - `UploadDocumentCommand`, `GetDocumentByIdQuery`
- **Handler**: hậu tố `Handler`  
  - `UploadDocumentHandler`
- **Validator**: hậu tố `Validator`  
  - `UploadDocumentCommandValidator`
- **Result / Dto**: hậu tố `Result` / `Dto`  
  - `UploadDocumentResult`, `DocumentDto`
- **Port / Interface**: hậu tố `Repository`, `Gateway`, `Publisher`, `Storage`  
  - `IDocumentRepository`, `IBlobStorage`, `IOcrEventPublisher`
- **Pipeline Behavior**: hậu tố `Behavior`  
  - `ValidationBehavior`, `TransactionBehavior`

❌ Không EF/ORM/HTTP cụ thể, không `IConfiguration`.  
✅ Vertical Slice mỗi use case 1 thư mục: Command, Handler, Validator, Result, Mapping.

---

## 3. Infrastructure Layer – Adapter & Implementation

- **Repository triển khai**: lặp tên port + công nghệ  
  - `EfDocumentRepository`, `DapperDocumentRepository`
- **Client adapter**: mang tên hệ thống ngoài  
  - `KafkaOcrEventPublisher`, `MinioBlobStorage`
- **DbContext / Config**:  
  - `EcmDbContext`, `DocumentConfiguration`

✅ Được dùng công nghệ (EF, Kafka, MinIO…), migrations ở đây.  
❌ Không chứa nghiệp vụ.

---

## 4. API Layer – Presentation

- **Controller**: hậu tố `Controller`, danh từ số nhiều  
  - `DocumentsController`
- **Request / Response model**: hậu tố `Request` / `Response`  
  - `UploadDocumentRequest`, `UploadDocumentResponse`

✅ Không nghiệp vụ, chỉ map request → Command/Query → Result → response.

---

## 5. Messaging & Events

- **Domain Event (nội bộ)**: `…DomainEvent`  
  - `OcrCompletedDomainEvent`
- **Integration Event (liên hệ thống)**: `…IntegrationEvent`  
  - `OcrCompletedIntegrationEvent`
- **Topic**: `context.aggregate.action`  
  - `ecm.document.uploaded`, `ecm.ocr.completed`

---

## 6. Mapping & Model chuyển đổi

- Domain ↔ Application: `Mapping.cs` trong slice  
  - `ToResult()`, `ToDto()`
- Application ↔ API: map tại Controller hoặc mapper riêng
- Persistence Model (nếu cần): hậu tố `Record` / `EntityModel`  
  - `DocumentRecord` ↔ `Document`

---

## 7. Quy ước nhanh (theo vai trò)

| Vai trò                     | Mẫu tên ví dụ                              |
|---------------------------|--------------------------------------------|
| Entity                   | `Document`, `Account`                      |
| Value Object / ID        | `DocumentId`, `Money`, `EmailAddress`     |
| Domain Service           | `MoneyTransferService`                     |
| Policy / Rule            | `OverdraftPolicy`, `RetentionRule`        |
| Domain Event             | `DocumentIndexedDomainEvent`              |
| Command / Query          | `UploadDocumentCommand`, `GetDocumentByIdQuery` |
| Handler                  | `UploadDocumentHandler`                   |
| Result / DTO             | `UploadDocumentResult`, `DocumentDto`     |
| Port (interface)         | `IDocumentRepository`, `IBlobStorage`     |
| Adapter (impl)           | `EfDocumentRepository`, `KafkaOcrEventPublisher` |
| Controller              | `DocumentsController`                      |
| Request / Response       | `UploadDocumentRequest`, `UploadDocumentResponse` |
| Integration Event        | `OcrCompletedIntegrationEvent`            |

---

## 8. Check nhanh khi review

- [ ] Tên phản ánh **nghiệp vụ**, không công nghệ ở Domain/Application.  
- [ ] Command/Query/Handler/Result đặt hậu tố đúng.  
- [ ] Port ở Application, Adapter ở Infrastructure.  
- [ ] API không lộ domain entity.  
- [ ] Domain Event ≠ Integration Event.  
- [ ] Không có `IConfiguration`, `DbContext`, `HttpClient` trong Domain/Application.

---
