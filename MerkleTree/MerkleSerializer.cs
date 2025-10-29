using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Clifton.Blockchain
{
    /// <summary>
    /// Provides serialization and deserialization for Merkle proofs and tree data.
    /// </summary>
    public static class MerkleSerializer
    {
        private static readonly JsonSerializerOptions IndentedOptions = new JsonSerializerOptions { WriteIndented = true };
        private static readonly JsonSerializerOptions MinifiedOptions = new JsonSerializerOptions { WriteIndented = false };

        /// <summary>
        /// Serializes an audit proof to JSON.
        /// </summary>
        public static string SerializeAuditProof(List<MerkleProofHash> auditProof)
        {
            if (auditProof == null || auditProof.Count == 0)
            {
                return "[]";
            }

            var proofData = auditProof.Select(p => new
            {
                direction = p.Direction == MerkleProofHash.Branch.Left ? "left" : "right",
                hash = p.Hash.ToHex()
            });

            return JsonSerializer.Serialize(proofData, MinifiedOptions);
        }

        /// <summary>
        /// Deserializes an audit proof from JSON.
        /// </summary>
        public static List<MerkleProofHash> DeserializeAuditProof(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("JSON string cannot be null or empty", nameof(json));
            }

            var proofList = new List<MerkleProofHash>();

            try
            {
                using JsonDocument document = JsonDocument.Parse(json);
                var root = document.RootElement;

                if (root.ValueKind != JsonValueKind.Array)
                {
                    throw new JsonException("Expected JSON array for audit proof");
                }

                foreach (var element in root.EnumerateArray())
                {
                    var directionStr = element.GetProperty("direction").GetString();
                    var hashStr = element.GetProperty("hash").GetString();

                    if (string.IsNullOrEmpty(directionStr) || string.IsNullOrEmpty(hashStr))
                    {
                        throw new JsonException("Invalid proof item: missing direction or hash");
                    }

                    var direction = directionStr.ToLower() == "left" 
                        ? MerkleProofHash.Branch.Left 
                        : MerkleProofHash.Branch.Right;

                    var hash = MerkleHash.FromHex(hashStr);
                    proofList.Add(new MerkleProofHash(hash, direction));
                }
            }
            catch (JsonException ex)
            {
                throw new MerkleException($"Failed to deserialize audit proof: {ex.Message}");
            }

            return proofList;
        }

        /// <summary>
        /// Serializes a consistency proof to JSON.
        /// </summary>
        public static string SerializeConsistencyProof(List<MerkleProofHash> consistencyProof)
        {
            if (consistencyProof == null || consistencyProof.Count == 0)
            {
                return "[]";
            }

            var proofData = consistencyProof.Select(p => new
            {
                direction = p.Direction switch
                {
                    MerkleProofHash.Branch.Left => "left",
                    MerkleProofHash.Branch.Right => "right",
                    MerkleProofHash.Branch.OldRoot => "old_root",
                    _ => "unknown"
                },
                hash = p.Hash.ToHex()
            });

            return JsonSerializer.Serialize(proofData, MinifiedOptions);
        }

        /// <summary>
        /// Deserializes a consistency proof from JSON.
        /// </summary>
        public static List<MerkleProofHash> DeserializeConsistencyProof(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("JSON string cannot be null or empty", nameof(json));
            }

            var proofList = new List<MerkleProofHash>();

            try
            {
                using JsonDocument document = JsonDocument.Parse(json);
                var root = document.RootElement;

                if (root.ValueKind != JsonValueKind.Array)
                {
                    throw new JsonException("Expected JSON array for consistency proof");
                }

                foreach (var element in root.EnumerateArray())
                {
                    var directionStr = element.GetProperty("direction").GetString();
                    var hashStr = element.GetProperty("hash").GetString();

                    if (string.IsNullOrEmpty(directionStr) || string.IsNullOrEmpty(hashStr))
                    {
                        throw new JsonException("Invalid proof item: missing direction or hash");
                    }

                    var direction = directionStr.ToLower() switch
                    {
                        "left" => MerkleProofHash.Branch.Left,
                        "right" => MerkleProofHash.Branch.Right,
                        "old_root" => MerkleProofHash.Branch.OldRoot,
                        _ => throw new JsonException($"Invalid direction value: {directionStr}")
                    };

                    var hash = MerkleHash.FromHex(hashStr);
                    proofList.Add(new MerkleProofHash(hash, direction));
                }
            }
            catch (JsonException ex)
            {
                throw new MerkleException($"Failed to deserialize consistency proof: {ex.Message}");
            }

            return proofList;
        }

        /// <summary>
        /// Serializes tree metadata to JSON.
        /// </summary>
        public static string SerializeTreeMetadata(MerkleTree tree, string? hashAlgorithmName = null)
        {
            if (tree == null)
            {
                throw new ArgumentNullException(nameof(tree));
            }

            if (tree.RootNode == null)
            {
                throw new MerkleException("Cannot serialize metadata for tree with no root node");
            }

            var metadata = new
            {
                rootHash = tree.RootNode.Hash.ToHex(),
                leafCount = tree.RootNode.Leaves().Count(),
                treeDepth = CalculateDepth(tree.RootNode),
                hashAlgorithm = hashAlgorithmName ?? "SHA256",
                timestamp = DateTime.UtcNow.ToString("O")
            };

            return JsonSerializer.Serialize(metadata, IndentedOptions);
        }

        /// <summary>
        /// Deserializes tree metadata from JSON.
        /// </summary>
        public static TreeMetadata DeserializeTreeMetadata(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("JSON string cannot be null or empty", nameof(json));
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(json);
                var root = document.RootElement;

                var rootHashStr = root.GetProperty("rootHash").GetString();
                var leafCount = root.GetProperty("leafCount").GetInt32();
                var treeDepth = root.GetProperty("treeDepth").GetInt32();
                var hashAlgorithm = root.GetProperty("hashAlgorithm").GetString();

                if (string.IsNullOrEmpty(rootHashStr))
                {
                    throw new JsonException("Invalid metadata: missing rootHash");
                }

                var rootHash = MerkleHash.FromHex(rootHashStr);

                return new TreeMetadata
                {
                    RootHash = rootHash,
                    LeafCount = leafCount,
                    TreeDepth = treeDepth,
                    HashAlgorithm = hashAlgorithm ?? "SHA256"
                };
            }
            catch (JsonException ex)
            {
                throw new MerkleException($"Failed to deserialize tree metadata: {ex.Message}");
            }
        }

        /// <summary>
        /// Exports a complete proof package including tree metadata and audit proof.
        /// </summary>
        public static string SerializeAuditProofPackage(
            MerkleHash leafHash, 
            MerkleHash rootHash, 
            List<MerkleProofHash> auditTrail,
            int leafCount,
            int treeDepth,
            string? hashAlgorithmName = null)
        {
            var proofPath = auditTrail.Select(p => new
            {
                direction = p.Direction == MerkleProofHash.Branch.Left ? "left" : "right",
                hash = p.Hash.ToHex()
            });

            var package = new
            {
                version = "1.0",
                type = "merkle_audit_proof",
                timestamp = System.DateTime.UtcNow.ToString("O"),
                treeMetadata = new 
                {
                    rootHash = rootHash.ToHex(),
                    leafCount,
                    treeDepth,
                    hashAlgorithm = hashAlgorithmName ?? "SHA256"
                },
                proof = new
                {
                    leafHash = leafHash.ToHex(),
                    proofPath
                }
            };

            return JsonSerializer.Serialize(package, IndentedOptions);
        }

        /// <summary>
        /// Exports a consistency proof package with metadata.
        /// </summary>
        public static string SerializeConsistencyProofPackage(
            MerkleHash oldRootHash,
            MerkleHash newRootHash,
            List<MerkleProofHash> consistencyProof,
            int oldLeafCount,
            int newLeafCount,
            string? hashAlgorithmName = null)
        {
            var proofPath = consistencyProof.Select(p => new
            {
                direction = p.Direction switch
                {
                    MerkleProofHash.Branch.Left => "left",
                    MerkleProofHash.Branch.Right => "right",
                    MerkleProofHash.Branch.OldRoot => "old_root",
                    _ => "unknown"
                },
                hash = p.Hash.ToHex()
            });

            var package = new
            {
                version = "1.0",
                type = "merkle_consistency_proof",
                timestamp = System.DateTime.UtcNow.ToString("O"),
                treeMetadata = new
                {
                    oldRootHash = oldRootHash.ToHex(),
                    newRootHash = newRootHash.ToHex(),
                    oldLeafCount,
                    newLeafCount,
                    hashAlgorithm = hashAlgorithmName ?? "SHA256"
                },
                proof = new
                {
                    proofPath
                }
            };

            return JsonSerializer.Serialize(package, IndentedOptions);
        }

        private static int CalculateDepth(MerkleNode node)
        {
            if (node.IsLeaf) return 0;

            int leftDepth = node.LeftNode != null ? CalculateDepth(node.LeftNode) : 0;
            int rightDepth = node.RightNode != null ? CalculateDepth(node.RightNode) : 0;

            return 1 + Math.Max(leftDepth, rightDepth);
        }
    }

    /// <summary>
    /// Represents deserialized tree metadata.
    /// </summary>
    public class TreeMetadata
    {
        public MerkleHash RootHash { get; set; } = null!;
        public int LeafCount { get; set; }
        public int TreeDepth { get; set; }
        public string HashAlgorithm { get; set; } = "SHA256";
    }
}