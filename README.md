# MerkleTree.Net

A Modern .NET port of [Marc Clifton's MerkleTree](https://github.com/cliftonm/MerkleTree).

Cryptographic Merkle tree implementation with audit proofs and consistency proofs for blockchain, transparency logs, and data integrity verification.

## Installation
```bash
dotnet add package MerkleTree.Net
```

## What's New in v2.3.0

Version 2.3.0 includes performance improvements and a consistency-proof serialization fix after v2.2.0:

- Optimized default SHA-256 internal-node hashing with direct `HashData` calls, `stackalloc` for small inputs, and pooled buffers for larger inputs while preserving RFC 6962 domain separation.
- Reduced consistency-proof generation overhead by removing unnecessary full-subtree leaf enumeration from the successful path.
- Allowed valid same-size consistency proof packages when the old and new leaf counts and roots match and the proof path is empty.
- Continued rejecting consistency packages with decreasing leaf counts, mismatched same-size roots, or non-empty same-size proof paths.
- Added regression coverage for same-size consistency proof serialization and deserialization.

## What's New in v2.2.0

Version 2.2.0 includes correctness, security, and compatibility fixes after v2.1.0:

- Added support for single-leaf audit proofs and preserved precomputed leaf digests.
- Switched tree hashing to RFC 6962-style domain-separated leaf and node hashes.
- Added support for variable-length custom hash algorithms.
- Made `MerkleHash` immutable from callers by protecting its underlying digest.
- Prevented `AppendLeaf`, node constructors, and node setters from reparenting nodes owned by another tree.
- Rejected audit and consistency proofs requested after leaves are appended but before the tree is rebuilt.
- Preserved configured custom hash algorithms through node mutation APIs.
- Retained `FixOddNumberLeaves()` for source compatibility as an obsolete no-op under RFC 6962 tree semantics.
- Hardened proof verification and deserialization against malformed, incomplete, invalid, or ambiguous proof data.
- Added regression coverage for hash mutation, node ownership, custom algorithms, stale trees, odd-leaf handling, duplicate hashes, and malformed proofs.

## Breaking Changes in v2.2.0

- Merkle roots and proofs are not compatible with versions that used the previous hash construction. v2.2.0 uses RFC 6962-style domain separation.
- `MerkleHash.Value` now returns a copy. Mutating the returned array no longer mutates the hash.
- `MerkleHash` hash-mutator methods are no longer public; create hashes with `Create`, `FromDigest`, or `FromHex`.
- Proof generation now rejects trees that have new leaves appended since the last `BuildTree()` call.
- `FixOddNumberLeaves()` is obsolete and does not duplicate leaves under the RFC 6962 tree model.
- Node constructors and setters clone nodes already attached to another tree instead of reparenting them.
- Proof-package deserialization now rejects unsupported metadata, invalid hashes, invalid counts, and malformed proof paths.

## ⚠️ Breaking Changes in v2.1.0

**If you're upgrading from v1.0.1, please read carefully:**

### What Changed
- **JSON Serialization Format**: Proof serialization now uses structured packages with metadata (version, timestamp, root hash)
- **Performance**: 20% faster with reduced memory allocations
- **API Additions**: New `AddLeaf()`, `ToHex()`, `FromHex()` methods

### Compatibility Matrix

| Aspect | v1.0.1 → v2.1.0 |
|--------|-----------------|
| **C# API (source code)** |  Mostly compatible - your code will compile |
| **Serialized proofs (JSON)** |  **Incompatible** - cannot share proofs between versions |
| **Hash computation** |  Changed - leaf and node hashes use RFC 6962 domain separation |

### Migration Guide

**If you DON'T serialize proofs** (only use in-memory):
```bash
# Safe to upgrade directly
dotnet add package MerkleTree.Net --version 2.1.0
```

**If you DO serialize proofs** (save to files/databases/APIs):
1. **Do NOT mix versions** - upgrade all systems simultaneously
2. Old proofs cannot be verified by v2.1.0
3. Consider maintaining v1.0.1 for legacy proof verification

**Example of breaking change:**
```csharp
// v1.0.1 JSON format (simple array)
["hash1", "hash2", "hash3"]

// v2.1.0 JSON format (structured package)
{
  "version": "1.0",
  "timestamp": "2025-01-15T10:30:00Z",
  "treeMetadata": { "rootHash": "abc123...", "leafCount": 1000 },
  "proof": { "leafHash": "def456...", "proofPath": [...] }
}
```

**Need help migrating?** Open an issue on GitHub.

---

## Quick Start

### Build a Merkle Tree
```csharp
using Clifton.Blockchain;

var tree = new MerkleTree();
tree.AppendLeaf(MerkleHash.Create("data1"));
tree.AppendLeaf(MerkleHash.Create("data2"));
tree.AppendLeaf(MerkleHash.Create("data3"));

MerkleHash rootHash = tree.BuildTree();
Console.WriteLine($"Root: {rootHash}");
```

### Verify a Leaf Exists (Audit Proof)
```csharp
var tree = new MerkleTree();
var myData = MerkleHash.Create("important data");

tree.AppendLeaf(myData);
tree.AppendLeaf(MerkleHash.Create("other data"));
tree.BuildTree();

// Prove myData is in the tree
var proof = tree.AuditProof(myData);
bool isValid = MerkleTree.VerifyAudit(tree.RootNode.Hash, myData, proof);
// isValid == true
```

### Verify Tree Growth (Consistency Proof)
```csharp
// Original tree with 4 items
var oldTree = new MerkleTree();
for (int i = 1; i <= 4; i++)
    oldTree.AppendLeaf(MerkleHash.Create($"item{i}"));
var oldRoot = oldTree.BuildTree();

// New tree with 8 items (same first 4)
var newTree = new MerkleTree();
for (int i = 1; i <= 8; i++)
    newTree.AppendLeaf(MerkleHash.Create($"item{i}"));
newTree.BuildTree();

// Prove new tree contains old tree
var proof = newTree.ConsistencyProof(4);
bool consistent = MerkleTree.VerifyConsistency(
    oldRoot,
    newTree.RootNode.Hash,
    4,
    8,
    proof);
// consistent == true
```

## Common Use Cases

- **Blockchain**: Verify transactions without downloading entire chain
- **Git-like Systems**: Content verification and history tracking  
- **Audit Logs**: Tamper-proof logging with verifiable history
- **Certificate Transparency-style trees**: Use RFC 6962 domain-separated leaf and node hashing

## API Overview

**MerkleTree**
- `AppendLeaf(MerkleHash)` - Add data to tree
- `AddLeaf(byte[], bool autoHash)` - Add raw data with optional auto-hashing
- `BuildTree()` - Build tree and get root hash
- `AuditProof(MerkleHash)` - Prove a leaf exists
- `ConsistencyProof(int)` - Generate a size-aware proof of append-only growth
- `VerifyConsistency(MerkleHash, MerkleHash, int, int, IReadOnlyList<MerkleProofHash>)` - Verify both tree roots and sizes

**MerkleHash**  
- `Create(string)` - Hash data as `SHA256(0x00 || UTF8(data))`
- `Create(byte[])` - Hash data as `SHA256(0x00 || data)`
- `Create(ReadOnlySpan<byte>)` - Domain-separated hash from span
- `Create(MerkleHash, MerkleHash)` - Hash nodes as `SHA256(0x01 || left || right)`
- `FromDigest(ReadOnlySpan<byte>)` - Create from an existing digest without hashing again
- `ToHex()` - Convert to hexadecimal string
- `FromHex(string)` - Parse from hexadecimal string

**MerkleSerializer**
- `SerializeAuditProofPackage(AuditProofPackage)` - Export audit proof to JSON
- `DeserializeAuditProofPackage(string)` - Import audit proof from JSON
- `SerializeConsistencyProofPackage(ConsistencyProofPackage)` - Export consistency proof to JSON
- `DeserializeConsistencyProofPackage(string)` - Import consistency proof from JSON

## What's Different from Original

- .NET 10 with modern C# features
- No external dependencies
- Nullable reference types for safety
- Resource leak fixes

## Enhancements in This Port

### Performance Optimizations
-  **Zero-allocation hot paths** - `stackalloc` for hash operations (20% faster BuildTree)
-  **ArrayPool integration** - Reduced memory allocations by 19% with automatic cleanup
-  **LINQ removal in critical paths** - Direct loops for better performance
-  **Pre-allocated buffers** - Capacity hints to avoid list reallocations
-  **TryComputeHash optimization** - Minimized GC pressure

### Extended Functionality
-  **JSON Serialization** - Complete serialization/deserialization for proofs and metadata
-  **Flexible Hash Algorithms** - Support for SHA256, SHA512, or any `HashAlgorithm`
-  **Auto-hashing** - `AddLeaf(data, autoHash: true)` for convenience
-  **Hex encoding/decoding** - Efficient `ToHex()` and `FromHex()` methods
-  **IDisposable pattern** - Proper resource cleanup for hash algorithms

### Code Quality
-  **104 unit tests** - Comprehensive test coverage including edge cases
-  **Benchmark suite** - Performance validation with BenchmarkDotNet
-  **Memory diagnostics** - GC pressure analysis and optimization
-  **Concurrency tests** - Thread-safety validation

### Performance Benchmarks
```
BenchmarkDotNet v0.15.4, .NET 10.0.3
AMD Ryzen 5 7535HS, Windows 11
ShortRun: 3 warmup iterations, 3 measurement iterations

| Method           | Leaves | Mean      | Gen0 | Gen1 | Gen2 | Allocated  |
|------------------|--------|-----------|------|------|------|------------|
| BuildTree        | 100    | 106.1 μs   | 13.9160 | 1.0986 | 0        | 116,595 B    |
| BuildTree        | 1,000  | 1.041 ms   | 142.5781 | 44.9219 | 0        | 1,203,024 B  |
| BuildTree        | 10,000 | 17.427 ms  | 1500.0000 | 625.0000 | 156.2500 | 11,569,279 B |
| AuditProof       | 100    | 353.6 ns   | 0.1230 | 0      | 0        | 1,033 B      |
| AuditProof       | 1,000  | 1.371 μs   | 0.1869 | 0      | 0        | 1,574 B      |
| AuditProof       | 10,000 | 10.460 μs  | 0.2441 | 0      | 0        | 2,076 B      |
| ConsistencyProof | 100    | 125.4 ns   | 0.0715 | 0      | 0        | 600 B        |
| ConsistencyProof | 1,000  | 172.5 ns   | 0.1032 | 0      | 0        | 864 B        |
| ConsistencyProof | 10,000 | 224.7 ns   | 0.1233 | 0.0002 | 0        | 1,032 B      |
```

## Acknowledgements

Original: [Marc Clifton](https://github.com/cliftonm/MerkleTree)  
Modern .NET Port & Enhancements: Sreekanth Edayar

## License

MIT - see [LICENSE](LICENSE)
