# AI usage — how this repo was built

This document is the human layer on top of generated code: which tools did the work, how context was structured, where judgment overrode suggestions, and how AI-assisted tests were checked before trusting them.

---

## 1. Overview

Generative AI accelerated scaffolding and repetition (solution layout, boilerplate tests, Docker wiring). It did **not** replace design: layer boundaries, error model, state machine rules, and cache semantics were decided first and enforced by review. Cursor Composer carried **architecture-wide** changes; GitHub Copilot (inline in the editor) handled **local** completions. Claude (Sonnet on claude.ai) was used for upfront prompt planning and for stress-testing edge cases before they became bugs in code.

---

## 2. Tools used

| Tool | Version / plan | Primary use |
| --- | --- | --- |
| Cursor | Pro | Multi-file architecture generation; Composer for solution scaffold, cross-project consistency, and test suites |
| GitHub Copilot | (IDE inline) | Single-line and small-snippet completion inside existing types — method bodies, test arrange blocks, `Moq` setups |
| Claude (claude.ai) | Sonnet | Session planning, edge-case brainstorming, and structuring this AI-USAGE narrative |

**Division of labor:** Composer = vertical slices (domain → infra → API → tests) with shared context. Copilot = speed inside a file when signatures and types already exist. Neither was used to “guess” business rules without an explicit spec in the prompt or in code comments.

---

## 3. AI strategy

### How context was structured

The core principle was **architecture first, implementation second**. Rather than asking the model to write one file at a time in isolation, architectural decisions were front-loaded into a single high-context Cursor Composer prompt before large batches of code: layer layout, project references, entity shapes, interface contracts, error types, and NuGet packages. That reduced drift — e.g. `IOrderRepository` and `MongoOrderRepository` stayed aligned because the interface was in context when the implementation was generated.

One Composer session per major phase kept prompts coherent and limited “lost in the middle” confusion:

1. Solution scaffold and domain model  
2. Docker infrastructure and configuration  
3. Infrastructure implementations (MongoDB, Redis, RabbitMQ)  
4. Application layer and state machine  
5. Controllers and middleware  
6. Unit test generation  

Within each session, `@codebase` (and file/folder references) pointed at existing contracts so new code matched signatures — for example, generating `MongoOrderRepository` after `IOrderRepository` was visible avoided parameter and return-type mismatches.

### Composer vs Copilot

| | **Cursor Composer** | **GitHub Copilot (inline)** |
| --- | --- | --- |
| **Best for** | New files, multi-project edits, “implement this interface everywhere” | Finishing a method, filling `Setup`/`Verify`, repetitive assertions |
| **Risk** | Over-confident structure that looks complete but omits edge cases | Plausible but wrong API calls or outdated package APIs |
| **Mitigation** | Single prompt per phase + `@codebase`; review cross-project references in one pass | Accept only when the suggestion matches visible types and team conventions |

---

## 4. Human audit — accept / reject

Three concrete decisions from working on this service (not generic “AI can be wrong” advice):

### Example A — **Accepted:** explicit state machine table + transition tests

**Suggestion:** Composer produced `OrderStatusTransitionService` with a static dictionary of allowed transitions and xUnit tests using `[Theory]` and `[InlineData]` for valid paths, skipped states, and invalid jumps.

**Reasoning:** The rules are finite and documentable (`Pending → … → Delivered`, cancel only from `Pending` / `Confirmed`, terminals immutable). Encoding them in one place plus table-driven tests made regressions obvious. I accepted the structure and manually verified every `[InlineData]` pair against the dictionary — especially “skipping” states (e.g. `Pending` → `Processing`), which must fail.

**Why not reject:** A vaguer AI output might have scattered `switch` logic across services; centralizing validation kept `UpdateOrderStatusAsync` and HTTP mapping honest.

### Example B — **Rejected:** noisy logging inside Redis cache “swallow” handlers

**Suggestion:** Inline completion repeatedly nudged toward `catch` blocks that log-and-rethrow or log verbosely on every Redis failure in `RedisCacheService`.

**Reasoning:** Cache here is **best-effort**: callers must keep working if Redis is down or a key is missing. Adding logging without a clear observability strategy would either spam logs on transient failures or imply operational guarantees the code does not provide. I kept intentionally quiet catches with a short comment that failures must not break callers, and left structured logging to API / host configuration rather than ad hoc lines inside infrastructure.

**Trade-off:** Less immediate visibility into cache failures in code — acceptable given the contract of `ICacheService`.

### Example C — **Accepted:** strict routing key in publisher verification

**Suggestion:** Early test drafts used `PublishAsync(It.IsAny<string>(), …)` for success paths.

**Reasoning:** `OrderService` uses a named constant for the create event (`order.created`). A passing test with `It.IsAny<string>()` would still pass if someone changed the routing key to the wrong topic and broke consumers. I accepted AI-generated arrange/act blocks but **tightened** the assertion to the concrete key so the test guards the integration contract, not just “something was published.”

---

## 5. Verification — validating AI-generated tests

AI-written tests can be **green but meaningless** if they assert the wrong thing or mirror bugs. Verification here followed a short checklist:

1. **`dotnet test`** on `tests/OrderProcessingService.Tests/OrderProcessingService.Tests.csproj` and on the full solution — must be green locally after any change to generated or hand-edited tests.  
2. **Behavior vs implementation:** For state transitions, tests were checked against the **stated rules** (README / comments), not only against the current code — if code and spec disagreed, the spec or the code was fixed deliberately.  
3. **Constants and magic values:** TTL (`TimeSpan.FromMinutes(5)` in cache tests), routing keys, and cache key helpers were cross-checked against `ProductCatalogService` / `OrderService` so tests did not encode a duplicate wrong number.  
4. **Mock intent:** Each `Verify` was read for necessity — e.g. “never create order” on failure paths, “publish exactly once” on success — and trimmed if it duplicated another assertion without adding a guarantee.  
5. **Readability:** FluentAssertions messages and test names were scanned for clarity; obscure one-liners from AI were expanded where a future reader would not see what broke.

This process treats tests as **executable specification**: generated tests are a draft until they pass the checklist above.

---

## 6. Summary

| Practice | Outcome |
| --- | --- |
| Composer per phase + `@codebase` | Consistent layers and fewer signature mismatches |
| Copilot for local completion only | Faster typing without letting the model redefine architecture |
| Human audit on infrastructure behavior and test strictness | Cache semantics and messaging contracts match intent |
| Explicit verification of AI tests | Green CI plus alignment with domain rules and constants |

This file reflects **judgment calls** in this repository — update it when tooling or process changes.
