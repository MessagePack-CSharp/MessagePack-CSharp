# Design: By-Reference Struct Serialization/Deserialization (Compatibility-Preserving)

Date: 2026-02-26
Status: Approved (brainstorming)

## Context

`IMessagePackFormatter<T>` currently serializes by value and deserializes by return value:

- `void Serialize(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options)`
- `T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)`

For large structs, this can introduce avoidable copies. It also prevents formatter authors from reusing an existing struct instance during deserialization (e.g. reclaiming pooled object fields before replacement).

## Goals

- Preserve full backward compatibility with existing `IMessagePackFormatter<T>` implementations and call sites.
- Add optional by-reference APIs for struct-centric optimization.
- Allow in-place deserialization semantics so custom formatters can process old field values before overwrite.
- Enable these optimizations in nested formatter paths (not just top-level serializer calls).

## Non-Goals

- No binary format change.
- No breaking changes to `IMessagePackFormatter<T>`.
- No mandatory migration for existing formatters.

## API Design

### 1. New optional interfaces

Introduce additive interfaces for by-ref optimization:

- `IMessagePackFormatterSerializeIn<T>`
  - `void Serialize(ref MessagePackWriter writer, in T value, MessagePackSerializerOptions options)`
- `IMessagePackFormatterDeserializeRef<T>`
  - `void Deserialize(ref MessagePackReader reader, ref T value, MessagePackSerializerOptions options)`

These are optional and can be implemented alongside `IMessagePackFormatter<T>`.

### 2. New top-level serializer overloads

Add additive overloads:

- `MessagePackSerializer.Serialize<T>(ref MessagePackWriter writer, in T value, MessagePackSerializerOptions? options = null)`
- `MessagePackSerializer.Deserialize<T>(ref MessagePackReader reader, ref T value, MessagePackSerializerOptions? options = null)`

Existing overloads remain unchanged.

## Dispatch Architecture

Introduce an internal dispatch layer, tentatively `FormatterDispatch<T>`, with generic static caches.

Responsibilities:

- Detect once per `T` whether formatter implements new optional interfaces.
- Provide unified call surface for all call sites.
- Prefer new interfaces when available; fallback to legacy interface otherwise.

Expected internal entrypoints:

- `SerializeByValue(...)`
- `SerializeByIn(...)`
- `DeserializeToValue(...)`
- `DeserializeIntoRef(...)`

This keeps hot paths free of per-call reflection and minimizes interface checks.

## Integration Plan (Architecture-Level)

### MessagePackSerializer

- New overloads call dispatch layer.
- Existing overloads may also route through dispatch for deduplicated logic.
- Preserve existing exception-wrapping behavior.

### Source Generator

- Generated formatter code should call dispatch helpers instead of directly binding only legacy method signatures.
- Struct members should route through `SerializeByIn` / `DeserializeIntoRef` pathways where applicable.

### Dynamic Resolver (IL emit)

- Replace direct `IMessagePackFormatter<T>.Serialize/Deserialize` call binding with dispatch calls.
- Keep fallback path functionally identical when no new interface is implemented.

## Behavioral Rules

- Priority: new by-ref interfaces > legacy interface.
- If formatter only implements legacy interface, behavior remains exactly as today.
- Formatter contract remains: read/write exactly one top-level MessagePack structure.

## Error Handling and Edge Cases

- Struct nil handling remains unchanged from current semantics.
- In-place deserialization (`ref T value`) introduces a deliberate trade-off:
  - Formatter may reclaim old resources before parse completes.
  - If deserialization later fails, value may be partially transitioned.
- This risk should be documented for custom formatter authors.

## Testing Strategy

### Dispatch correctness

- Legacy-only formatter path.
- New-interface-only formatter path.
- Dual-implementation path (verify new interface precedence).

### Serializer overloads

- New overload behavior matches existing wire format.
- Exception behavior parity with legacy overloads.

### Struct optimization scenarios

- Large struct top-level serialization/deserialization.
- Nested struct fields in generated and dynamic resolver paths.
- Collection/tuple/value-tuple nested struct coverage.

### Reuse/Pooling scenario

- Custom formatter using `Deserialize(ref reader, ref T value, ...)` can observe old value and reclaim pooled resources before overwrite.

### Compatibility regression

- Existing formatter ecosystem compiles and runs unchanged.
- Existing test suite passes without requiring formatter rewrites.

## Alternative Approaches Considered

1. Top-level overloads only: low risk, but limited benefit for nested member serialization.
2. Struct-only parallel formatter ecosystem: clearer semantics, but much higher complexity.
3. Breaking change to `IMessagePackFormatter<T>`: rejected due to compatibility impact.

## Recommendation

Adopt additive optional interfaces + unified dispatch layer + serializer/resolver/sourcegen integration.

This provides the desired struct optimization and in-place deserialization extensibility while preserving compatibility.
