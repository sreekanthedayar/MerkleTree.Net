using System;
using System.Security.Cryptography;
using System.Text;
using Xunit;
using Clifton.Blockchain;

namespace MerkleTree.Tests
{
    public class MerkleHashTests
    {
        [Fact]
        public void CreateLeafHash_UsesRfc6962LeafDomainSeparation()
        {
            var data = Encoding.UTF8.GetBytes("rfc6962-leaf");
            var rfcInput = new byte[data.Length + 1];
            rfcInput[0] = 0x00;
            Buffer.BlockCopy(data, 0, rfcInput, 1, data.Length);

            var expected = SHA256.HashData(rfcInput);
            var actual = MerkleHash.Create(data);

            Assert.Equal(expected, actual.Value);
        }

        [Fact]
        public void CreateParentHash_UsesRfc6962NodeDomainSeparation()
        {
            var left = MerkleHash.FromDigest(new byte[Constants.HASH_LENGTH]);
            var rightDigest = new byte[Constants.HASH_LENGTH];
            Array.Fill(rightDigest, (byte)1);
            var right = MerkleHash.FromDigest(rightDigest);
            var rfcInput = new byte[1 + left.Value.Length + right.Value.Length];
            rfcInput[0] = 0x01;
            Buffer.BlockCopy(left.Value, 0, rfcInput, 1, left.Value.Length);
            Buffer.BlockCopy(right.Value, 0, rfcInput, 1 + left.Value.Length, right.Value.Length);

            var expected = SHA256.HashData(rfcInput);
            var actual = MerkleHash.Create(left, right);

            Assert.Equal(expected, actual.Value);
        }

        [Fact]
        public void Create_FromString_ShouldGenerateExpectedLengthHash()
        {
            string input = "hello world";
            MerkleHash hash = MerkleHash.Create(input);
            Assert.NotNull(hash.Value);
            Assert.Equal(Constants.HASH_LENGTH, hash.Value.Length);
        }

        [Fact]
        public void Create_FromBytes_ShouldMatchRfc6962LeafSha256()
        {
            byte[] input = System.Text.Encoding.UTF8.GetBytes("test");
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            byte[] rfcInput = new byte[input.Length + 1];
            Buffer.BlockCopy(input, 0, rfcInput, 1, input.Length);
            byte[] expected = sha256.ComputeHash(rfcInput);

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
        public void Create_WithSHA512HashAlgorithm_UsesTheAlgorithmDigestLength()
        {
            // Arrange
            byte[] input = System.Text.Encoding.UTF8.GetBytes("custom algorithm test");
            using var sha512 = System.Security.Cryptography.SHA512.Create(); // Produces a 64-byte hash

            var hash = MerkleHash.Create(input, sha512);

            Assert.Equal(64, hash.Value.Length);
        }
    }
}
