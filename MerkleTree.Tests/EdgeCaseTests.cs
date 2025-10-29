using System;
using System.Collections.Generic;
using Xunit;
using Clifton.Blockchain;

namespace MerkleTree.Tests
{
    public class EdgeCaseTests
    {
        [Fact]
        public void BuildTree_EmptyTree_ThrowsException()
        {
            var tree = new Clifton.Blockchain.MerkleTree();
            
            var ex = Assert.Throws<MerkleException>(() => tree.BuildTree());
            Assert.Equal("Cannot build a tree with no leaves.", ex.Message);
        }

        [Fact]
        public void BuildTree_SingleLeaf_RootIsLeaf()
        {
            var tree = new Clifton.Blockchain.MerkleTree();
            var hash = MerkleHash.Create("single");
            tree.AppendLeaf(hash);
            tree.BuildTree();
            
            Assert.Equal(hash, tree.RootNode.Hash);
        }

        [Fact]
        public void BuildTree_1000Leaves_Succeeds()
        {
            var tree = new Clifton.Blockchain.MerkleTree();
            var hashes = new List<MerkleHash>();
            
            for (int i = 0; i < 1000; i++)
            {
                var hash = MerkleHash.Create($"leaf{i}");
                hashes.Add(hash);
                tree.AppendLeaf(hash);
            }
            
            tree.BuildTree();
            Assert.NotNull(tree.RootNode);
            
            // Verify audit proofs still work
            var randomLeaf = hashes[Random.Shared.Next(1000)];
            var proof = tree.AuditProof(randomLeaf);
            Assert.True(Clifton.Blockchain.MerkleTree.VerifyAudit(tree.RootNode.Hash, randomLeaf, proof));
        }

        [Fact]
        public void MerkleHash_EmptyByteArray_CreatesValidHash()
        {
            var hash = MerkleHash.Create(new byte[0]);
            
            Assert.NotNull(hash);
            Assert.NotNull(hash.Value);
            Assert.Equal(Constants.HASH_LENGTH, hash.Value.Length);
        }

        [Fact]
        public void MerkleHash_EmptyString_CreatesValidHash()
        {
            var hash = MerkleHash.Create("");
            
            Assert.NotNull(hash);
            Assert.NotNull(hash.Value);
            Assert.Equal(Constants.HASH_LENGTH, hash.Value.Length);
        }

        [Fact]
        public void MerkleHash_LargeData_CreatesValidHash()
        {
            var largeData = new string('x', 10000);
            var hash = MerkleHash.Create(largeData);
            
            Assert.NotNull(hash);
            Assert.Equal(Constants.HASH_LENGTH, hash.Value.Length);
        }

        [Fact]
        public void MerkleHash_SHA256Disposal_NoMemoryLeak()
        {
            var iterations = 1000;
            var before = GC.GetTotalMemory(true);
            
            for (int i = 0; i < iterations; i++)
            {
                MerkleHash.Create($"test{i}");
            }
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var after = GC.GetTotalMemory(true);
            var growth = after - before;
            
            Assert.True(growth < 1_000_000, $"Memory leaked: {growth} bytes");
        }

        [Fact]
        public void SetHash_InvalidLength_ThrowsException()
        {
            // MerkleHash.Create() always produces valid 32-byte hash
            // Test SetHash directly with invalid length
            var merkleHash = MerkleHash.Create("test");
            var invalidHash = new byte[16]; // Wrong length
            
            var ex = Assert.Throws<MerkleException>(() => merkleHash.SetHash(invalidHash));
            Assert.Contains("Unexpected hash length", ex.Message);
        }

        [Fact]
        public void BuildTree_OddNumberOfLeaves_HandlesGracefully()
        {
            var tree = new Clifton.Blockchain.MerkleTree();
            
            for (int i = 1; i <= 7; i++)
            {
                tree.AppendLeaf(MerkleHash.Create($"leaf{i}"));
            }
            
            tree.BuildTree();
            
            Assert.NotNull(tree.RootNode);
            
            // Verify all leaves can be audited
            for (int i = 1; i <= 7; i++)
            {
                var leafHash = MerkleHash.Create($"leaf{i}");
                var proof = tree.AuditProof(leafHash);
                Assert.True(Clifton.Blockchain.MerkleTree.VerifyAudit(tree.RootNode.Hash, leafHash, proof));
            }
        }
    }
}