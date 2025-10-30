using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Xunit;
using Clifton.Blockchain;

namespace MerkleTree.Tests
{
    public class ConsistencyProofTests
    {
        private bool VerifyConsistencyManually(MerkleHash oldRootHash, List<MerkleProofHash> proof, HashAlgorithm hashAlgorithm)
        {
            if (proof == null || proof.Count == 0)
            {
                return false; // Or handle as per specification, maybe true if oldRoot is also empty/null
            }

            MerkleHash computedHash;
            // If there's only one hash in the proof, it should be the old root itself.
            if (proof.Count == 1)
            {
                return proof[0].Hash == oldRootHash;
            }
            
            int hidx = proof.Count - 1;
            computedHash = MerkleHash.Create(proof[hidx - 1].Hash, proof[hidx].Hash, hashAlgorithm);
            hidx -= 2;
            while (hidx >= 0)
            {
                computedHash = MerkleHash.Create(proof[hidx].Hash, computedHash, hashAlgorithm);
                --hidx;
            }

            return computedHash == oldRootHash;
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
                    bool isValid = VerifyConsistencyManually(oldRootHash, proof, tree.HashAlgorithm);
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
            bool isValid = VerifyConsistencyManually(oldRoot, proof, newTree.HashAlgorithm);

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
            bool isValid = VerifyConsistencyManually(oldRoot, proof, tree2.HashAlgorithm);

            Assert.True(isValid);
        }
    }
}