using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace Clifton.Blockchain
{
    public class MerkleTree : IDisposable
    {
        public MerkleNode RootNode { get; protected set; } = null!;
        public HashAlgorithm HashAlgorithm { get; protected set; }

        protected List<MerkleNode> nodes = new List<MerkleNode>();
        protected List<MerkleNode> leaves = new List<MerkleNode>();
        private bool _disposed = false;

        public static void Contract(Func<bool> action, string msg)
        {
            if (!action())
            {
                throw new MerkleException(msg);
            }
        }

        public MerkleTree()
        {
            HashAlgorithm = SHA256.Create();
        }

        /// <summary>
        /// Constructor that accepts a custom hash algorithm.
        /// </summary>
        public MerkleTree(HashAlgorithm hashAlgorithm)
        {
            if (hashAlgorithm == null)
            {
                throw new ArgumentNullException(nameof(hashAlgorithm));
            }

            HashAlgorithm = hashAlgorithm;
        }

        /// <summary>
        /// Appends a pre-existing MerkleNode as a leaf.
        /// </summary>
        /// <param name="node">The node to append.</param>
        /// <returns>The appended node.</returns>
        public MerkleNode AppendLeaf(MerkleNode node)
        {
            nodes.Add(node);
            leaves.Add(node);

            return node;
        }

        /// <summary>
        /// Appends an array of pre-existing MerkleNodes as leaves.
        /// </summary>
        /// <param name="nodes">The array of nodes to append.</param>
        public void AppendLeaves(MerkleNode[] nodes)
        {
            // Use for loop instead of foreach for better performance
            for (int i = 0; i < nodes.Length; i++)
            {
                AppendLeaf(nodes[i]);
            }
        }

        /// <summary>
        /// Creates a new MerkleNode from a hash and appends it as a leaf.
        /// </summary>
        /// <param name="hash">The hash to append.</param>
        /// <returns>The newly created and appended node.</returns>
        public MerkleNode AppendLeaf(MerkleHash hash)
        {
            var node = CreateNode(hash);
            nodes.Add(node);
            leaves.Add(node);

            return node;
        }

        /// <summary>
        /// Adds a leaf from raw byte data with optional auto-hashing.
        /// </summary>
        /// <param name="data">The raw byte data for the leaf.</param>
        /// <param name="autoHash">If true, the data is hashed using the tree's hash algorithm. If false, the data is assumed to be a pre-computed hash of the correct length.</param>
        /// <returns>The newly created and appended node.</returns>
        public MerkleNode AddLeaf(byte[] data, bool autoHash = false)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            MerkleHash hash;
            if (autoHash)
            {
                hash = MerkleHash.Create(data, HashAlgorithm);
            }
            else
            {
                // Assume data is already a hash
                if (data.Length != Constants.HASH_LENGTH)
                {
                    throw new MerkleException($"Data must be {Constants.HASH_LENGTH} bytes when autoHash is false");
                }
                hash = MerkleHash.FromDigest(data);
            }

            return AppendLeaf(hash);
        }

        /// <summary>
        /// Appends an array of hashes as new leaves.
        /// </summary>
        /// <param name="hashes">The array of hashes to append.</param>
        /// <returns>A list of the newly created and appended nodes.</returns>
        public List<MerkleNode> AppendLeaves(MerkleHash[] hashes)
        {
            List<MerkleNode> nodes = new List<MerkleNode>(hashes.Length);

            for (int i = 0; i < hashes.Length; i++)
            {
                nodes.Add(AppendLeaf(hashes[i]));
            }

            return nodes;
        }

        /// <summary>
        /// Appends all leaves from another tree to this tree and rebuilds the current tree.
        /// </summary>
        /// <param name="tree">The tree from which to append leaves.</param>
        /// <returns>The new root hash of the current tree.</returns>
        public MerkleHash AddTree(MerkleTree tree)
        {
            Contract(() => leaves.Count > 0, "Cannot add to a tree with no leaves.");

            // Use for loop instead of foreach
            for (int i = 0; i < tree.leaves.Count; i++)
            {
                AppendLeaf(tree.leaves[i]);
            }

            return BuildTree();
        }

        /// <summary>
        /// If we have an odd number of leaves, add a leaf that
        /// is a duplicate of the last leaf hash so that when we add the leaves of the new tree,
        /// we don't change the root hash of the current tree.
        /// This method should only be used if you have a specific reason that you need to balance
        /// the last node with it's right branch, for example as a pre-step to computing an audit trail
        /// on the last leaf of an odd number of leaves in the tree.
        /// </summary>
        public void FixOddNumberLeaves()
        {
            if ((leaves.Count & 1) == 1)
            {
                var lastLeaf = leaves[leaves.Count - 1]; // Use indexer instead of Last()
                var l = AppendLeaf(lastLeaf.Hash);
            }
        }

        /// <summary>
        /// Computes the Merkle Tree based on the appended leaves and returns the root hash.
        /// </summary>
        public MerkleHash BuildTree()
        {
            Contract(() => leaves.Count > 0, "Cannot build a tree with no leaves.");
            BuildTree(leaves);

            return RootNode.Hash;
        }

        /// <summary>
        /// Generates an audit proof for a specific leaf. The proof consists of the sibling hashes required to compute the root hash.
        /// </summary>
        /// <param name="leafHash">The leaf hash we want to verify exists in the tree.</param>
        /// <returns>The audit trail of hashes needed to create the root, or an empty list if the leaf hash doesn't exist.</returns>
        public List<MerkleProofHash> AuditProof(MerkleHash leafHash)
        {
            List<MerkleProofHash> auditTrail = new List<MerkleProofHash>();

            var leafNode = FindLeaf(leafHash);

            if (leafNode?.Parent != null)
            {
                var parent = leafNode.Parent;
                BuildAuditTrail(auditTrail, parent, leafNode);
            }

            return auditTrail;
        }

        /// <summary>
        /// Generates a consistency proof between two versions of the tree.
        /// This proves that the newer version of the tree is a valid append-only extension of the older version.
        /// </summary>
        /// <param name="m">The number of leaves in the older version of the tree.</param>
        public List<MerkleProofHash> ConsistencyProof(int m)
        {
            Contract(() => RootNode != null, "Build the tree before requesting a consistency proof.");
            Contract(() => m > 0 && m <= leaves.Count, "The old tree size must be between 1 and the current tree size.");

            var proof = new List<MerkleProofHash>();
            if (m < leaves.Count)
            {
                BuildConsistencyProof(RootNode, leaves.Count, m, true, proof);
            }

            return proof;
        }

        /// <summary>
        /// Completes the consistency proof with an audit proof using the last node in the consistency proof.
        /// </summary>
        public List<MerkleProofHash> ConsistencyAuditProof(MerkleHash nodeHash)
        {
            List<MerkleProofHash> auditTrail = new List<MerkleProofHash>();

            var node = RootNode.Single(n => n.Hash == nodeHash);
            var parent = node.Parent;
            BuildAuditTrail(auditTrail, parent, node);

            return auditTrail;
        }

        /// <summary>
        /// Verifies an audit proof, confirming that a leaf hash is included in the tree that produced the given root hash.
        /// Static method using default SHA256.
        /// </summary>
        public static bool VerifyAudit(MerkleHash rootHash, MerkleHash leafHash, List<MerkleProofHash> auditTrail)
        {
            if (rootHash is null || leafHash is null || auditTrail is null)
            {
                return false;
            }

            if (auditTrail.Count == 0)
            {
                return rootHash == leafHash;
            }

            MerkleHash testHash = leafHash;
            // Allocate the buffer once outside the loop.
            Span<byte> buffer = stackalloc byte[Constants.HASH_LENGTH * 2];

            // Use for loop instead of foreach for better performance
            for (int i = 0; i < auditTrail.Count; i++)
            {
                var auditHash = auditTrail[i];
                testHash = auditHash.Direction == MerkleProofHash.Branch.Left ?
                    MerkleHash.Create(ComputeCombinedHash(testHash, auditHash.Hash, buffer)) :
                    MerkleHash.Create(ComputeCombinedHash(auditHash.Hash, testHash, buffer));
            }

            return rootHash == testHash;
        }

        /// <summary>
        /// Verifies an audit proof using this tree's specific hash algorithm.
        /// Instance method.
        /// </summary>
        public bool VerifyAuditWithAlgorithm(MerkleHash rootHash, MerkleHash leafHash, List<MerkleProofHash> auditTrail)
        {
            if (rootHash is null || leafHash is null || auditTrail is null)
            {
                return false;
            }

            if (auditTrail.Count == 0)
            {
                return rootHash == leafHash;
            }

            MerkleHash testHash = leafHash;
            // Allocate the buffer once outside the loop.
            Span<byte> buffer = stackalloc byte[Constants.HASH_LENGTH * 2];

            // Use for loop instead of foreach
            for (int i = 0; i < auditTrail.Count; i++)
            {
                var auditHash = auditTrail[i];
                testHash = auditHash.Direction == MerkleProofHash.Branch.Left ?
                    MerkleHash.Create(ComputeCombinedHash(testHash, auditHash.Hash, buffer), HashAlgorithm) :
                    MerkleHash.Create(ComputeCombinedHash(auditHash.Hash, testHash, buffer), HashAlgorithm);
            }

            return rootHash == testHash;
        }

        /// <summary>
        /// For demo / debugging purposes, we return the pairs of hashes used to verify the audit proof.
        /// </summary>
        public static List<Tuple<MerkleHash, MerkleHash>> AuditHashPairs(MerkleHash leafHash, List<MerkleProofHash> auditTrail)
        {
            if (auditTrail is null)
            {
                return new List<Tuple<MerkleHash, MerkleHash>>();
            }

            var auditPairs = new List<Tuple<MerkleHash, MerkleHash>>(auditTrail.Count);
            MerkleHash testHash = leafHash;
            // Allocate the buffer once outside the loop.
            Span<byte> buffer = stackalloc byte[Constants.HASH_LENGTH * 2];

            // Use for loop instead of foreach
            for (int i = 0; i < auditTrail.Count; i++)
            {
                var auditHash = auditTrail[i];
                switch (auditHash.Direction)
                {
                    case MerkleProofHash.Branch.Left:
                        auditPairs.Add(new Tuple<MerkleHash, MerkleHash>(testHash, auditHash.Hash));
                        testHash = MerkleHash.Create(ComputeCombinedHash(testHash, auditHash.Hash, buffer));
                        break;

                    case MerkleProofHash.Branch.Right:
                        auditPairs.Add(new Tuple<MerkleHash, MerkleHash>(auditHash.Hash, testHash));
                        testHash = MerkleHash.Create(ComputeCombinedHash(auditHash.Hash, testHash, buffer));
                        break;
                }
            }

            return auditPairs;
        }

        /// <summary>
        /// Verifies a size-aware consistency proof against both tree roots.
        /// </summary>
        /// <param name="oldRootHash">The root hash of the older tree.</param>
        /// <param name="newRootHash">The root hash of the newer tree.</param>
        /// <param name="oldLeafCount">The number of leaves in the older tree.</param>
        /// <param name="newLeafCount">The number of leaves in the newer tree.</param>
        /// <param name="proof">The consistency proof generated from the newer tree.</param>
        public static bool VerifyConsistency(
            MerkleHash? oldRootHash,
            MerkleHash? newRootHash,
            int oldLeafCount,
            int newLeafCount,
            IReadOnlyList<MerkleProofHash>? proof)
        {
            return VerifyConsistencyCore(oldRootHash, newRootHash, oldLeafCount, newLeafCount, proof, null);
        }

        /// <summary>
        /// Verifies a size-aware consistency proof using this tree's hash algorithm.
        /// </summary>
        public bool VerifyConsistencyWithAlgorithm(
            MerkleHash? oldRootHash,
            MerkleHash? newRootHash,
            int oldLeafCount,
            int newLeafCount,
            IReadOnlyList<MerkleProofHash>? proof)
        {
            return VerifyConsistencyCore(oldRootHash, newRootHash, oldLeafCount, newLeafCount, proof, HashAlgorithm);
        }

        private static void BuildConsistencyProof(
            MerkleNode node,
            int nodeLeafCount,
            int oldLeafCount,
            bool useKnownOldRoot,
            List<MerkleProofHash> proof)
        {
            if (oldLeafCount == nodeLeafCount)
            {
                if (!useKnownOldRoot)
                {
                    proof.Add(new MerkleProofHash(node.Hash, MerkleProofHash.Branch.Consistency));
                }

                return;
            }

            if (node.RightNode == null)
            {
                Contract(() => node.LeftNode != null, "Invalid tree structure for a consistency proof.");
                BuildConsistencyProof(node.LeftNode!, nodeLeafCount, oldLeafCount, useKnownOldRoot, proof);
                return;
            }

            int leftLeafCount = LargestPowerOfTwoLessThan(nodeLeafCount);
            Contract(
                () => node.LeftNode != null,
                $"Invalid tree structure for a consistency proof (node leaves: {nodeLeafCount}, actual leaves: {node.Leaves().Count()}, old leaves: {oldLeafCount}, is leaf: {node.IsLeaf}).");

            if (oldLeafCount <= leftLeafCount)
            {
                BuildConsistencyProof(node.LeftNode!, leftLeafCount, oldLeafCount, useKnownOldRoot, proof);
                proof.Add(new MerkleProofHash(node.RightNode!.Hash, MerkleProofHash.Branch.Consistency));
            }
            else
            {
                BuildConsistencyProof(
                    node.RightNode!,
                    nodeLeafCount - leftLeafCount,
                    oldLeafCount - leftLeafCount,
                    false,
                    proof);
                proof.Add(new MerkleProofHash(node.LeftNode!.Hash, MerkleProofHash.Branch.Consistency));
            }
        }

        private static int LargestPowerOfTwoLessThan(int value)
        {
            int target = value - 1;
            int power = 1;
            while (power <= target / 2)
            {
                power <<= 1;
            }

            return power;
        }

        private static bool VerifyConsistencyCore(
            MerkleHash? oldRootHash,
            MerkleHash? newRootHash,
            int oldLeafCount,
            int newLeafCount,
            IReadOnlyList<MerkleProofHash>? proof,
            HashAlgorithm? hashAlgorithm)
        {
            if (oldRootHash is null || newRootHash is null || proof is null ||
                oldLeafCount <= 0 || newLeafCount <= 0 || oldLeafCount > newLeafCount)
            {
                return false;
            }

            if (oldLeafCount == newLeafCount)
            {
                return proof.Count == 0 && oldRootHash == newRootHash;
            }

            if (proof.Count == 0)
            {
                return false;
            }

            int proofIndex = 0;
            try
            {
                var result = EvaluateConsistencySubproof(
                    oldLeafCount,
                    newLeafCount,
                    true,
                    oldRootHash,
                    proof,
                    ref proofIndex,
                    hashAlgorithm);

                return proofIndex == proof.Count &&
                       result.OldRoot == oldRootHash &&
                       result.NewRoot == newRootHash;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private static (MerkleHash OldRoot, MerkleHash NewRoot) EvaluateConsistencySubproof(
            int oldLeafCount,
            int newLeafCount,
            bool useKnownOldRoot,
            MerkleHash knownOldRoot,
            IReadOnlyList<MerkleProofHash> proof,
            ref int proofIndex,
            HashAlgorithm? hashAlgorithm)
        {
            if (oldLeafCount == newLeafCount)
            {
                if (useKnownOldRoot)
                {
                    return (knownOldRoot, knownOldRoot);
                }

                MerkleHash subtreeHash = TakeConsistencyProofHash(proof, ref proofIndex);
                return (subtreeHash, subtreeHash);
            }

            int leftLeafCount = LargestPowerOfTwoLessThan(newLeafCount);
            if (oldLeafCount <= leftLeafCount)
            {
                var leftResult = EvaluateConsistencySubproof(
                    oldLeafCount,
                    leftLeafCount,
                    useKnownOldRoot,
                    knownOldRoot,
                    proof,
                    ref proofIndex,
                    hashAlgorithm);
                MerkleHash rightHash = TakeConsistencyProofHash(proof, ref proofIndex);

                return (
                    leftResult.OldRoot,
                    CombineConsistencyHashes(leftResult.NewRoot, rightHash, hashAlgorithm));
            }

            var rightResult = EvaluateConsistencySubproof(
                oldLeafCount - leftLeafCount,
                newLeafCount - leftLeafCount,
                false,
                knownOldRoot,
                proof,
                ref proofIndex,
                hashAlgorithm);
            MerkleHash leftHash = TakeConsistencyProofHash(proof, ref proofIndex);

            return (
                CombineConsistencyHashes(leftHash, rightResult.OldRoot, hashAlgorithm),
                CombineConsistencyHashes(leftHash, rightResult.NewRoot, hashAlgorithm));
        }

        private static MerkleHash TakeConsistencyProofHash(
            IReadOnlyList<MerkleProofHash> proof,
            ref int proofIndex)
        {
            if (proofIndex >= proof.Count)
            {
                throw new InvalidOperationException("The consistency proof is incomplete.");
            }

            MerkleProofHash proofNode = proof[proofIndex++];
            if (proofNode is null)
            {
                throw new InvalidOperationException("The consistency proof contains a null node.");
            }

            return proofNode.Hash;
        }

        private static MerkleHash CombineConsistencyHashes(
            MerkleHash left,
            MerkleHash right,
            HashAlgorithm? hashAlgorithm)
        {
            return hashAlgorithm == null
                ? ComputeHashStatic(left, right)
                : MerkleHash.Create(left, right, hashAlgorithm);
        }

        /// <summary>
        /// Static method to compute hash using default SHA256.
        /// </summary>
        public static MerkleHash ComputeHashStatic(MerkleHash left, MerkleHash right)
        {
            Span<byte> buffer = stackalloc byte[Constants.HASH_LENGTH * 2];
            left.Value.CopyTo(buffer);
            right.Value.CopyTo(buffer.Slice(Constants.HASH_LENGTH));
            return MerkleHash.Create(buffer);
        }

        /// <summary>
        /// Instance method to compute hash using this tree's hash algorithm.
        /// </summary>
        public MerkleHash ComputeHashWithAlgorithm(MerkleHash left, MerkleHash right)
        {
            Span<byte> buffer = stackalloc byte[Constants.HASH_LENGTH * 2];
            left.Value.CopyTo(buffer);
            right.Value.CopyTo(buffer.Slice(Constants.HASH_LENGTH));
            return MerkleHash.Create(buffer, HashAlgorithm);
        }

        protected void BuildAuditTrail(List<MerkleProofHash> auditTrail, MerkleNode? parent, MerkleNode child)
        {
            if (parent != null)
            {
                Contract(() => child.Parent == parent, "Parent of child is not expected parent.");
                var nextChild = parent.LeftNode == child ? parent.RightNode : parent.LeftNode;
                var direction = parent.LeftNode == child ? MerkleProofHash.Branch.Left : MerkleProofHash.Branch.Right;

                if (nextChild != null)
                {
                    auditTrail.Add(new MerkleProofHash(nextChild.Hash, direction));
                }

                BuildAuditTrail(auditTrail, child.Parent!.Parent, child.Parent);
            }
        }

        private static ReadOnlySpan<byte> ComputeCombinedHash(MerkleHash left, MerkleHash right, Span<byte> buffer)
        {
            left.Value.CopyTo(buffer);
            right.Value.CopyTo(buffer.Slice(Constants.HASH_LENGTH));
            return buffer;
        }

        protected MerkleNode? FindLeaf(MerkleHash leafHash)
        {
            // Use for loop instead of LINQ FirstOrDefault for better performance
            for (int i = 0; i < leaves.Count; i++)
            {
                if (leaves[i].Hash == leafHash)
                {
                    return leaves[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Reduce the current list of n nodes to n/2 parents.
        /// </summary>
        protected void BuildTree(List<MerkleNode> nodes)
        {
            Contract(() => nodes.Count > 0, "node list not expected to be empty.");

            if (nodes.Count == 1)
            {
                RootNode = nodes[0];
            }
            else
            {
                // Pre-allocate the list with the exact capacity to avoid reallocations.
                int parentCount = (nodes.Count + 1) / 2;
                List<MerkleNode> parents = new List<MerkleNode>(parentCount);

                for (int i = 0; i < nodes.Count; i += 2)
                {
                    MerkleNode? right = (i + 1 < nodes.Count) ? nodes[i + 1] : null;
                    MerkleNode parent = CreateNode(nodes[i], right);
                    parents.Add(parent);
                }

                BuildTree(parents);
            }
        }

        protected virtual MerkleNode CreateNode(MerkleHash hash)
        {
            return new MerkleNode(hash);
        }

        protected virtual MerkleNode CreateNode(MerkleNode left, MerkleNode? right)
        {
            return new MerkleNode(left, right, HashAlgorithm);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    HashAlgorithm?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
