using Clifton.Blockchain;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MerkleTree.Tests
{
    public class MerkleSerializerTests
    {
        [Fact]
        public void Serialization_RoundTrip_AuditProofPackage()
        {
            // Arrange
            var tree = new Clifton.Blockchain.MerkleTree();
            var leaves = Enumerable.Range(0, 5).Select(i => MerkleHash.Create(Encoding.UTF8.GetBytes($"leaf{i}"))).ToArray();
            tree.AppendLeaves(leaves);
            var rootHash = tree.BuildTree();
            var leafToProve = leaves[2];
            var auditProof = tree.AuditProof(leafToProve);

            var originalPackage = new AuditProofPackage
            {
                Timestamp = DateTime.UtcNow,
                TreeMetadata = new AuditTreeMetadata
                {
                    RootHash = rootHash.ToHex(),
                    LeafCount = leaves.Length,
                    TreeDepth = 4, // Calculated for 5 leaves
                    HashAlgorithm = "SHA256"
                },
                Proof = new AuditProof
                {
                    LeafHash = leafToProve.ToHex(),
                    ProofPath = auditProof.Select(p => new ProofNode { Direction = p.Direction.ToString(), Hash = p.Hash.ToHex() }).ToList()
                }
            };

            // Act
            string json = MerkleSerializer.SerializeAuditProofPackage(originalPackage);
            var deserializedPackage = MerkleSerializer.DeserializeAuditProofPackage(json);

            // Assert
            Assert.NotNull(deserializedPackage);
            Assert.Equal(originalPackage.Type, deserializedPackage.Type);
            Assert.Equal(originalPackage.TreeMetadata.RootHash, deserializedPackage.TreeMetadata.RootHash);
            Assert.Equal(originalPackage.TreeMetadata.LeafCount, deserializedPackage.TreeMetadata.LeafCount);
            Assert.Equal(originalPackage.TreeMetadata.HashAlgorithm, deserializedPackage.TreeMetadata.HashAlgorithm);
            Assert.Equal(originalPackage.Proof.LeafHash, deserializedPackage.Proof.LeafHash);
            Assert.Equal(originalPackage.Proof.ProofPath.Count, deserializedPackage.Proof.ProofPath.Count);
            for (int i = 0; i < originalPackage.Proof.ProofPath.Count; i++)
            {
                Assert.Equal(originalPackage.Proof.ProofPath[i].Hash, deserializedPackage.Proof.ProofPath[i].Hash);
                Assert.Equal(originalPackage.Proof.ProofPath[i].Direction, deserializedPackage.Proof.ProofPath[i].Direction);
            }
        }

        [Fact]
        public void Serialization_RoundTrip_ConsistencyProofPackage()
        {
            // Arrange
            var tree = new Clifton.Blockchain.MerkleTree();
            var leaves1 = Enumerable.Range(0, 3).Select(i => MerkleHash.Create(Encoding.UTF8.GetBytes($"leaf{i}"))).ToArray();
            tree.AppendLeaves(leaves1);
            var oldRootHash = tree.BuildTree();
            
            // Access protected field through reflection for testing
            var leavesField = typeof(Clifton.Blockchain.MerkleTree).GetField("leaves", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (leavesField == null)
            {
                throw new InvalidOperationException("Could not find 'leaves' field via reflection");
            }
            
            var leavesListObject = leavesField.GetValue(tree);
            if (leavesListObject == null)
            {
                throw new InvalidOperationException("'leaves' field is null");
            }
            
            var leavesList = (List<MerkleNode>)leavesListObject;
            int oldLeafCount = leavesList.Count;

            var leaves2 = Enumerable.Range(3, 2).Select(i => MerkleHash.Create(Encoding.UTF8.GetBytes($"leaf{i}"))).ToArray();
            tree.AppendLeaves(leaves2);
            var newRootHash = tree.BuildTree();
            int newLeafCount = leavesList.Count;

            var consistencyProof = tree.ConsistencyProof(oldLeafCount);

            var originalPackage = new ConsistencyProofPackage
            {
                Timestamp = DateTime.UtcNow,
                TreeMetadata = new ConsistencyTreeMetadata
                {
                    OldRootHash = oldRootHash.ToHex(),
                    NewRootHash = newRootHash.ToHex(),
                    OldLeafCount = oldLeafCount,
                    NewLeafCount = newLeafCount,
                    HashAlgorithm = "SHA256"
                },
                Proof = new ConsistencyProof
                {
                    ProofPath = consistencyProof.Select(p => new ProofNode { Direction = p.Direction.ToString(), Hash = p.Hash.ToHex() }).ToList()
                }
            };

            // Act
            string json = MerkleSerializer.SerializeConsistencyProofPackage(originalPackage);
            var deserializedPackage = MerkleSerializer.DeserializeConsistencyProofPackage(json);

            // Assert
            Assert.NotNull(deserializedPackage);
            Assert.Equal(originalPackage.Type, deserializedPackage.Type);
            Assert.Equal(originalPackage.TreeMetadata.OldRootHash, deserializedPackage.TreeMetadata.OldRootHash);
            Assert.Equal(originalPackage.TreeMetadata.NewRootHash, deserializedPackage.TreeMetadata.NewRootHash);
            Assert.Equal(originalPackage.TreeMetadata.OldLeafCount, deserializedPackage.TreeMetadata.OldLeafCount);
            Assert.Equal(originalPackage.TreeMetadata.NewLeafCount, deserializedPackage.TreeMetadata.NewLeafCount);
            Assert.Equal(originalPackage.Proof.ProofPath.Count, deserializedPackage.Proof.ProofPath.Count);
        }

        [Fact]
        public void Deserialization_MalformedJson_ThrowsException()
        {
            // Arrange
            string malformedJson1 = "{\"type\": \"merkle_audit_proof\", \"proof\": {}}"; // Missing metadata
            string malformedJson2 = "{\"type\": \"merkle_consistency_proof\", \"proof\": null}"; // Null proof
            string invalidJson = "this is not json";

            // Act & Assert
            Assert.Throws<MerkleException>(() =>
            {
                MerkleSerializer.DeserializeAuditProofPackage(malformedJson1);
            });
            Assert.Throws<MerkleException>(() =>
            {
                MerkleSerializer.DeserializeConsistencyProofPackage(malformedJson2);
            });
            Assert.Throws<MerkleException>(() => {
                MerkleSerializer.DeserializeAuditProofPackage(invalidJson);
            });
        }

        [Fact]
        public void Serialization_EdgeCases_HandlesGracefully()
        {
            // Arrange
            var emptyAuditPackage = new AuditProofPackage
            {
                Timestamp = DateTime.UtcNow,
                TreeMetadata = new AuditTreeMetadata { RootHash = "root", LeafCount = 0, TreeDepth = 0, HashAlgorithm = "SHA256" },
                Proof = new AuditProof { LeafHash = "leaf", ProofPath = new List<ProofNode>() } // Empty proof path
            };

            // Act
            string json = MerkleSerializer.SerializeAuditProofPackage(emptyAuditPackage);
            var deserializedPackage = MerkleSerializer.DeserializeAuditProofPackage(json);

            // Assert
            Assert.NotNull(deserializedPackage);
            Assert.Empty(deserializedPackage.Proof.ProofPath);
            Assert.Contains("\"proofPath\":[]", json.Replace(" ", ""));
        }
    }
}