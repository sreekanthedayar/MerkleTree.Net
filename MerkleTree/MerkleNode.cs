using System.Collections;  
using System.Collections.Generic;  
using System.Linq;  
  
namespace Clifton.Blockchain  
{  
    public class MerkleNode : IEnumerable<MerkleNode>  
    {  
        public MerkleHash Hash { get; protected set; } = MerkleHash.Create(new byte[0]);
        public MerkleNode? LeftNode { get; protected set; }  
        public MerkleNode? RightNode { get; protected set; }  
        public MerkleNode? Parent { get; protected set; }  
  
        public bool IsLeaf { get { return LeftNode == null && RightNode == null; } }  
  
        public MerkleNode()  
        {  
            Hash = MerkleHash.Create(new byte[0]);
        }  
  
        /// <summary>  
        /// Constructor for a base node (leaf), representing the lowest level of the tree.  
        /// </summary>  
        public MerkleNode(MerkleHash hash)  
        {  
            Hash = hash;  
        }  
  
        /// <summary>                  
        /// Constructor for a parent node.  
        /// </summary>  
        public MerkleNode(MerkleNode left, MerkleNode? right = null)  
        {  
            LeftNode = left;  
            RightNode = right;  
            LeftNode.Parent = this;  
              
            // Replace IfNotNull extension method with simple null check  
            if (RightNode != null)  
            {  
                RightNode.Parent = this;  
            }  
              
            ComputeHash();  
        }  
  
        public override string ToString()  
        {  
            return Hash.ToString();  
        }  
  
        IEnumerator IEnumerable.GetEnumerator()  
        {  
            return GetEnumerator();  
        }  
  
        public IEnumerator<MerkleNode> GetEnumerator()  
        {  
            foreach (var n in Iterate(this)) yield return n;  
        }  
  
        /// <summary>  
        /// Bottom-up/left-right iteration of the tree.  
        /// </summary>  
        /// <param name="node"></param>  
        /// <returns></returns>  
        protected IEnumerable<MerkleNode> Iterate(MerkleNode node)  
        {  
            if (node.LeftNode != null)  
            {  
                foreach (var n in Iterate(node.LeftNode)) yield return n;  
            }  
  
            if (node.RightNode != null)  
            {  
                foreach (var n in Iterate(node.RightNode)) yield return n;  
            }  
  
            yield return node;  
        }  
  
        public MerkleHash ComputeHash(byte[] buffer)  
        {  
            Hash = MerkleHash.Create(buffer);  
  
            return Hash;  
        }  
  
        /// <summary>  
        /// Return the leaves (not all children, just leaves) under this node  
        /// </summary>  
        public IEnumerable<MerkleNode> Leaves()  
        {  
            return this.Where(n => n.LeftNode == null && n.RightNode == null);  
        }  
  
        public void SetLeftNode(MerkleNode node)  
        {  
            LeftNode = node;  
            LeftNode.Parent = this;  
            ComputeHash();  
        }  
  
        public void SetRightNode(MerkleNode node)  
        {  
            RightNode = node;  
            RightNode.Parent = this;  
  
            // Can't compute hash if the left node isn't set yet.  
            if (LeftNode != null)  
            {  
                ComputeHash();  
            }  
        }  
  
        /// <summary>  
        /// True if we have enough data to verify our hash, particularly if we have child nodes.  
        /// </summary>  
        /// <returns>True if this node is a leaf or a branch with at least a left node.</returns>  
        public bool CanVerifyHash()  
        {  
            return (LeftNode != null && RightNode != null) || (LeftNode != null);  
        }  
  
        /// <summary>  
        /// Verifies the hash for this node against the computed hash for our child nodes.  
        /// If we don't have any children, the return is always true because we have nothing to verify against.  
        /// </summary>  
        public bool VerifyHash()  
        {  
            if (LeftNode == null && RightNode == null)  
            {  
                return true;  
            }  
  
            if (RightNode == null && LeftNode != null)  
            {  
                return Hash.Equals(LeftNode.Hash);  
            }  
  
            if (LeftNode != null && RightNode != null)  
            {  
                MerkleHash leftRightHash = MerkleHash.Create(LeftNode.Hash, RightNode.Hash);  
                return Hash.Equals(leftRightHash);  
            }  
  
            return false;  
        }  
  
        /// <summary>  
        /// If the hashes are equal, we know the entire node tree is equal.  
        /// </summary>  
        public bool Equals(MerkleNode node)  
        {  
            return Hash.Equals(node.Hash);  
        }  
  
        protected void ComputeHash()  
        {  
            // Repeat the left node if the right node doesn't exist.  
            // This process breaks the case of doing a consistency check on 3 leaves when there are only 3 leaves in the tree.  
            //MerkleHash rightHash = RightNode == null ? LeftNode.Hash : RightNode.Hash;  
            //Hash = MerkleHash.Create(LeftNode.Hash.Value.Concat(rightHash.Value).ToArray());  
  
            // Alternativately, do not repeat the left node, but carry the left node's hash up.  
            // This process does not break the edge case described above.  
            // We're implementing this version because the consistency check unit tests pass when we don't simulate  
            // a right-hand node.  
              
            // Add null check for LeftNode to satisfy nullable analysis  
            if (LeftNode == null)  
            {  
                Hash = MerkleHash.Create(new byte[0]);
                return;  
            }  
              
            Hash = RightNode == null ?  
                LeftNode.Hash :   
                MerkleHash.Create(LeftNode.Hash.Value.Concat(RightNode.Hash.Value).ToArray());  
            Parent?.ComputeHash();      // Recurse, because out hash has changed.  
        }  
    }  
}