using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace Clifton.Blockchain
{
    public class MerkleTree
    {
        public MerkleNode RootNode { get; protected set; } = null!;
        public HashAlgorithm HashAlgorithm { get; protected set; }

        protected List<MerkleNode> nodes = new List<MerkleNode>();
        protected List<MerkleNode> leaves = new List<MerkleNode>();

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

        public MerkleNode AppendLeaf(MerkleNode node)
        {
            nodes.Add(node);
            leaves.Add(node);

            return node;
        }

        public void AppendLeaves(MerkleNode[] nodes)
        {
            foreach (var n in nodes)
            {
                AppendLeaf(n);
            }
        }

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
                hash = MerkleHash.Create(data);
            }

            return AppendLeaf(hash);
        }

        public List<MerkleNode> AppendLeaves(MerkleHash[] hashes)
        {
            List<MerkleNode> nodes = new List<MerkleNode>();

            foreach (var h in hashes)
            {
                nodes.Add(AppendLeaf(h));
            }

            return nodes;
        }

        public MerkleHash AddTree(MerkleTree tree)
        {
            Contract(() => leaves.Count > 0, "Cannot add to a tree with no leaves.");

            foreach (var l in tree.leaves)
            {
                AppendLeaf(l);
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
                var lastLeaf = leaves.Last();
                var l = AppendLeaf(lastLeaf.Hash);
            }
        }

        /// <summary>
        /// Builds the tree for leaves and returns the root node.
        /// </summary>
        public MerkleHash BuildTree()
        {
            Contract(() => leaves.Count > 0, "Cannot build a tree with no leaves.");
            BuildTree(leaves);

            return RootNode.Hash;
        }

        /// <summary>
        /// Returns the audit proof hashes to reconstruct the root hash.
        /// </summary>
        /// <param name="leafHash">The leaf hash we want to verify exists in the tree.</param>
        /// <returns>The audit trail of hashes needed to create the root, or an empty list if the leaf hash doesn't exist.</returns>
        public List<MerkleProofHash> AuditProof(MerkleHash leafHash)
        {
            List<MerkleProofHash> auditTrail = new List<MerkleProofHash>();

            var leafNode = FindLeaf(leafHash);

            if (leafNode != null)
            {
                Contract(() => leafNode.Parent != null, "Expected leaf to have a parent.");
                var parent = leafNode.Parent;
                BuildAuditTrail(auditTrail, parent, leafNode);
            }

            return auditTrail;
        }

        /// <summary>
        /// Verifies ordering and consistency of the first n leaves, such that we reach the expected subroot.
        /// This verifies that the prior data has not been changed and that leaf order has been preserved.
        /// m is the number of leaves for which to do a consistency check.
        /// </summary>
        public List<MerkleProofHash> ConsistencyProof(int m)
        {
            List<MerkleProofHash> hashNodes = new List<MerkleProofHash>();
            int idx = (int)Math.Log(m, 2);

            MerkleNode node = leaves[0];

            while (idx > 0)
            {
                if (node.Parent == null)
                {
                    throw new MerkleException("Invalid consistency proof request. The tree structure is smaller than the proof requires.");
                }
                node = node.Parent;
                --idx;
            }

            int k = node.Leaves().Count();
            hashNodes.Add(new MerkleProofHash(node.Hash, MerkleProofHash.Branch.OldRoot));

            if (m == k)
            {
                // Continue with Rule 3 -- the remainder is the audit proof
            }
            else
            {
                if (node.Parent == null) throw new InvalidOperationException("Node parent is null");
                MerkleNode? sn = node.Parent.RightNode;
                bool traverseTree = true;

                while (traverseTree)
                {
                    Contract(() => sn != null, "Sibling node must exist because m != k");
                    int sncount = sn!.Leaves().Count();

                    if (m - k == sncount)
                    {
                        hashNodes.Add(new MerkleProofHash(sn.Hash, MerkleProofHash.Branch.OldRoot));
                        break;
                    }

                    if (m - k > sncount)
                    {
                        hashNodes.Add(new MerkleProofHash(sn.Hash, MerkleProofHash.Branch.OldRoot));
                        if (sn.Parent == null)
                        {
                            throw new MerkleException("Invalid tree structure encountered during consistency proof.");
                        }
                        sn = sn.Parent.RightNode;
                        k += sncount;
                    }
                    else
                    {
                        sn = sn.LeftNode;
                    }
                }
            }

            return hashNodes;
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
        /// Verify that if we walk up the tree from a particular leaf, we encounter the expected root hash.
        /// Static method using default SHA256.
        /// </summary>
        public static bool VerifyAudit(MerkleHash rootHash, MerkleHash leafHash, List<MerkleProofHash> auditTrail)
        {
            Contract(() => auditTrail.Count > 0, "Audit trail cannot be empty.");
            MerkleHash testHash = leafHash;

            foreach (MerkleProofHash auditHash in auditTrail)
            {
                testHash = auditHash.Direction == MerkleProofHash.Branch.Left ?
                    MerkleHash.Create(testHash.Value.Concat(auditHash.Hash.Value).ToArray()) :
                    MerkleHash.Create(auditHash.Hash.Value.Concat(testHash.Value).ToArray());
            }

            return rootHash == testHash;
        }

        /// <summary>
        /// Verify audit using this tree's hash algorithm.
        /// Instance method.
        /// </summary>
        public bool VerifyAuditWithAlgorithm(MerkleHash rootHash, MerkleHash leafHash, List<MerkleProofHash> auditTrail)
        {
            Contract(() => auditTrail.Count > 0, "Audit trail cannot be empty.");
            MerkleHash testHash = leafHash;

            foreach (MerkleProofHash auditHash in auditTrail)
            {
                testHash = auditHash.Direction == MerkleProofHash.Branch.Left ?
                    MerkleHash.Create(testHash.Value.Concat(auditHash.Hash.Value).ToArray(), HashAlgorithm) :
                    MerkleHash.Create(auditHash.Hash.Value.Concat(testHash.Value).ToArray(), HashAlgorithm);
            }

            return rootHash == testHash;
        }

        /// <summary>
        /// For demo / debugging purposes, we return the pairs of hashes used to verify the audit proof.
        /// </summary>
        public static List<Tuple<MerkleHash, MerkleHash>> AuditHashPairs(MerkleHash leafHash, List<MerkleProofHash> auditTrail)
        {
            Contract(() => auditTrail.Count > 0, "Audit trail cannot be empty.");
            var auditPairs = new List<Tuple<MerkleHash, MerkleHash>>();
            MerkleHash testHash = leafHash;

            foreach (MerkleProofHash auditHash in auditTrail)
            {
                switch (auditHash.Direction)
                {
                    case MerkleProofHash.Branch.Left:
                        auditPairs.Add(new Tuple<MerkleHash, MerkleHash>(testHash, auditHash.Hash));
                        testHash = MerkleHash.Create(testHash.Value.Concat(auditHash.Hash.Value).ToArray());
                        break;

                    case MerkleProofHash.Branch.Right:
                        auditPairs.Add(new Tuple<MerkleHash, MerkleHash>(auditHash.Hash, testHash));
                        testHash = MerkleHash.Create(auditHash.Hash.Value.Concat(testHash.Value).ToArray());
                        break;
                }
            }

            return auditPairs;
        }

        public static bool VerifyConsistency(MerkleHash oldRootHash, List<MerkleProofHash> proof)
        {
            MerkleHash hash, lhash, rhash;

            if (proof.Count > 1)
            {
                lhash = proof[proof.Count - 2].Hash;
                int hidx = proof.Count - 1;
                hash = rhash = ComputeHashStatic(lhash, proof[hidx].Hash);
                hidx -= 2;

                while (hidx >= 0)
                {
                    lhash = proof[hidx].Hash;
                    hash = rhash = ComputeHashStatic(lhash, rhash);

                    --hidx;
                }
            }
            else
            {
                hash = proof[0].Hash;
            }

            return hash == oldRootHash;
        }

        /// <summary>
        /// Static method to compute hash using default SHA256.
        /// </summary>
        public static MerkleHash ComputeHashStatic(MerkleHash left, MerkleHash right)
        {
            return MerkleHash.Create(left.Value.Concat(right.Value).ToArray());
        }

        /// <summary>
        /// Instance method to compute hash using this tree's hash algorithm.
        /// </summary>
        public MerkleHash ComputeHashWithAlgorithm(MerkleHash left, MerkleHash right)
        {
            return MerkleHash.Create(left.Value.Concat(right.Value).ToArray(), HashAlgorithm);
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

        protected MerkleNode? FindLeaf(MerkleHash leafHash)
        {
            return leaves.FirstOrDefault(l => l.Hash == leafHash);
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
                List<MerkleNode> parents = new List<MerkleNode>();

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
    }
}