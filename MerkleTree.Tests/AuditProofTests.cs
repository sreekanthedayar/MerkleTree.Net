using System.Collections.Generic;
using Xunit;
using Clifton.Blockchain;

namespace MerkleTree.Tests
{
    public class AuditProofTests
    {
        [Fact]
        public void AuditProof_ValidLeaf_ReturnsCorrectProof()
        {
            var tree = new Clifton.Blockchain.MerkleTree();
            MerkleHash l1 = MerkleHash.Create("abc");
            MerkleHash l2 = MerkleHash.Create("def");
            MerkleHash l3 = MerkleHash.Create("123");
            MerkleHash l4 = MerkleHash.Create("456");
            tree.AppendLeaves(new MerkleHash[] { l1, l2, l3, l4 });
            MerkleHash rootHash = tree.BuildTree();

            List<MerkleProofHash> auditTrail = tree.AuditProof(l1);
            Assert.True(Clifton.Blockchain.MerkleTree.VerifyAudit(rootHash, l1, auditTrail));

            auditTrail = tree.AuditProof(l2);
            Assert.True(Clifton.Blockchain.MerkleTree.VerifyAudit(rootHash, l2, auditTrail));

            auditTrail = tree.AuditProof(l3);
            Assert.True(Clifton.Blockchain.MerkleTree.VerifyAudit(rootHash, l3, auditTrail));

            auditTrail = tree.AuditProof(l4);
            Assert.True(Clifton.Blockchain.MerkleTree.VerifyAudit(rootHash, l4, auditTrail));
        }

        [Theory]
        [InlineData(4, 2)]
        [InlineData(8, 5)]
        [InlineData(16, 10)]
        [InlineData(100, 42)]
        public void VerifyAudit_MultipleScenarios_AllValid(int leafCount, int targetIndex)
        {
            var tree = new Clifton.Blockchain.MerkleTree();
            var hashes = new List<MerkleHash>();

            for (int i = 0; i < leafCount; i++)
            {
                var hash = MerkleHash.Create($"leaf{i}");
                hashes.Add(hash);
                tree.AppendLeaf(hash);
            }

            tree.BuildTree();

            var targetLeaf = hashes[targetIndex];
            var proof = tree.AuditProof(targetLeaf);
            bool isValid = Clifton.Blockchain.MerkleTree.VerifyAudit(tree.RootNode.Hash, targetLeaf, proof);

            Assert.True(isValid);
        }

        [Fact]
        public void VerifyAudit_TamperedProof_ReturnsFalse()
        {
            var tree = new Clifton.Blockchain.MerkleTree();
            tree.AppendLeaf(MerkleHash.Create("leaf1"));
            tree.AppendLeaf(MerkleHash.Create("leaf2"));
            tree.BuildTree();

            var targetLeaf = MerkleHash.Create("leaf1");
            var proof = tree.AuditProof(targetLeaf);

            // Tamper with root hash
            var tamperedRoot = MerkleHash.Create("tampered");

            bool isValid = Clifton.Blockchain.MerkleTree.VerifyAudit(tamperedRoot, targetLeaf, proof);

            Assert.False(isValid);
        }

        [Fact]
        public void AuditProof_NonExistentLeaf_ReturnsEmptyList()
        {
            var tree = new Clifton.Blockchain.MerkleTree();
            tree.AppendLeaf(MerkleHash.Create("leaf1"));
            tree.AppendLeaf(MerkleHash.Create("leaf2"));
            tree.BuildTree();

            var nonExistentLeaf = MerkleHash.Create("does-not-exist");
            var proof = tree.AuditProof(nonExistentLeaf);

            Assert.Empty(proof);
        }
    }
}