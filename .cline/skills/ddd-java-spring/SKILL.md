---
name: ddd-java-spring
description: Domain-Driven Design patterns for Java 17 + Lombok + Spring + Oracle DB. Covers entities, value objects, aggregates, services, repositories, and domain events.
tags: [ddd, domain-driven-design, java, spring, oracle, architecture]
version: 1.0.0
---

# ddd-java-spring

DDD means domain logic in plain Java. Spring/Oracle are infrastructure concerns at the edges.

## When to Activate

- Designing a new module, service, or bounded context
- Deciding Entity vs Value Object vs Service
- Reviewing domain logic leaked into repositories or controllers
- Naming classes/methods to align with business language
- Extracting pure business logic from legacy services

**When NOT:** Simple CRUD with no business rules. Read-only projections/reports. Pure data pipelines (ETL). DDD adds complexity — don't use it for "load, map, return."

## Package Structure

```
com.example.module/
├── domain/          # Entities, VOs, aggregates, domain services, repo interfaces
├── application/     # Application services (use cases)
├── infrastructure/  # Repository implementations, JPA/Oracle config
└── interfaces/      # REST controllers, DTOs
```

Dependency arrow points inward: `interfaces → application → domain ← infrastructure`.

## Ubiquitous Language

Code must speak the same language as the business. Method names describe **business actions**, not data mutations.

```java
// PASS
order.submit();
order.applyDiscount(discountPolicy);
customer.getName();

// FAIL — technical or DB language
orderService.updateOrderStatusToPendingPayment(order);
customer.getCustNm();     // DB column leaked into domain
order.getOrdStsCd();      // DB column leaked into domain
```

The mapper/repository handles column-name translation. Domain objects know nothing about `CUST_NM` or `ORD_STS_CD`.

## Building Blocks

### Entity — identity-based, stateful

```java
@Entity @Table(name = "ORDERS")
@Getter @EqualsAndHashCode(onlyExplicitlyIncluded = true) @ToString
public class Order {
    protected Order() {}  // JPA no-arg

    @Id @GeneratedValue(strategy = GenerationType.SEQUENCE, generator = "order_seq")
    @SequenceGenerator(name = "order_seq", sequenceName = "SEQ_ORDER_ID", allocationSize = 1)
    @Column(name = "ORDER_ID") @EqualsAndHashCode.Include
    private Long id;

    @Column(name = "CUSTOMER_ID")
    private Long customerId;        // reference aggregates by ID, not object

    @Column(name = "STATUS")
    private String status;

    @Column(name = "TOTAL_AMOUNT")
    private BigDecimal totalAmount;

    public void submit() {
        if (!"PENDING".equals(this.status))
            throw new InvalidOrderStateException("Order must be PENDING to submit");
        this.status = "SUBMITTED";
    }
}
```

**Rules:** ✅ `@EqualsAndHashCode(onlyExplicitlyIncluded = true)` — identity by ID only. ✅ No `@Data`. ✅ Protected no-arg for JPA. ✅ Reference other aggregates by `Long customerId;` not `Customer customer;`. ❌ Never expose setters for state fields.

### Value Object — immutable, attribute-defined

```java
@Embeddable @Value
public class Money {
    BigDecimal amount;
    String currency;

    protected Money() {}

    public static Money of(BigDecimal amount, String currency) {
        if (amount == null || currency == null || currency.isBlank())
            throw new IllegalArgumentException("Amount and currency required");
        if (amount.compareTo(BigDecimal.ZERO) < 0)
            throw new IllegalArgumentException("Amount cannot be negative");
        return new Money(amount, currency);
    }

    public Money add(Money other) {
        if (!this.currency.equals(other.currency))
            throw new CurrencyMismatchException("Cannot add " + this.currency + " and " + other.currency);
        return Money.of(this.amount.add(other.amount), this.currency);
    }
}
```

**Rules:** ✅ All fields final. ✅ `@Embeddable` for JPA. ✅ Input validation. ✅ Operations return new instances. ✅ No setters.

### Aggregate — cluster with a single root

```java
@Entity @Table(name = "ORDERS")
public class Order {
    protected Order() {}

    @Id @GeneratedValue(strategy = GenerationType.SEQUENCE, generator = "order_seq")
    @SequenceGenerator(name = "order_seq", sequenceName = "SEQ_ORDER_ID", allocationSize = 1)
    @Column(name = "ORDER_ID")
    private Long id;

    @Column(name = "CUSTOMER_ID") private Long customerId;  // by ID

    @OneToMany(cascade = {CascadeType.PERSIST, CascadeType.MERGE}, orphanRemoval = true, fetch = FetchType.LAZY)
    @JoinColumn(name = "ORDER_ID")
    private List<OrderLine> lines = new ArrayList<>();

    @Column(name = "TOTAL_AMOUNT") private BigDecimal totalAmount;

    public List<OrderLine> getLines() {
        return Collections.unmodifiableList(lines);  // don't leak mutable collection
    }

    public void addLine(OrderLine line) {
        if (line == null) throw new ValidationException("Line must not be null");
        this.lines.add(line);
        recalculateTotal();
    }

    private void recalculateTotal() {
        this.totalAmount = lines.stream().map(OrderLine::getSubtotal)
                .reduce(BigDecimal.ZERO, BigDecimal::add);
    }
}

@Entity @Table(name = "ORDER_LINES") @Getter
public class OrderLine {
    protected OrderLine() {}
    @Id private Long id;
    @Column(name = "PRODUCT_ID") private Long productId;
    @Column(name = "QUANTITY") private int quantity;
    @Column(name = "SUBTOTAL") private BigDecimal subtotal;

    OrderLine(Long productId, String productName, int quantity, BigDecimal price) {
        this.productId = productId; this.quantity = quantity;
        this.subtotal = price.multiply(BigDecimal.valueOf(quantity));
    }
}
```

**Rules:** ✅ Reference aggregates by ID. ✅ Root is the only entry point. ✅ Collections exposed as `unmodifiableList`. ✅ Prefer `CascadeType.PERSIST + MERGE` over `ALL`. ❌ Never save child entities directly through repositories.

## Service Types

| Type | Location | Contains | Spring Annotation |
|---|---|---|---|
| Domain Service | Domain layer | Pure business logic not belonging on one entity | Plain Java |
| Application Service | Application layer | Orchestration: load, call domain, persist, publish | `@Service` |

```java
// Domain Service — pure logic, no DB
public class DiscountPolicy {
    public BigDecimal calculateDiscount(Order order, Customer customer) {
        if (customer.isPremium() && order.getTotalAmount().compareTo(new BigDecimal("1000000")) > 0)
            return new BigDecimal("0.15");
        return BigDecimal.ZERO;
    }
}

// Application Service — orchestrates
@Service @RequiredArgsConstructor @Transactional
public class OrderApplicationService {
    private final OrderRepository orderRepository;
    private final CustomerRepository customerRepository;
    private final DiscountPolicy discountPolicy;

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

## Repository (Domain Contract)

Interface in domain, implementation in infrastructure.

```java
// Domain layer
public interface OrderRepository {
    Optional<Order> findById(Long id);
    void save(Order order);
}

// Infrastructure layer — Oracle-specific
@Repository @RequiredArgsConstructor
public class OracleOrderRepository implements OrderRepository {
    private final JdbcTemplate jdbcTemplate;
    // Oracle SQL here
}
```

## Domain Events

Surface side effects through events, not coupled service calls.

```java
public record OrderSubmittedEvent(Long orderId, Long customerId, LocalDateTime occurredAt) {
    public OrderSubmittedEvent(Long orderId, Long customerId) {
        this(orderId, customerId, LocalDateTime.now());
    }
}

// In application service:
eventPublisher.publishEvent(new OrderSubmittedEvent(order.getId(), order.getCustomerId()));
```

`ApplicationEventPublisher` from `org.springframework.context`.

## DDD Code Smells

- **Anemic domain:** entities are just getters/setters, logic in services. Fix: move behavior onto entities.
- **Fat application service:** service contains business rules. Fix: push rules to entities or domain services.
- **Repository in domain service:** domain service calls DB. Fix: app service loads data first.
- **DB naming in domain:** `CUST_NM`, `ORD_STS_CD` in domain objects. Fix: mapper handles column translation.
- **Technical method names:** `updateStatusFlag`, `setActiveBoolean`. Fix: business actions (`submit`, `approve`).
- **Cross-aggregate navigation:** `order.getCustomer().getAddress()`. Fix: by ID, load separately.
- **@Getter on entities with collections:** exposes mutable list. Fix: `unmodifiableList()`.

## Check Before You Ship

- [ ] Entity identity by ID only (`@EqualsAndHashCode(onlyExplicitlyIncluded = true)`)
- [ ] Protected no-arg constructor on all JPA entities
- [ ] No `@Data` on entities
- [ ] Collections exposed as `Collections.unmodifiableList()` or copy
- [ ] VOs: `@Embeddable`, immutable, input validation, all-args from factory only
- [ ] Aggregate references other aggregates by ID (`Long customerId` not `Customer customer`)
- [ ] `CascadeType.PERSIST + MERGE` preferred over `ALL`
- [ ] Domain service: no Spring beans, no DB calls
- [ ] App service orchestrates only — no business rules
- [ ] Repository interface in domain, impl in infrastructure
- [ ] Ubiquitous language in all method names — no `getCustNm()`
- [ ] Exception classes defined in domain layer (extend `RuntimeException`)
- [ ] Tests cover entity behavior, aggregate invariants, domain exceptions