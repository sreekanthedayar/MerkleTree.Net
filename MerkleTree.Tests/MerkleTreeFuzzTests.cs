using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Clifton.Blockchain;
using Xunit;

namespace MerkleTree.Tests
{
    /// <summary>
    /// Contains in-process mutation fuzz tests that run within the standard xUnit test runner.
    /// This approach does not require any external fuzzing engines or dependencies like WSL.
    /// </summary>
    public class MerkleTreeFuzzTests
    {
        private const int FuzzIterations = 10000; // Number of mutations to test per seed.

        /// <summary>
        /// Fuzzes the MerkleHash.Create(string) method with random string inputs.
        /// </summary>
        [Fact]
        public void Fuzz_MerkleHashCreate_RandomStrings_NoCrash()
        {
            var random = new Random();
            for (int i = 0; i < FuzzIterations; i++)
            {
                string randomString = GenerateRandomString(random.Next(256));
                try
                {
                    MerkleHash.Create(randomString);
                }
                catch (Exception ex) when (ex is not MerkleException)
                {
                    // We expect MerkleException for some inputs, but any other exception is a potential bug.
                    Assert.Fail($"Fuzz test failed with an unexpected exception. Input: '{randomString}'. Exception: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Fuzzes the MerkleSerializer.DeserializeAuditProofPackage method by mutating a valid JSON input.
        /// </summary>
        [Fact]
        public void Fuzz_DeserializeAuditProof_Mutations_NoCrash()
        {
            // A valid JSON structure to use as a starting point for mutations.
            string seedInput = @"{
                ""version"": ""1.0"",
                ""type"": ""merkle_audit_proof"",
                ""timestamp"": ""2025-10-29T18:00:00Z"",
                ""treeMetadata"": {
                    ""rootHash"": ""11223344556677889900aabbccddeeff11223344556677889900aabbccddeeff"",
                    ""leafCount"": 2,
                    ""treeDepth"": 2,
                    ""hashAlgorithm"": ""SHA256""
                },
                ""proof"": {
                    ""leafHash"": ""abcdefabcdefabcdefabcdefabcdefabcdefabcdefabcdefabcdefabcdefabcd"",
                    ""proofPath"": [
                        { ""direction"": ""Left"", ""hash"": ""0000000000000000000000000000000000000000000000000000000000000000"" }
                    ]
                }
            }";

            var mutator = new Mutator(seedInput);

            for (int i = 0; i < FuzzIterations; i++)
            {
                string mutatedJson = mutator.GetMutatedString();
                try
                {
                    MerkleSerializer.DeserializeAuditProofPackage(mutatedJson);
                }
                // We expect MerkleException or JsonException for malformed data.
                // We catch them so the test can continue, but we fail the test for any other exception type,
                // as that would indicate a potential crash or unhandled bug.
                catch (Exception ex) when (ex is MerkleException || ex is JsonException)
                {
                    // This is an expected failure from a mutated input. Continue to the next iteration.
                }
            }
        }

        /// <summary>
        /// Fuzzes the MerkleSerializer.DeserializeConsistencyProofPackage method by mutating a valid JSON input.
        /// </summary>
        [Fact]
        public void Fuzz_DeserializeConsistencyProof_Mutations_NoCrash()
        {
            // A valid JSON structure to use as a starting point for mutations.
            string seedInput = @"{
                ""version"": ""1.0"",
                ""type"": ""merkle_consistency_proof"",
                ""timestamp"": ""2025-10-29T18:00:00Z"",
                ""treeMetadata"": {
                    ""oldRootHash"": ""11223344556677889900aabbccddeeff11223344556677889900aabbccddeeff"",
                    ""newRootHash"": ""abcdefabcdefabcdefabcdefabcdefabcdefabcdefabcdefabcdefabcdefabcd"",
                    ""oldLeafCount"": 2,
                    ""newLeafCount"": 4,
                    ""hashAlgorithm"": ""SHA256""
                },
                ""proof"": {
                    ""proofPath"": [
                        { ""direction"": ""Left"", ""hash"": ""0000000000000000000000000000000000000000000000000000000000000000"" }
                    ]
                }
            }";

            var mutator = new Mutator(seedInput);

            for (int i = 0; i < FuzzIterations; i++)
            {
                string mutatedJson = mutator.GetMutatedString();
                try
                {
                    MerkleSerializer.DeserializeConsistencyProofPackage(mutatedJson);
                }
                catch (Exception ex) when (ex is MerkleException || ex is JsonException)
                {
                    // Expected failure from a mutated input. Continue to the next iteration.
                }
            }
        }

        private string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+-=[]{}|;:',.<>/?`~ \t\r\n";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }

    /// <summary>
    /// A simple mutator for fuzz testing. Takes a seed input and applies random corruptions.
    /// </summary>
    public class Mutator
    {
        private readonly byte[] _seed;
        private readonly Random _random = new Random();

        public Mutator(string seed)
        {
            _seed = Encoding.UTF8.GetBytes(seed);
        }

        public string GetMutatedString()
        {
            byte[] mutated = (byte[])_seed.Clone();
            if (mutated.Length == 0) return "";

            // Increase the number of mutation types for more variety.
            int mutationType = _random.Next(4);
            int index = _random.Next(mutated.Length);

            switch (mutationType)
            {
                case 0: // Bit Flip: Flip a random bit in a random byte.
                    mutated[index] ^= (byte)(1 << _random.Next(8));
                    break;

                case 1: // Byte Replace: Replace a random byte with a random value.
                    mutated[index] = (byte)_random.Next(256);
                    break;

                case 2: // Delete Block: Delete a small, random number of bytes.
                    if (mutated.Length > 1)
                    {
                        int lengthToRemove = Math.Min(_random.Next(1, 5), mutated.Length - index);
                        var list = new List<byte>(mutated);
                        list.RemoveRange(index, lengthToRemove);
                        mutated = list.ToArray();
                    }
                    break;

                case 3: // Insert Block: Insert a small block of random bytes.
                    int lengthToInsert = _random.Next(1, 5);
                    var bytesToInsert = new byte[lengthToInsert];
                    _random.NextBytes(bytesToInsert);
                    var insertList = new List<byte>(mutated);
                    insertList.InsertRange(index, bytesToInsert);
                    mutated = insertList.ToArray();
                    break;
            }

            return Encoding.UTF8.GetString(mutated);
        }
    }
}