using System;
using Xunit;
using Clifton.Blockchain;

namespace MerkleTree.Tests
{
    public class MerkleHashTests
    {
        [Fact]
        public void Create_FromString_ShouldGenerateExpectedLengthHash()
        {
            string input = "hello world";
            MerkleHash hash = MerkleHash.Create(input);
            Assert.NotNull(hash.Value);
            Assert.Equal(Constants.HASH_LENGTH, hash.Value.Length);
        }

        [Fact]
        public void Create_FromBytes_ShouldMatchManualSha256()
        {
            byte[] input = System.Text.Encoding.UTF8.GetBytes("test");
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            byte[] expected = sha256.ComputeHash(input);

            MerkleHash hash = MerkleHash.Create(input);
            Assert.Equal(expected, hash.Value);
        }

        [Fact]
        public void Create_FromTwoHashes_ShouldCombineCorrectly()
        {
            var left = MerkleHash.Create("left");
            var right = MerkleHash.Create("right");
            var combined = MerkleHash.Create(left, right);

            Assert.NotNull(combined.Value);
            Assert.Equal(Constants.HASH_LENGTH, combined.Value.Length);
            Assert.NotEqual(left.Value, combined.Value);
            Assert.NotEqual(right.Value, combined.Value);
        }

        [Fact]
        public void Equals_ShouldReturnTrueForSameHash()
        {
            var hash1 = MerkleHash.Create("same");
            var hash2 = MerkleHash.Create("same");

            Assert.True(hash1.Equals(hash2));
            Assert.True(hash1 == hash2);
            Assert.False(hash1 != hash2);
        }

        [Fact]
        public void ToString_ShouldReturnHexRepresentation()
        {
            var hash = MerkleHash.Create("hex");
            string hex = hash.ToString();

            Assert.False(string.IsNullOrWhiteSpace(hex));
            Assert.Equal(Constants.HASH_LENGTH * 2, hex.Length);
        }
    }
}