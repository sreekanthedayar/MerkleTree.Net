using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Clifton.Blockchain
{
    public static class MerkleSerializer
    {
        private static readonly JsonSerializerOptions IndentedOptions = new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
        private static readonly JsonSerializerOptions MinifiedOptions = new JsonSerializerOptions { WriteIndented = false, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

        // Existing methods for individual proofs and metadata (DeserializeAuditProof, etc.) are here...
        // Omitting for brevity, no changes to them.

        public static string SerializeAuditProofPackage(AuditProofPackage package)
        {
            return JsonSerializer.Serialize(package, IndentedOptions);
        }

        public static AuditProofPackage DeserializeAuditProofPackage(string json)
        {
            if (json is null)
            {
                throw new MerkleException("Failed to deserialize audit proof package: JSON cannot be null.");
            }

            try
            {
                var package = JsonSerializer.Deserialize<AuditProofPackage>(json, IndentedOptions);
                if (package == null || package.TreeMetadata == null || package.Proof == null)
                {
                    throw new MerkleException("Failed to deserialize audit proof package due to missing required properties.");
                }

                ValidateAuditProofPackage(package);

                return package;
            }
            catch (JsonException ex)
            {
                throw new MerkleException("Failed to deserialize audit proof package: " + ex.Message, ex);
            }
        }

        public static string SerializeConsistencyProofPackage(ConsistencyProofPackage package)
        {
            return JsonSerializer.Serialize(package, IndentedOptions);
        }

        public static ConsistencyProofPackage DeserializeConsistencyProofPackage(string json)
        {
            if (json is null)
            {
                throw new MerkleException("Failed to deserialize consistency proof package: JSON cannot be null.");
            }

            try
            {
                var package = JsonSerializer.Deserialize<ConsistencyProofPackage>(json, IndentedOptions);
                if (package == null || package.TreeMetadata == null || package.Proof == null)
                {
                    throw new MerkleException("Failed to deserialize consistency proof package due to missing required properties.");
                }

                ValidateConsistencyProofPackage(package);

                return package;
            }
            catch (JsonException ex)
            {
                throw new MerkleException("Failed to deserialize consistency proof package: " + ex.Message, ex);
            }
        }

        private static void ValidateAuditProofPackage(AuditProofPackage package)
        {
            if (package.Version != "1.0" || package.Type != "merkle_audit_proof" || package.Timestamp == default)
            {
                throw new MerkleException("Failed to deserialize audit proof package due to invalid metadata.");
            }

            if (package.TreeMetadata.LeafCount <= 0 ||
                package.TreeMetadata.TreeDepth < 0 ||
                !IsSupportedHashAlgorithm(package.TreeMetadata.HashAlgorithm) ||
                !IsValidHash(package.TreeMetadata.RootHash, package.TreeMetadata.HashAlgorithm) ||
                !IsValidHash(package.Proof.LeafHash, package.TreeMetadata.HashAlgorithm) ||
                package.Proof.ProofPath == null)
            {
                throw new MerkleException("Failed to deserialize audit proof package due to missing required properties.");
            }

            for (int i = 0; i < package.Proof.ProofPath.Count; i++)
            {
                var proofNode = package.Proof.ProofPath[i];
                if (proofNode == null ||
                    !IsValidHash(proofNode.Hash, package.TreeMetadata.HashAlgorithm) ||
                    (proofNode.Direction != "Left" && proofNode.Direction != "Right"))
                {
                    throw new MerkleException("Failed to deserialize audit proof package due to an invalid proof path.");
                }
            }
        }

        private static void ValidateConsistencyProofPackage(ConsistencyProofPackage package)
        {
            if (package.Version != "1.0" || package.Type != "merkle_consistency_proof" || package.Timestamp == default)
            {
                throw new MerkleException("Failed to deserialize consistency proof package due to invalid metadata.");
            }

            if (package.TreeMetadata.OldLeafCount <= 0 ||
                package.TreeMetadata.NewLeafCount < package.TreeMetadata.OldLeafCount ||
                !IsSupportedHashAlgorithm(package.TreeMetadata.HashAlgorithm) ||
                !IsValidHash(package.TreeMetadata.OldRootHash, package.TreeMetadata.HashAlgorithm) ||
                !IsValidHash(package.TreeMetadata.NewRootHash, package.TreeMetadata.HashAlgorithm) ||
                package.Proof.ProofPath == null)
            {
                throw new MerkleException("Failed to deserialize consistency proof package due to missing required properties.");
            }

            if (package.TreeMetadata.NewLeafCount == package.TreeMetadata.OldLeafCount &&
                (!string.Equals(package.TreeMetadata.OldRootHash, package.TreeMetadata.NewRootHash, StringComparison.OrdinalIgnoreCase) ||
                 package.Proof.ProofPath.Count != 0))
            {
                throw new MerkleException("Failed to deserialize consistency proof package due to an invalid same-size proof.");
            }

            for (int i = 0; i < package.Proof.ProofPath.Count; i++)
            {
                var proofNode = package.Proof.ProofPath[i];
                if (proofNode == null ||
                    !IsValidHash(proofNode.Hash, package.TreeMetadata.HashAlgorithm) ||
                    proofNode.Direction != "Consistency")
                {
                    throw new MerkleException("Failed to deserialize consistency proof package due to an invalid proof path.");
                }
            }
        }

        private static bool IsSupportedHashAlgorithm(string? hashAlgorithm)
        {
            return string.Equals(hashAlgorithm, "SHA256", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(hashAlgorithm, "SHA512", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsValidHash(string? value, string? hashAlgorithm)
        {
            if (!IsSupportedHashAlgorithm(hashAlgorithm) || string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            int expectedLength = string.Equals(hashAlgorithm, "SHA512", StringComparison.OrdinalIgnoreCase)
                ? 64
                : 32;

            try
            {
                return HexEncoder.Decode(value).Length == expectedLength;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        // ... other existing methods like CalculateDepth, etc.
    }

    public class AuditProofPackage
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "merkle_audit_proof";

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("treeMetadata")]
        public AuditTreeMetadata TreeMetadata { get; set; } = null!;

        [JsonPropertyName("proof")]
        public AuditProof Proof { get; set; } = null!;
    }

    public class AuditTreeMetadata
    {
        [JsonPropertyName("rootHash")]
        public string RootHash { get; set; } = null!;

        [JsonPropertyName("leafCount")]
        public int LeafCount { get; set; }

        [JsonPropertyName("treeDepth")]
        public int TreeDepth { get; set; }

        [JsonPropertyName("hashAlgorithm")]
        public string HashAlgorithm { get; set; } = null!;
    }

    public class AuditProof
    {
        [JsonPropertyName("leafHash")]
        public string LeafHash { get; set; } = null!;

        [JsonPropertyName("proofPath")]
        public List<ProofNode> ProofPath { get; set; } = null!;
    }

    public class ConsistencyProofPackage
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "merkle_consistency_proof";

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("treeMetadata")]
        public ConsistencyTreeMetadata TreeMetadata { get; set; } = null!;

        [JsonPropertyName("proof")]
        public ConsistencyProof Proof { get; set; } = null!;
    }

    public class ConsistencyTreeMetadata
    {
        [JsonPropertyName("oldRootHash")]
        public string OldRootHash { get; set; } = null!;

        [JsonPropertyName("newRootHash")]
        public string NewRootHash { get; set; } = null!;

        [JsonPropertyName("oldLeafCount")]
        public int OldLeafCount { get; set; }

        [JsonPropertyName("newLeafCount")]
        public int NewLeafCount { get; set; }

        [JsonPropertyName("hashAlgorithm")]
        public string HashAlgorithm { get; set; } = null!;
    }

    public class ConsistencyProof
    {
        [JsonPropertyName("proofPath")]
        public List<ProofNode> ProofPath { get; set; } = null!;
    }

    public class ProofNode
    {
        [JsonPropertyName("direction")]
        public string Direction { get; set; } = null!;

        [JsonPropertyName("hash")]
        public string Hash { get; set; } = null!;
    }

    // Existing TreeMetadata class for DeserializeTreeMetadata method
    public class TreeMetadata
    {
        public MerkleHash RootHash { get; set; } = null!;
        public int LeafCount { get; set; }
        public int TreeDepth { get; set; }
        public string HashAlgorithm { get; set; } = "SHA256";
    }
}
