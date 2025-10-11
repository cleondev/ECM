# CLEAN ARCHITECTURE – DOMAIN SERVICES & APPLICATION VERTICAL SLICE
---

## 0) Phạm vi & nguyên tắc chung

* Domain thuần nghiệp vụ, không phụ thuộc hạ tầng (EF/HTTP/IO).
* Application orchestration (use case), không chứa logic hạ tầng.
* Infrastructure triển khai adapter (EF, Message broker, Object storage…).
* Presentation (Controller/API) chỉ nhận/gửi request/response.

---

## 1) DOMAIN LAYER – TÁCH LOGIC VÀO DOMAIN SERVICES

### 1.1 Khi nào đặt logic trong Entity, khi nào dùng Domain Service

* Đặt trong **Entity** khi:

  * Hành vi gắn chặt bất biến của chính entity (invariants).
  * Chỉ thao tác trên trạng thái của entity đó.
  * Ví dụ: `Account.Deposit(amount)` với kiểm tra `amount > 0`.

* Dùng **Domain Service** khi:

  * Logic nghiệp vụ chạm **nhiều entity/aggregate**.
  * Luật nghiệp vụ phức tạp cần tách để dễ test/tái sử dụng.
  * Cần phối hợp nhiều bước tính toán nhưng vẫn **domain-pure** (không IO).
  * Ví dụ: `MoneyTransferService.Transfer(from, to, amount, policy)`.

* Không đưa IO vào Domain Service (không gọi DB, HTTP, file, message broker). IO do Application/Infrastructure lo.

### 1.2 Phân loại Domain Service thường gặp

* **Coordination Service**: điều phối hành vi giữa nhiều entity.
* **Policy/Rule Service**: đóng gói luật (overdraft, interest calculation…).
* **Calculation/Decision Service**: tính toán điểm, hạn mức, lãi suất…

### 1.3 Cấu trúc thư mục Domain khuyến nghị

```
Domain/
  <BoundedContext>/
    Aggregates/
      <AggregateName>/
        <AggregateName>.cs
        ValueObjects/
          ...
        Policies/
          ...
        Events/
          ...
        Exceptions/
          ...
        Factory/
          ...
    Services/
      <HighLevelDomainService>.cs
  Shared/
    Primitives/
      Entity.cs
      AggregateRoot.cs
      ValueObject.cs
    Events/
      IDomainEvent.cs
    Time/
      IClock.cs
```

Gợi ý: khi một aggregate có > 5–7 file, tách `Policies/`, `Events/`, `Exceptions/` để dễ định vị.

### 1.4 Mẫu Domain Entity (không EF attribute)

```csharp
public sealed class Account : AggregateRoot<AccountId>
{
    private decimal _balance;
    public decimal Balance => _balance;

    private Account() { } // for ORM
    public Account(AccountId id, decimal initial)
    {
        if (initial < 0) throw new ArgumentException("Initial >= 0");
        Id = id;
        _balance = initial;
    }

    public void Deposit(decimal amount, IOverdraftPolicy policy)
    {
        if (amount <= 0) throw new InvalidOperationException("Amount > 0");
        var newBalance = _balance + amount;
        policy.EnsureValid(this, newBalance);
        _balance = newBalance;
        Raise(new MoneyDeposited(Id, amount));
    }

    public void Withdraw(decimal amount, IOverdraftPolicy policy)
    {
        if (amount <= 0) throw new InvalidOperationException("Amount > 0");
        var newBalance = _balance - amount;
        policy.EnsureValid(this, newBalance);
        _balance = newBalance;
        Raise(new MoneyWithdrawn(Id, amount));
    }
}
```

### 1.5 Mẫu Domain Policy & Domain Service

```csharp
public interface IOverdraftPolicy
{
    void EnsureValid(Account account, decimal newBalance);
}

public sealed class NoOverdraftPolicy : IOverdraftPolicy
{
    public void EnsureValid(Account account, decimal newBalance)
    {
        if (newBalance < 0) throw new InsufficientBalanceException();
    }
}

public sealed class MoneyTransferService
{
    // domain-pure: không IO
    public void Transfer(Account from, Account to, decimal amount, IOverdraftPolicy policy)
    {
        if (amount <= 0) throw new InvalidOperationException("Amount > 0");
        from.Withdraw(amount, policy);
        to.Deposit(amount, policy);
    }
}
```

### 1.6 Quy tắc kiểm soát phụ thuộc (Domain)

* Không `IConfiguration`, không EF attribute, không `DbContext`, không HTTP/IO.
* Domain Service chỉ nhận entity/value object/policy/parameters.
* Domain Event chỉ mang dữ liệu nghiệp vụ, không xử lý IO.

---

## 2) APPLICATION LAYER – VERTICAL SLICE + CQRS

### 2.1 Tổ chức thư mục Vertical Slice

```
Application/
  Common/
    Interfaces/               // ports: repositories, external gateways
      IAccountRepository.cs
      INotificationService.cs
      IUnitOfWork.cs
    Behaviors/                // MediatR pipeline
      ValidationBehavior.cs
      LoggingBehavior.cs
      TransactionBehavior.cs
    Models/                   // DTO/result types dùng chung (ít)
  Features/
    Accounts/
      Create/
        CreateAccountCommand.cs
        CreateAccountHandler.cs
        CreateAccountValidator.cs
        CreateAccountResult.cs
        Mapping.cs
      GetById/
        GetAccountByIdQuery.cs
        GetAccountByIdHandler.cs
        GetAccountByIdResult.cs
      Transfer/
        TransferCommand.cs
        TransferHandler.cs
        TransferValidator.cs
        TransferResult.cs
    Transactions/
      Deposit/
        DepositCommand.cs
        DepositHandler.cs
        DepositValidator.cs
        DepositResult.cs
```

Nguyên tắc:

* Mỗi use case là một slice: Command/Query + Handler + Validator + Mapping + Result đặt cùng thư mục.
* Cross-cutting để ở `Common/Behaviors`.
* Port (interface) để ở `Common/Interfaces`.

### 2.2 Mẫu Command/Handler (orchestration, không nghiệp vụ thuần)

```csharp
public sealed record DepositCommand(Guid AccountId, decimal Amount)
  : IRequest<DepositResult>;

public sealed class DepositHandler
  : IRequestHandler<DepositCommand, DepositResult>
{
    private readonly IAccountRepository _repo;
    private readonly IAuditLogger _logger;
    private readonly IOverdraftPolicy _policy; // có thể inject policy mặc định

    public DepositHandler(IAccountRepository repo, IAuditLogger logger, IOverdraftPolicy policy)
    {
        _repo = repo;
        _logger = logger;
        _policy = policy;
    }

    public async Task<DepositResult> Handle(DepositCommand cmd, CancellationToken ct)
    {
        var account = await _repo.GetByIdAsync(new AccountId(cmd.AccountId))
                     ?? throw new NotFoundException("Account");

        account.Deposit(cmd.Amount, _policy);        // gọi domain

        await _repo.SaveAsync(account, ct);          // IO do repo lo
        await _logger.LogAsync($"Deposit {cmd.Amount} to {cmd.AccountId}", ct);

        return new DepositResult(account.Id.Value, account.Balance);
    }
}
```

Ghi chú:

* Handler gọi **Domain Entity/Domain Service**, không nhét nghiệp vụ phức tạp vào handler.
* IO (repo, logger, publisher) diễn ra ở đây hoặc trong behaviors (transaction).

### 2.3 Validator, Mapping, Pipeline

* Validator (FluentValidation) đặt cạnh Command/Query.
* Mapping (Mapster/AutoMapper) cho DTO/Result đặt trong `Mapping.cs` của slice.
* Pipeline behaviors:

  * `ValidationBehavior` (bắt buộc)
  * `LoggingBehavior` (tùy)
  * `AuthorizationBehavior` (tùy)
  * `TransactionBehavior` (nếu cần bao atomic nhiều repo)

### 2.4 Port/Interface giữa Application ↔ Infrastructure

Ví dụ:

```csharp
public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(AccountId id, CancellationToken ct = default);
    Task SaveAsync(Account account, CancellationToken ct = default);
}
```

Application chỉ biết interface. Hạ tầng implement.

### 2.5 Quy tắc kiểm soát phụ thuộc (Application)

* Không tham chiếu `Infrastructure`.
* Không dùng trực tiếp `IConfiguration`. Nếu cần config nghiệp vụ: define `IOptions<T>` hoặc interface config riêng rồi DI từ composition root.
* Không EF/ORM/HTTP client cụ thể trong handler.

---

## 3) INFRASTRUCTURE – GHI NHỚ CHÍNH

* EF Core mapping bằng Fluent API (`IEntityTypeConfiguration<T>`), migrations ở đây.
* Implement ports: repositories, outbox, email, object storage, brokers.
* Được phép dùng `IConfiguration` ở composition root/factory; adapters nên nhận `IOptions<T>`/tham số/client dựng sẵn.
* Không chứa business rules.

---

## 4) PRESENTATION (CONTROLLER) – GHI NHỚ CHÍNH

* Không business logic; nhận request → `IMediator.Send()` → trả DTO/result.
* Không gọi `DbContext`/repo trực tiếp.
* Trả mã trạng thái chuẩn và lỗi đã chuẩn hóa.

---

## 5) TEST CHỈ DẪN NGẮN

* Domain.UnitTests: test entity, value object, policy, domain service (thuần, không IO).
* Application.UnitTests: test handler với repo giả (mock); xác nhận orchestration đúng.
* Infrastructure.IntegrationTests: dùng Testcontainers (DB/MinIO/Kafka…) xác nhận adapter hoạt động thật.
* Contract Tests: một bộ test chung cho port (ví dụ `IAccountRepository`) áp dụng cho mọi adapter.

---

## 6) CHECKLIST NGẮN

Domain

* [ ] Không EF attribute, không IO, không `IConfiguration`.
* [ ] Entity chỉ hành vi gắn với bất biến của chính nó.
* [ ] Logic chạm nhiều entity → Domain Service.
* [ ] Policy/Rule tách riêng, dễ test.
* [ ] Domain Event chỉ mang dữ liệu nghiệp vụ.

Application

* [ ] Vertical Slice theo use case: Command/Query + Handler + Validator + Mapping + Result cùng thư mục.
* [ ] Handler gọi Domain (Entity/Domain Service), IO qua ports.
* [ ] Pipeline behaviors cho cross-cutting.
* [ ] Không `Infrastructure` dependency, không `IConfiguration`.

Infrastructure

* [ ] Implement ports, EF mapping Fluent API, migrations tại đây.
* [ ] Adapter nhận `IOptions<T>`/tham số/client dựng sẵn.
* [ ] Không business rules.

Presentation

* [ ] Controller mỏng, gọi `IMediator.Send()`.
* [ ] Trả DTO/result, không lộ entity.

Tests

* [ ] Domain: unit thuần.
* [ ] Application: unit với mock ports.
* [ ] Infrastructure: integration bằng Testcontainers.
* [ ] Contract tests cho ports quan trọng.

---

## 7) SKELETON THƯ MỤC TÓM TẮT

```
src/
  Domain/
    <Context>/
      Aggregates/
        <Aggregate>/
          <Aggregate>.cs
          ValueObjects/...
          Policies/...
          Events/...
          Exceptions/...
      Services/
        <DomainService>.cs
    Shared/...
  Application/
    Common/
      Interfaces/...
      Behaviors/...
    Features/
      <Feature>/<UseCase>/
        <CmdOrQuery>.cs
        <Handler>.cs
        <Validator>.cs
        <Result>.cs
        Mapping.cs
  Infrastructure/
    Persistence/
      AppDbContext.cs
      Configurations/...
      Migrations/...
    Repositories/...
    Messaging/...
    Storage/...
  WebApi/
    Controllers/...
    Program.cs
tests/
  Domain.UnitTests/...
  Application.UnitTests/...
  Infrastructure.IntegrationTests/...
  Contracts.Tests/...
```

---

## 8) QUY TẮC RA QUYẾT ĐỊNH NHANH

* Logic có IO? → Không ở Domain.
* Logic đụng >1 entity? → Domain Service.
* Hành vi bất biến của riêng entity? → phương thức trên Entity.
* Dàn dựng kịch bản (tìm, gọi domain, lưu, phát sự kiện…)? → Application Handler.
* Mapping/persistence chi tiết? → Infrastructure.