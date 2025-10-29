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

        [Fact]
        public void FromHex_ToHex_RoundTrip()
        {
            // Arrange
            var originalHash = MerkleHash.Create("round-trip test");
            
            // Act
            string hex = originalHash.ToHex();
            var finalHash = MerkleHash.FromHex(hex);

            // Assert
            Assert.Equal(originalHash, finalHash);
        }

        [Theory]
        [InlineData("not-a-hex-string")]
        [InlineData("12345")] // Odd length
        [InlineData("gg")] // Invalid characters
        public void FromHex_InvalidString_ThrowsException(string invalidHex)
        {
            // Act & Assert
            Assert.Throws<MerkleException>(() => MerkleHash.FromHex(invalidHex));
        }

        [Fact]
        public void Create_WithNon32ByteHashAlgorithm_ThrowsException()
        {
            // Arrange
            byte[] input = System.Text.Encoding.UTF8.GetBytes("custom algorithm test");
            using var sha512 = System.Security.Cryptography.SHA512.Create(); // Produces a 64-byte hash

            // Act & Assert
            // MerkleHash.Create calls SetHash, which enforces a 32-byte hash length via Constants.HASH_LENGTH
            var ex = Assert.Throws<MerkleException>(() => MerkleHash.Create(input, sha512));
            Assert.Contains("Unexpected hash length", ex.Message);
        }
    }
}