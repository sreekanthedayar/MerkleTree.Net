using System.Text;
using Xunit;
using Clifton.Blockchain;

namespace MerkleTree.Tests
{
    public class MerkleNodeTests
    {
        [Fact]
        public void HashesAreSameTest()
        {
            MerkleHash h1 = MerkleHash.Create("abc");
            MerkleHash h2 = MerkleHash.Create("abc");
            Assert.True(h1 == h2);
        }

        [Fact]
        public void CreateNodeTest()
        {
            MerkleNode node = new MerkleNode();
            Assert.Null(node.Parent);
            Assert.Null(node.LeftNode);
            Assert.Null(node.RightNode);
        }

        [Fact]
        public void LeftRightHashVerificationTest()
        {
            // Arrange
            var leftNode = new MerkleNode(MerkleHash.Create("abc"));
            var rightNode = new MerkleNode(MerkleHash.Create("def"));
            var parentNode = new MerkleNode(leftNode, rightNode);
            // Act & Assert
            Assert.True(parentNode.VerifyHash());
        }

        [Fact]
        public void NodesEqualTest()
        {
            MerkleNode parentNode1 = CreateParentNode("abc", "def");
            var leftNode = new MerkleNode(MerkleHash.Create("abc"));
            var rightNode = new MerkleNode(MerkleHash.Create("def"));
            var parentNode2 = new MerkleNode(leftNode, rightNode);

            Assert.True(parentNode1.Equals(parentNode2));
            Assert.Equal(parentNode1.Hash, parentNode2.Hash);
        }

        [Fact]
        public void NodesNotEqualTest()
        {
            MerkleNode parentNode1 = CreateParentNode("abc", "def");
            MerkleNode parentNode2 = CreateParentNode("def", "abc");
            Assert.False(parentNode1.Equals(parentNode2));
        }

        [Fact]
        public void VerifyTwoLevelTree()
        {
            MerkleNode parentNode1 = CreateParentNode("abc", "def");
            MerkleNode parentNode2 = CreateParentNode("123", "456"); 
            MerkleNode rootNode = new MerkleNode(parentNode1, parentNode2);
            Assert.True(rootNode.VerifyHash());
        }

        private MerkleNode CreateParentNode(string leftData, string rightData)
        {
            var leftNode = new MerkleNode(MerkleHash.Create(leftData));
            var rightNode = new MerkleNode(MerkleHash.Create(rightData));
            return new MerkleNode(leftNode, rightNode);
        }

        [Fact]
        public void CreateNode_WithNon32ByteHashAlgorithm_ThrowsException()
        {
            // Arrange
            using var sha512 = System.Security.Cryptography.SHA512.Create();
            var leftNode = new MerkleNode(MerkleHash.Create("left")); // Uses default SHA256
            var rightNode = new MerkleNode(MerkleHash.Create("right"));

            // Act
            // MerkleNode constructor calls MerkleHash.Create, which enforces a 32-byte hash length
            var ex = Assert.Throws<MerkleException>(() => new MerkleNode(leftNode, rightNode, sha512));

            // Assert
            Assert.Contains("Unexpected hash length", ex.Message);
        }
    }
}