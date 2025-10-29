# MerkleTree.Net

A Modern .NET port of [Marc Clifton's MerkleTree](https://github.com/cliftonm/MerkleTree).

Cryptographic Merkle tree implementation with audit proofs and consistency proofs for blockchain, transparency logs, and data integrity verification.

## Installation
```bash
dotnet add package Clifton.Blockchain.MerkleTree
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
- `BuildTree()` - Build tree and get root hash
- `AuditProof(MerkleHash)` - Prove a leaf exists
- `ConsistencyProof(int)` - Prove tree growth

**MerkleHash**  
- `Create(string)` - Hash from string
- `Create(byte[])` - Hash from bytes

## What's Different from Original

- ✅ .NET 8 with modern C# features
- ✅ No external dependencies
- ✅ Nullable reference types for safety
- ✅ Resource leak fixes

## Acknowledgements

Original: [Marc Clifton](https://github.com/cliftonm/MerkleTree)  
Modern .NET  Port: Sreekanth Edayar

## License

MIT - see [LICENSE](LICENSE)
