using System.Collections.Generic;
using System.Text;
using Xunit;
using Clifton.Blockchain;

namespace MerkleTree.Tests
{
    public class MerkleTreeTests
    {
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