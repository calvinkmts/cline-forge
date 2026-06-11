---
name: ddd-java-spring
description: Domain-Driven Design patterns for Java 17 + Lombok + Spring Framework (non-Boot) + Oracle DB. Covers ubiquitous language, bounded contexts, entities, value objects, aggregates, domain services, application services, repositories, and domain events.
tags:
  - ddd
  - domain-driven-design
  - java
  - spring
  - oracle
  - architecture
version: 1.0.0
---

# ddd-java-spring

DDD is a design approach, not a framework. It works within your Spring/Oracle stack by structuring the code so that **domain logic is expressed in plain Java**, and Spring/Oracle are infrastructure concerns kept at the edges.

---

## When to Activate

- Designing a new module, service, or bounded context.
- Deciding whether something belongs in a Service, Entity, or Value Object.
- Reviewing domain logic that has leaked into repositories or controllers.
- Naming classes, methods, or packages to align with business language.
- During refactoring when extracting pure business logic from legacy services.

### When NOT to Activate

- Simple CRUD modules with no business rules (data-in/data-out)
- Read-only projections, reporting queries, or dashboards
- Pure data pipelines (ETL, batch transformations)
- One-to-one REST wrappers around existing database tables

DDD adds complexity. If the operation is "load this, map it, return it" with no business decisions, DDD is overkill.

---

## Package Structure

```
com.example.module/
├── domain/              # Entities, VOs, aggregates, domain services, repository interfaces
├── application/         # Application services (use cases)
├── infrastructure/      # Repository implementations, JPA config, Oracle specifics
└── interfaces/          # REST controllers, DTOs
```

The dependency arrow points inward: `interfaces → application → domain ← infrastructure`.

---

## Ubiquitous Language

The single most important DDD rule: **the code must speak the same language as the business.**

```java
// PASS: speaks the domain language
order.submit();
order.applyDiscount(discountPolicy);
customer.isEligibleForCredit();

// FAIL: technical language leaking into domain
orderService.updateOrderStatusToPendingPayment(order);
customerService.checkCreditFlag(customerId);
```

- Method names should describe **business actions**, not data mutations.
- Class names should match terms the business actually uses — if they say "Invoice", not "Bill".
- **Never let database column names leak into the domain.** `getCustNm()` and `getOrdStsCd()` are data smells, not domain language.

```java
// PASS: domain speaks business language
order.getCustomerName();
order.getStatus();

// FAIL: database naming leaks into domain
order.getCustNm();
order.getOrdStsCd();
```

The mapper/repository handles the translation between database column names and domain field names. Domain objects should not know about `CUST_NM`, `ORD_STS_CD`, or any table structure.

- If you need a comment to explain what a method does, the method name is wrong.

---

## Domain Exception Conventions

All custom exceptions extend `RuntimeException` and are named after the domain concept they guard.

```java
public class InvalidOrderStateException extends RuntimeException {
    public InvalidOrderStateException(String message) { super(message); }
}

public class InvalidDiscountException extends RuntimeException {
    public InvalidDiscountException(String message) { super(message); }
}

public class CurrencyMismatchException extends RuntimeException {
    public CurrencyMismatchException(String message) { super(message); }
}
```

Keep them in the domain layer alongside the entity they protect. One file per exception.

---

## Building Blocks

### Entity

An object defined by its **identity**, not its attributes. Two orders with the same data but different IDs are different orders.

```java
@Entity
@Table(name = "ORDERS")
@Getter
@EqualsAndHashCode(onlyExplicitlyIncluded = true)
@ToString
public class Order {

    // JPA requires a protected no-arg constructor
    protected Order() {}

    @Id
    @GeneratedValue(strategy = GenerationType.SEQUENCE, generator = "order_seq")
    @SequenceGenerator(name = "order_seq", sequenceName = "SEQ_ORDER_ID", allocationSize = 1)
    @Column(name = "ORDER_ID")
    @EqualsAndHashCode.Include
    private Long id;

    @Column(name = "CUSTOMER_ID")
    private Long customerId;        // reference other aggregates by ID

    @Column(name = "STATUS")
    private String status;

    @Column(name = "TOTAL_AMOUNT")
    private BigDecimal totalAmount;

    // Domain behavior lives on the entity, not in a service
    public void submit() {
        if (!"PENDING".equals(this.status)) {
            throw new InvalidOrderStateException("Order must be PENDING to submit");
        }
        this.status = "SUBMITTED";
    }

    public void applyDiscount(BigDecimal rate) {
        if (rate.compareTo(BigDecimal.ZERO) < 0 || rate.compareTo(BigDecimal.ONE) > 0) {
            throw new InvalidDiscountException("Rate must be between 0 and 1");
        }
        this.totalAmount = this.totalAmount.multiply(BigDecimal.ONE.subtract(rate));
    }
}
```

**Entity rules:**
- Always include a `protected` no-arg constructor for JPA.
- Do NOT use `@Data` — use `@EqualsAndHashCode(onlyExplicitlyIncluded = true)` based on ID.
- Domain behavior (state transitions, business rules) belongs on the entity, not the service.
- Protect invariants: never expose setters for state-transitioning fields. Use domain methods.
- Reference other aggregates by **ID only** — `private Long customerId`, not `private Customer customer`.

### Value Object

An object defined by its **attributes**, with no identity of its own. Two money amounts with the same currency and value are the same thing.

```java
@Embeddable
@Value   // Lombok: @Getter, @EqualsAndHashCode, @ToString, all-args constructor, final fields
public class Money {

    BigDecimal amount;
    String currency;

    // Prevent direct instantiation — use the static factory
    private Money() {}

    public static Money of(BigDecimal amount, String currency) {
        if (amount == null || currency == null || currency.isBlank()) {
            throw new IllegalArgumentException("Amount and currency are required");
        }
        if (amount.compareTo(BigDecimal.ZERO) < 0) {
            throw new IllegalArgumentException("Amount cannot be negative");
        }
        return new Money(amount, currency);
    }

    public Money add(Money other) {
        if (!this.currency.equals(other.currency)) {
            throw new CurrencyMismatchException("Cannot add " + this.currency + " and " + other.currency);
        }
        return Money.of(this.amount.add(other.amount), this.currency);
    }
}
```

**Value Object rules:**
- Always immutable. All fields `final`. No setters.
- Use `@Embeddable` if JPA-embedded.
- `equals` and `hashCode` based on all fields (Lombok `@Value` handles this).
- Operations return **new instances**, they do not mutate.
- Include input validation — prevent null/negative/empty states.

### Aggregate

A cluster of entities and value objects with a single **root** that controls access and enforces invariants across the cluster.

```java
@Entity
@Table(name = "ORDERS")
public class Order {

    // Protected no-arg constructor
    protected Order() {}

    @Id
    @GeneratedValue(strategy = GenerationType.SEQUENCE, generator = "order_seq")
    @SequenceGenerator(name = "order_seq", sequenceName = "SEQ_ORDER_ID", allocationSize = 1)
    private Long id;

    @Column(name = "CUSTOMER_ID")
    private Long customerId;        // ✅ reference by ID, not object

    @OneToMany(cascade = {CascadeType.PERSIST, CascadeType.MERGE}, orphanRemoval = true, fetch = FetchType.LAZY)
    @JoinColumn(name = "ORDER_ID")
    private List<OrderLine> lines = new ArrayList<>();

    @Column(name = "TOTAL_AMOUNT")
    private BigDecimal totalAmount;

    // Expose collection as unmodifiable — never let callers bypass the aggregate root
    public List<OrderLine> getLines() {
        return Collections.unmodifiableList(lines);
    }

    // External code adds lines through the aggregate root — never directly to the collection
    public void addLine(OrderLine line) {
        if (line == null) throw new ValidationException("Line must not be null");
        this.lines.add(line);
        recalculateTotal();
    }

    public void removeLine(Long lineId) {
        lines.removeIf(l -> l.getId().equals(lineId));
        recalculateTotal();
    }

    private void recalculateTotal() {
        this.totalAmount = lines.stream()
                .map(OrderLine::getSubtotal)
                .reduce(BigDecimal.ZERO, BigDecimal::add);
    }
}
```

```java
// OrderLine is a child entity, not an aggregate root
@Entity
@Table(name = "ORDER_LINES")
@Getter
public class OrderLine {

    protected OrderLine() {}

    @Id
    @GeneratedValue(strategy = GenerationType.SEQUENCE, generator = "order_line_seq")
    @SequenceGenerator(name = "order_line_seq", sequenceName = "SEQ_ORDER_LINE_ID", allocationSize = 1)
    private Long id;

    @Column(name = "PRODUCT_ID")
    private Long productId;

    @Column(name = "PRODUCT_NAME")
    private String productName;

    @Column(name = "QUANTITY")
    private int quantity;

    @Column(name = "SUBTOTAL")
    private BigDecimal subtotal;

    // Package-private constructor — only the aggregate root creates children
    OrderLine(Long productId, String productName, int quantity, BigDecimal price) {
        this.productId = productId;
        this.productName = productName;
        this.quantity = quantity;
        this.subtotal = price.multiply(BigDecimal.valueOf(quantity));
    }
}
```

**Aggregate rules:**
- Only reference other aggregates by **ID**, not direct object reference.
  - ✅ `private Long customerId;`
  - ❌ `private Customer customer;`
- The aggregate root is the only entry point for modifications.
- Repositories work at the aggregate root level — never save child entities directly.
- Expose child collections as unmodifiable (`Collections.unmodifiableList()`).
- Prefer `CascadeType.PERSIST` and `CascadeType.MERGE` over `CascadeType.ALL` — avoid unintended cascade removals.

---

## Embedding Value Objects in Entities

```java
@Entity
@Table(name = "ORDERS")
public class Order {

    @Embedded
    @AttributeOverrides({
        @AttributeOverride(name = "amount", column = @Column(name = "ORDER_TOTAL")),
        @AttributeOverride(name = "currency", column = @Column(name = "ORDER_CURRENCY"))
    })
    private Money totalAmount;
}
```

This bridges the gap between the domain's `Money` object and the database's flat column layout.

---

## Service Types

DDD distinguishes two kinds of services. Mixing them is the most common mistake.

| Type | Location | Contains | Spring annotation |
|---|---|---|---|
| **Domain Service** | Domain layer | Pure business logic that doesn't belong on a single entity | Plain Java class or `@Component` |
| **Application Service** | Application layer | Orchestration: load aggregate, call domain, persist, publish event | `@Service` |

```java
// Domain Service — pure logic, no DB, no Spring injection needed
public class DiscountPolicy {
    public BigDecimal calculateDiscount(Order order, Customer customer) {
        if (customer.isPremium() && order.getTotalAmount().compareTo(new BigDecimal("1000000")) > 0) {
            return new BigDecimal("0.15");
        }
        return BigDecimal.ZERO;
    }
}

// Application Service — orchestrates, uses domain service and repository
@Service
@RequiredArgsConstructor
@Transactional
public class OrderApplicationService {

    private final OrderRepository orderRepository;
    private final CustomerRepository customerRepository;
    private final DiscountPolicy discountPolicy;   // domain service injected

    public void submitOrder(Long orderId) {
        Order order = orderRepository.findById(orderId)
                .orElseThrow(() -> new OrderNotFoundException(orderId));
        Customer customer = customerRepository.findById(order.getCustomerId())
                .orElseThrow(() -> new CustomerNotFoundException(order.getCustomerId()));

        BigDecimal discount = discountPolicy.calculateDiscount(order, customer);
        order.applyDiscount(discount);
        order.submit();

        orderRepository.save(order);
    }
}
```

---

## Repository (Domain Contract)

The repository interface lives in the **domain layer**. The implementation lives in the **infrastructure layer** (where Oracle/JDBC lives). This keeps the domain ignorant of persistence mechanics.

```java
// Domain layer — interface only, no Oracle dependency
public interface OrderRepository {
    Optional<Order> findById(Long id);
    List<Order> findByStatus(String status);
    void save(Order order);
    void delete(Long id);
}

// Infrastructure layer — Oracle implementation
@Repository
@RequiredArgsConstructor
public class OracleOrderRepository implements OrderRepository {
    private final JdbcTemplate jdbcTemplate;

    @Override
    public Optional<Order> findById(Long id) {
        // Oracle-specific SQL here
    }
}
```

---

## Domain Events (Optional, but Valuable)

Raise events from the aggregate when something meaningful happens. This decouples side effects (email, audit) from the domain.

```java
// Domain event — plain Java, no Spring
public class OrderSubmittedEvent {
    private final Long orderId;
    private final Long customerId;
    private final LocalDateTime occurredAt;

    public OrderSubmittedEvent(Long orderId, Long customerId) {
        this.orderId = orderId;
        this.customerId = customerId;
        this.occurredAt = LocalDateTime.now();
    }
}

// Application service publishes after successful domain operation
@Service
@RequiredArgsConstructor
public class OrderApplicationService {
    private final ApplicationEventPublisher eventPublisher;

    @Transactional
    public void submitOrder(Long orderId) {
        // ... domain logic ...
        order.submit();
        orderRepository.save(order);
        eventPublisher.publishEvent(new OrderSubmittedEvent(order.getId(), order.getCustomerId()));
    }
}
```

Note: `ApplicationEventPublisher` comes from `org.springframework.context.ApplicationEventPublisher`.

---

## Mapping DDD to Spring Layers

```
Presentation Layer    →  @Controller / @RestController
                              ↓ DTO in / DTO out
Application Layer     →  @Service (Application Services only)
                              ↓ Domain objects
Domain Layer          →  Entities, Value Objects, Aggregates, Domain Services
                         (plain Java — no Spring annotations here ideally)
                              ↓ Repository interfaces
Infrastructure Layer  →  @Repository implementations, JdbcTemplate, JPA config, Oracle
```

---

## DDD Code Smells

- **Anemic domain model:** entities are just getters/setters and all logic is in services. Fix: move behavior onto entities.
- **Fat application service:** application service contains business rules. Fix: push rules onto entities or domain services.
- **Repository in domain service:** domain service calls a repository. Fix: application service loads data, passes it to domain service.
- **Database naming in domain:** fields named `CUST_NM`, `ORD_STS_CD` instead of `customerName`, `orderStatus`. Fix: domain objects should not know about column names — the mapper/repository handles that translation.
- **Technical language in domain:** method names like `updateStatusFlag`, `setActiveBoolean`. Fix: rename to domain actions.
- **Cross-aggregate direct reference:** `order.getCustomer().getAddress()`. Fix: reference by ID, load separately.

---

## Testing Strategy

- **Unit test** entities, value objects, and domain services — no Spring context needed.
- **Integration test** application services and repositories — use `@SpringBootTest` with an embedded database or testcontainers for Oracle.
- **Test domain exceptions** explicitly: invalid states, null inputs, boundary values.
- **Test aggregate invariants:** after every mutation, verify the aggregate is still consistent.

---

## Output Checklist

- [ ] Entity identity based on ID only (`@EqualsAndHashCode(onlyExplicitlyIncluded = true)`)
- [ ] Protected no-arg constructor present on all JPA entities
- [ ] No `@Data` on entities
- [ ] Child collections exposed as `Collections.unmodifiableList()` or similar
- [ ] Value Objects are immutable — Lombok `@Value` or all-field `final`
- [ ] Value Objects include `@Embeddable` if JPA-embedded
- [ ] Value Objects include input validation (null, negative, empty guards)
- [ ] Aggregate root is the only modification entry point for child entities
- [ ] Aggregate references other aggregates by ID only (`private Long customerId;`)
- [ ] `CascadeType.PERSIST` + `MERGE` preferred over `ALL`
- [ ] Domain Service contains no Spring beans, no DB calls
- [ ] Application Service orchestrates — does not contain business rules
- [ ] Repository interface in domain layer, implementation in infrastructure layer
- [ ] Ubiquitous language used in all method and class names
- [ ] No database column names leak into domain objects
- [ ] Exception classes are defined in the domain layer
- [ ] Tests cover entity behavior, aggregate invariants, and domain exceptions

---

## Related

- [Fowler — Anemic Domain Model](https://martinfowler.com/bliki/AnemicDomainModel.html)
- [Vernon — IDDD Samples (GitHub)](https://github.com/VaughnVernon/IDDD_Samples)
- [Khononov — Learning Domain-Driven Design](https://www.oreilly.com/library/view/learning-domain-driven-design/9781098100131/)
- [ttulka — ddd-example-ecommerce](https://github.com/ttulka/ddd-example-ecommerce)