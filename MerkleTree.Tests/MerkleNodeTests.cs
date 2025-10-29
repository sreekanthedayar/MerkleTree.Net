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
        public void LeftHashVerificationTest()
        {
            MerkleNode parentNode = new MerkleNode();
            MerkleNode leftNode = new MerkleNode();
            leftNode.ComputeHash(Encoding.UTF8.GetBytes("abc"));
            parentNode.SetLeftNode(leftNode);
            Assert.True(parentNode.VerifyHash());
        }

        [Fact]
        public void LeftRightHashVerificationTest()
        {
            MerkleNode parentNode = CreateParentNode("abc", "def");
            Assert.True(parentNode.VerifyHash());
        }

        [Fact]
        public void NodesEqualTest()
        {
            MerkleNode parentNode1 = CreateParentNode("abc", "def");
            MerkleNode parentNode2 = CreateParentNode("abc", "def");
            Assert.True(parentNode1.Equals(parentNode2));
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
            MerkleNode rootNode = new MerkleNode();
            rootNode.SetLeftNode(parentNode1);
            rootNode.SetRightNode(parentNode2);
            Assert.True(rootNode.VerifyHash());
        }

        private MerkleNode CreateParentNode(string leftData, string rightData)
        {
            MerkleNode parentNode = new MerkleNode();
            MerkleNode leftNode = new MerkleNode();
            MerkleNode rightNode = new MerkleNode();
            leftNode.ComputeHash(Encoding.UTF8.GetBytes(leftData));
            rightNode.ComputeHash(Encoding.UTF8.GetBytes(rightData));
            parentNode.SetLeftNode(leftNode);
            parentNode.SetRightNode(rightNode);

            return parentNode;
        }
    }
}