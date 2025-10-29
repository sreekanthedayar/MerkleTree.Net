using System.Collections.Generic;
using Xunit;
using Clifton.Blockchain;

namespace MerkleTree.Tests
{
    public class ConsistencyProofTests
    {
        [Fact]
        public void ConsistencyTest()
        {
            // Start with a tree with 2 leaves:
            var tree = new Clifton.Blockchain.MerkleTree();
            tree.AppendLeaf(MerkleHash.Create("1"));
            tree.AppendLeaf(MerkleHash.Create("2"));

            MerkleHash firstRoot = tree.BuildTree();

            List<MerkleHash> oldRoots = new List<MerkleHash>() { firstRoot };

            // Add a new leaf and verify that each time we add a leaf, we can get a consistency check
            // for all the previous leaves.
            for (int i = 2; i < 100; i++)
            {
                tree.AppendLeaf(MerkleHash.Create(i.ToString()));
                tree.BuildTree();

                // After adding a leaf, verify that all the old root hashes exist.
                for (int n = 0; n < oldRoots.Count; n++)
                {
                    var oldRootHash = oldRoots[n];
                    List<MerkleProofHash> proof = tree.ConsistencyProof(n + 2);
                    MerkleHash hash, lhash, rhash;

                    if (proof.Count > 1)
                    {
                        lhash = proof[proof.Count - 2].Hash;
                        int hidx = proof.Count - 1;
                        hash = rhash = Clifton.Blockchain.MerkleTree.ComputeHash(lhash, proof[hidx].Hash);
                        hidx -= 2;

                        while (hidx >= 0)
                        {
                            lhash = proof[hidx].Hash;
                            hash = rhash = Clifton.Blockchain.MerkleTree.ComputeHash(lhash, rhash);

                            --hidx;
                        }
                    }
                    else
                    {
                        hash = proof[0].Hash;
                    }

                    Assert.True(hash == oldRootHash, "Old root hash not found for index " + i + " m = " + (n + 2).ToString());
                }

                // Then we add this root hash as the next old root hash to check.
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
            bool isValid = Clifton.Blockchain.MerkleTree.VerifyConsistency(oldRoot, proof);

            Assert.True(isValid);
        }

        [Fact]
        public void ConsistencyProof_TreeGrowth_VerifiesCorrectly()
        {
            // Build tree with 4 leaves
            var tree1 = new Clifton.Blockchain.MerkleTree();
            tree1.AppendLeaf(MerkleHash.Create("1"));
            tree1.AppendLeaf(MerkleHash.Create("2"));
            tree1.AppendLeaf(MerkleHash.Create("3"));
            tree1.AppendLeaf(MerkleHash.Create("4"));
            var oldRoot = tree1.BuildTree();

            // Grow tree to 8 leaves
            var tree2 = new Clifton.Blockchain.MerkleTree();
            tree2.AppendLeaf(MerkleHash.Create("1"));
            tree2.AppendLeaf(MerkleHash.Create("2"));
            tree2.AppendLeaf(MerkleHash.Create("3"));
            tree2.AppendLeaf(MerkleHash.Create("4"));
            tree2.AppendLeaf(MerkleHash.Create("5"));
            tree2.AppendLeaf(MerkleHash.Create("6"));
            tree2.AppendLeaf(MerkleHash.Create("7"));
            tree2.AppendLeaf(MerkleHash.Create("8"));
            tree2.BuildTree();

            // Get consistency proof
            var proof = tree2.ConsistencyProof(4);

            // Verify
            bool isValid = Clifton.Blockchain.MerkleTree.VerifyConsistency(oldRoot, proof);

            Assert.True(isValid);
        }
    }
}