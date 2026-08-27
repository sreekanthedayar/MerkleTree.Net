using System.Collections.Generic;
using Xunit;
using Clifton.Blockchain;

namespace MerkleTree.Tests
{
    public class ConsistencyProofTests
    {
        [Fact]
        public void VerifyConsistency_OneElementOldRootProof_DoesNotProveAnExtension()
        {
            var oldRoot = MerkleHash.Create("old-root");
            var newRoot = MerkleHash.Create("new-root");
            var proof = new List<MerkleProofHash>
            {
                new MerkleProofHash(oldRoot, MerkleProofHash.Branch.Consistency)
            };

            Assert.False(Clifton.Blockchain.MerkleTree.VerifyConsistency(
                oldRoot,
                newRoot,
                4,
                8,
                proof));
        }

        [Fact]
        public void ConsistencyTest()
        {
            var tree = new Clifton.Blockchain.MerkleTree();
            tree.AppendLeaf(MerkleHash.Create("1"));
            tree.AppendLeaf(MerkleHash.Create("2"));
            MerkleHash firstRoot = tree.BuildTree();
            var oldRoots = new List<MerkleHash>() { firstRoot };

            for (int i = 2; i < 30; i++) // Reduced for performance
            {
                tree.AppendLeaf(MerkleHash.Create(i.ToString()));
                tree.BuildTree();

                for (int n = 0; n < oldRoots.Count; n++)
                {
                    var oldRootHash = oldRoots[n];
                    var proof = tree.ConsistencyProof(n + 2);
                    bool isValid = Clifton.Blockchain.MerkleTree.VerifyConsistency(
                        oldRootHash,
                        tree.RootNode.Hash,
                        n + 2,
                        i + 1,
                        proof);
                    Assert.True(isValid, $"Consistency failed for new tree size {i + 1} against old tree size {n + 2}. Old root: {oldRootHash}");
                }

                oldRoots.Add(tree.RootNode.Hash);
            }
        }

        [Theory]
        [InlineData(2, 4)]
        [InlineData(4, 8)]
        [InlineData(7, 15)]
        [InlineData(3, 7)]
        [InlineData(5, 10)]
        public void ConsistencyProof_VariousSizes_Verifies(int oldSize, int newSize)
        {
            var oldTree = new Clifton.Blockchain.MerkleTree();
            for (int i = 0; i < oldSize; i++)
                oldTree.AppendLeaf(MerkleHash.Create($"leaf{i}"));
            var oldRoot = oldTree.BuildTree();

            var newTree = new Clifton.Blockchain.MerkleTree();
            for (int i = 0; i < newSize; i++)
                newTree.AppendLeaf(MerkleHash.Create($"leaf{i}"));
            newTree.BuildTree();

            var proof = newTree.ConsistencyProof(oldSize);
            bool isValid = Clifton.Blockchain.MerkleTree.VerifyConsistency(
                oldRoot,
                newTree.RootNode.Hash,
                oldSize,
                newSize,
                proof);

            Assert.True(isValid);
        }

        [Fact]
        public void ConsistencyProof_TreeGrowth_VerifiesCorrectly()
        {
            var tree1 = new Clifton.Blockchain.MerkleTree();
            tree1.AppendLeaf(MerkleHash.Create("1"));
            tree1.AppendLeaf(MerkleHash.Create("2"));
            tree1.AppendLeaf(MerkleHash.Create("3"));
            tree1.AppendLeaf(MerkleHash.Create("4"));
            var oldRoot = tree1.BuildTree();

            var tree2 = new Clifton.Blockchain.MerkleTree();
            for (int i = 1; i <= 8; i++)
                tree2.AppendLeaf(MerkleHash.Create(i.ToString()));
            tree2.BuildTree();

            var proof = tree2.ConsistencyProof(4);
            bool isValid = Clifton.Blockchain.MerkleTree.VerifyConsistency(
                oldRoot,
                tree2.RootNode.Hash,
                4,
                8,
                proof);

            Assert.True(isValid);
        }

        [Fact]
        public void VerifyConsistency_TamperedNewRoot_IsRejected()
        {
            var oldTree = new Clifton.Blockchain.MerkleTree();
            for (int i = 0; i < 4; i++)
                oldTree.AppendLeaf(MerkleHash.Create($"leaf{i}"));
            var oldRoot = oldTree.BuildTree();

            var newTree = new Clifton.Blockchain.MerkleTree();
            for (int i = 0; i < 8; i++)
                newTree.AppendLeaf(MerkleHash.Create($"leaf{i}"));
            var newRoot = newTree.BuildTree();
            var proof = newTree.ConsistencyProof(4);

            Assert.False(Clifton.Blockchain.MerkleTree.VerifyConsistency(
                oldRoot,
                MerkleHash.Create("tampered-new-root"),
                4,
                8,
                proof));
            Assert.True(Clifton.Blockchain.MerkleTree.VerifyConsistency(
                oldRoot,
                newRoot,
                4,
                8,
                proof));
        }

        [Fact]
        public void VerifyConsistency_InvalidSizesAndIncompleteProofs_AreRejected()
        {
            var oldTree = new Clifton.Blockchain.MerkleTree();
            for (int i = 0; i < 4; i++)
                oldTree.AppendLeaf(MerkleHash.Create($"leaf{i}"));
            var oldRoot = oldTree.BuildTree();

            var newTree = new Clifton.Blockchain.MerkleTree();
            for (int i = 0; i < 8; i++)
                newTree.AppendLeaf(MerkleHash.Create($"leaf{i}"));
            var newRoot = newTree.BuildTree();
            var proof = newTree.ConsistencyProof(4);

            Assert.False(Clifton.Blockchain.MerkleTree.VerifyConsistency(oldRoot, newRoot, 0, 8, proof));
            Assert.False(Clifton.Blockchain.MerkleTree.VerifyConsistency(oldRoot, newRoot, 8, 4, proof));
            Assert.False(Clifton.Blockchain.MerkleTree.VerifyConsistency(oldRoot, newRoot, 4, 8, new List<MerkleProofHash>()));
        }

        [Fact]
        public void VerifyConsistency_NullProofHash_ReturnsFalse()
        {
            var oldTree = new Clifton.Blockchain.MerkleTree();
            for (int i = 0; i < 4; i++)
                oldTree.AppendLeaf(MerkleHash.Create($"leaf{i}"));
            var oldRoot = oldTree.BuildTree();

            var newTree = new Clifton.Blockchain.MerkleTree();
            for (int i = 0; i < 8; i++)
                newTree.AppendLeaf(MerkleHash.Create($"leaf{i}"));
            var newRoot = newTree.BuildTree();
            var proof = newTree.ConsistencyProof(4);
            proof[0] = new MerkleProofHash(null!, proof[0].Direction);

            Assert.False(Clifton.Blockchain.MerkleTree.VerifyConsistency(
                oldRoot,
                newRoot,
                4,
                8,
                proof));
        }

        [Fact]
        public void VerifyConsistency_SameTreeSizeRequiresMatchingRootsAndNoProof()
        {
            var tree = new Clifton.Blockchain.MerkleTree();
            tree.AppendLeaf(MerkleHash.Create("leaf0"));
            tree.AppendLeaf(MerkleHash.Create("leaf1"));
            var root = tree.BuildTree();

            Assert.True(Clifton.Blockchain.MerkleTree.VerifyConsistency(
                root,
                root,
                2,
                2,
                new List<MerkleProofHash>()));
            Assert.False(Clifton.Blockchain.MerkleTree.VerifyConsistency(
                root,
                MerkleHash.Create("different-root"),
                2,
                2,
                new List<MerkleProofHash>()));
        }

        [Fact]
        public void ConsistencyProof_AfterAppendingWithoutRebuild_IsRejected()
        {
            var tree = new Clifton.Blockchain.MerkleTree();
            tree.AppendLeaf(MerkleHash.Create("leaf1"));
            tree.AppendLeaf(MerkleHash.Create("leaf2"));
            tree.BuildTree();
            tree.AppendLeaf(MerkleHash.Create("leaf3"));

            Assert.Throws<MerkleException>(() => tree.ConsistencyProof(2));
        }

        [Fact]
        public void ConsistencyAuditProof_WithDuplicateNodeHash_DoesNotThrow()
        {
            var tree = new Clifton.Blockchain.MerkleTree();
            var repeatedHash = MerkleHash.Create("repeated-leaf");
            tree.AppendLeaf(repeatedHash);
            tree.AppendLeaf(repeatedHash);
            tree.AppendLeaf(repeatedHash);
            tree.AppendLeaf(repeatedHash);
            tree.BuildTree();

            var proof = tree.ConsistencyAuditProof(repeatedHash);

            Assert.NotNull(proof);
        }
    }
}
