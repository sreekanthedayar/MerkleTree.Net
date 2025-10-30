# MerkleTree.Net

A Modern .NET port of [Marc Clifton's MerkleTree](https://github.com/cliftonm/MerkleTree).

Cryptographic Merkle tree implementation with audit proofs and consistency proofs for blockchain, transparency logs, and data integrity verification.

## Installation
```bash
dotnet add package MerkleTree.Net
```

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
bool consistent = MerkleTree.VerifyConsistency(oldRoot, proof);
// consistent == true
```

## Common Use Cases

- **Blockchain**: Verify transactions without downloading entire chain
- **Git-like Systems**: Content verification and history tracking  
- **Audit Logs**: Tamper-proof logging with verifiable history
- **Certificate Transparency**: Verify certificate issuance (RFC 6962)

## API Overview

**MerkleTree**
- `AppendLeaf(MerkleHash)` - Add data to tree
- `AddLeaf(byte[], bool autoHash)` - Add raw data with optional auto-hashing
- `BuildTree()` - Build tree and get root hash
- `AuditProof(MerkleHash)` - Prove a leaf exists
- `ConsistencyProof(int)` - Prove tree growth

**MerkleHash**  
- `Create(string)` - Hash from string
- `Create(byte[])` - Hash from bytes
- `Create(ReadOnlySpan<byte>)` - Zero-allocation hash from span
- `ToHex()` - Convert to hexadecimal string
- `FromHex(string)` - Parse from hexadecimal string

**MerkleSerializer**
- `SerializeAuditProof(List<MerkleProofHash>)` - Export audit proof to JSON
- `DeserializeAuditProof(string)` - Import audit proof from JSON
- `SerializeConsistencyProof(List<MerkleProofHash>)` - Export consistency proof to JSON
- `DeserializeConsistencyProof(string)` - Import consistency proof from JSON
- `SerializeTreeMetadata(MerkleTree)` - Export tree metadata to JSON

## What's Different from Original

- .NET 8 with modern C# features
-  No external dependencies
-  Nullable reference types for safety
-  Resource leak fixes

## Enhancements in This Port

### Performance Optimizations
-  **Zero-allocation hot paths** - `stackalloc` for hash operations (18% faster BuildTree)
-  **ArrayPool integration** - Reduced memory allocations by 19% with automatic cleanup
-  **LINQ removal in critical paths** - Direct loops for better performance
-  **Pre-allocated buffers** - Capacity hints to avoid list reallocations
-  **TryComputeHash optimization** - Minimized GC pressure (Gen2 reduced 35%)

### Extended Functionality
-  **JSON Serialization** - Complete serialization/deserialization for proofs and metadata
-  **Flexible Hash Algorithms** - Support for SHA256, SHA512, or any `HashAlgorithm`
-  **Auto-hashing** - `AddLeaf(data, autoHash: true)` for convenience
-  **Hex encoding/decoding** - Efficient `ToHex()` and `FromHex()` methods
-  **IDisposable pattern** - Proper resource cleanup for hash algorithms

### Code Quality
-  **73 unit tests** - Comprehensive test coverage including edge cases
-  **Benchmark suite** - Performance validation with BenchmarkDotNet
-  **Memory diagnostics** - GC pressure analysis and optimization
-  **Concurrency tests** - Thread-safety validation

### Performance Benchmarks
```
BenchmarkDotNet v0.15.4, .NET 8.0.18
AMD Ryzen 5 7535HS, Windows 11

| Method           | Leaves | Mean      | Gen0 | Gen1 | Gen2 | Allocated  |
|------------------|--------|-----------|------|------|------|------------|
| BuildTree        | 100    | 110 μs    | 15   | 1    | 0    | 126 KB     |
| BuildTree        | 1,000  | 1.06 ms   | 150  | 47   | 0    | 1.23 MB    |
| BuildTree        | 10,000 | 18.63 ms  | 1625 | 688  | 156  | 12.47 MB   |
| AuditProof       | 100    | 1.19 μs   | 0    | 0    | 0    | 1.16 KB    |
| AuditProof       | 1,000  | 9.92 μs   | 0    | 0    | 0    | 1.69 KB    |
| AuditProof       | 10,000 | 96.29 μs  | 0    | 0    | 0    | 2.18 KB    |
| ConsistencyProof | 1,000  | 285 μs    | 31   | 0    | 0    | 252 KB     |
| ConsistencyProof | 10,000 | 4.83 ms   | 398  | 0    | 0    | 3.29 MB    |
```

## Acknowledgements

Original: [Marc Clifton](https://github.com/cliftonm/MerkleTree)  
Modern .NET Port & Enhancements: Sreekanth Edayar

## License

MIT - see [LICENSE](LICENSE)