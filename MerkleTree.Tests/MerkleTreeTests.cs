using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Xunit;
using Clifton.Blockchain;

namespace MerkleTree.Tests
{
    public class MerkleTreeTests
    {
        [Fact]
        public void AddLeaf_WithoutAutoHash_UsesProvidedDigestAsLeaf()
        {
            var tree = new Clifton.Blockchain.MerkleTree();
            var digest = SHA256.HashData(Encoding.UTF8.GetBytes("precomputed-leaf"));

            tree.AddLeaf(digest, autoHash: false);
            var root = tree.BuildTree();

            Assert.Equal(digest, root.Value);
        }

        [Fact]
        public void CustomHashAlgorithm_SHA512_IsSupported()
        {
            using var sha512 = SHA512.Create();
            var tree = new Clifton.Blockchain.MerkleTree(sha512);

            tree.AddLeaf(Encoding.UTF8.GetBytes("sha512-leaf"), autoHash: true);
            var root = tree.BuildTree();

            Assert.Equal(64, root.Value.Length);
        }

        [Fact]
        public void VerifyHash_UsesTheConfiguredCustomAlgorithm()
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("test-key"));
            var tree = new Clifton.Blockchain.MerkleTree(hmac);
            tree.AddLeaf(Encoding.UTF8.GetBytes("left"), autoHash: true);
            tree.AddLeaf(Encoding.UTF8.GetBytes("right"), autoHash: true);
            tree.BuildTree();

            Assert.True(tree.RootNode.VerifyHash());
        }

        [Fact]
        public void AddTree_DoesNotReparentSourceLeaves()
        {
            var source = new Clifton.Blockchain.MerkleTree();
            var sourceLeaf1 = source.AppendLeaf(MerkleHash.Create("source-1"));
            var sourceLeaf2 = source.AppendLeaf(MerkleHash.Create("source-2"));
            var sourceRoot = source.BuildTree();

            var destination = new Clifton.Blockchain.MerkleTree();
            destination.AppendLeaves(new[]
            {
                MerkleHash.Create("destination-1"),
                MerkleHash.Create("destination-2")
            });
            destination.BuildTree();

            destination.AddTree(source);

            Assert.Same(source.RootNode, sourceLeaf1.Parent);
            Assert.Same(source.RootNode, sourceLeaf2.Parent);
            Assert.Equal(sourceRoot, source.RootNode.Hash);
        }

        [Fact]
        public void AppendLeaf_WithNodeFromAnotherTree_DoesNotReparentSourceLeaf()
        {
            var source = new Clifton.Blockchain.MerkleTree();
            var sourceLeaf = source.AppendLeaf(MerkleHash.Create("source-leaf"));
            source.AppendLeaf(MerkleHash.Create("source-sibling"));
            source.BuildTree();
            var sourceParent = sourceLeaf.Parent;

            var destination = new Clifton.Blockchain.MerkleTree();
            destination.AppendLeaf(sourceLeaf);
            destination.AppendLeaf(MerkleHash.Create("destination-sibling"));
            destination.BuildTree();

            Assert.Same(sourceParent, sourceLeaf.Parent);
        }

        [Fact]
        public void AddTree_PreservesSourceAuditProofs()
        {
            var source = new Clifton.Blockchain.MerkleTree();
            var sourceLeaf = source.AppendLeaf(MerkleHash.Create("source-leaf"));
            source.AppendLeaf(MerkleHash.Create("source-sibling"));
            var sourceRoot = source.BuildTree();

            var destination = new Clifton.Blockchain.MerkleTree();
            destination.AppendLeaf(MerkleHash.Create("destination-leaf"));
            destination.BuildTree();
            destination.AddTree(source);

            var proof = source.AuditProof(sourceLeaf.Hash);

            Assert.True(source.VerifyAuditWithAlgorithm(sourceRoot, sourceLeaf.Hash, proof));
        }

        [Fact]
        public void BuildTree_WithSingleLeaf_ShouldSetRootCorrectly()
        {
            var tree = new Clifton.Blockchain.MerkleTree();
            var hash = MerkleHash.Create("leaf1");
            tree.AppendLeaf(hash);
            tree.BuildTree();

            Assert.NotNull(tree.RootNode);
            Assert.Equal(hash, tree.RootNode.Hash);
        }

        [Fact]
        public void BuildTree_WithMultipleLeaves_ShouldGenerateValidRoot()
        {
            var tree = new Clifton.Blockchain.MerkleTree();
            tree.AppendLeaf(MerkleHash.Create("a"));
            tree.AppendLeaf(MerkleHash.Create("b"));
            tree.AppendLeaf(MerkleHash.Create("c"));
            tree.AppendLeaf(MerkleHash.Create("d"));
            tree.BuildTree();

            Assert.NotNull(tree.RootNode);
            Assert.Equal(Constants.HASH_LENGTH, tree.RootNode.Hash.Value.Length);
        }

        [Fact]
        public void BuildTree_WithEmptyList_ShouldThrowException()
        {
            var tree = new Clifton.Blockchain.MerkleTree();
            var ex = Assert.Throws<MerkleException>(() => tree.BuildTree());
            Assert.Equal("Cannot build a tree with no leaves.", ex.Message);
        }

        [Fact]
        public void RootHash_ShouldChange_WhenLeafDataChanges()
        {
            var tree1 = new Clifton.Blockchain.MerkleTree();
            tree1.AppendLeaf(MerkleHash.Create("a"));
            tree1.AppendLeaf(MerkleHash.Create("b"));
            tree1.AppendLeaf(MerkleHash.Create("c"));
            var root1 = tree1.BuildTree();

            var tree2 = new Clifton.Blockchain.MerkleTree();
            tree2.AppendLeaf(MerkleHash.Create("a"));
            tree2.AppendLeaf(MerkleHash.Create("b"));
            tree2.AppendLeaf(MerkleHash.Create("x"));
            var root2 = tree2.BuildTree();

            Assert.NotEqual(root1.Value, root2.Value);
        }

        [Fact]
        public void BuildTree_TwoLeaves_CreatesParent()
        {
            var tree = new Clifton.Blockchain.MerkleTree();
            tree.AppendLeaf(MerkleHash.Create("leaf1"));
            tree.AppendLeaf(MerkleHash.Create("leaf2"));
            tree.BuildTree();

            Assert.NotNull(tree.RootNode);
            Assert.NotNull(tree.RootNode.LeftNode);
            Assert.NotNull(tree.RootNode.RightNode);
        }

        [Theory]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(7)]
        [InlineData(8)]
        [InlineData(15)]
        [InlineData(16)]
        [InlineData(100)]
        public void BuildTree_VariousLeafCounts_Succeeds(int leafCount)
        {
            var tree = new Clifton.Blockchain.MerkleTree();

            for (int i = 0; i < leafCount; i++)
            {
                tree.AppendLeaf(MerkleHash.Create($"leaf{i}"));
            }

            tree.BuildTree();

            Assert.NotNull(tree.RootNode);
        }

        [Fact]
        public void BuildTree_OddNumberOfLeaves_HandlesCorrectly()
        {
            var tree = new Clifton.Blockchain.MerkleTree();
            tree.AppendLeaf(MerkleHash.Create("1"));
            tree.AppendLeaf(MerkleHash.Create("2"));
            tree.AppendLeaf(MerkleHash.Create("3"));
            tree.BuildTree();

            Assert.NotNull(tree.RootNode);
        }
    }
}
