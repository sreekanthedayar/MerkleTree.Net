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
            try
            {
                var package = JsonSerializer.Deserialize<AuditProofPackage>(json, IndentedOptions);
                if (package == null || package.TreeMetadata == null || package.Proof == null)
                {
                    throw new MerkleException("Failed to deserialize audit proof package due to missing required properties.");
                }

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
            try
            {
                var package = JsonSerializer.Deserialize<ConsistencyProofPackage>(json, IndentedOptions);
                if (package == null || package.TreeMetadata == null || package.Proof == null)
                {
                    throw new MerkleException("Failed to deserialize consistency proof package due to missing required properties.");
                }

                return package;
            }
            catch (JsonException ex)
            {
                throw new MerkleException("Failed to deserialize consistency proof package: " + ex.Message, ex);
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