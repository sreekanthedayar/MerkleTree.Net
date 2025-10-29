using System.Collections;  
using System.Collections.Generic;  
using System.Linq;
using System.Security.Cryptography;
  
namespace Clifton.Blockchain  
{  
    public class MerkleNode : IEnumerable<MerkleNode>  
    {  
        public MerkleHash Hash { get; protected set; } = MerkleHash.Create(new byte[0]);
        public MerkleNode? LeftNode { get; protected set; }  
        public MerkleNode? RightNode { get; protected set; }  
        public MerkleNode? Parent { get; protected set; }
        protected HashAlgorithm? _hashAlgorithm;
  
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
              
            if (RightNode != null)  
            {  
                RightNode.Parent = this;  
            }  
              
            ComputeHash();  
        }

        /// <summary>
        /// Constructor for a parent node with custom hash algorithm.
        /// </summary>
        public MerkleNode(MerkleNode left, MerkleNode? right, HashAlgorithm hashAlgorithm)
        {
            _hashAlgorithm = hashAlgorithm;
            LeftNode = left;
            RightNode = right;
            LeftNode.Parent = this;

            if (RightNode != null)
            {
                RightNode.Parent = this;
            }

            ComputeHash(_hashAlgorithm);
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

        public MerkleHash ComputeHash(byte[] buffer, HashAlgorithm hashAlgorithm)
        {
            Hash = MerkleHash.Create(buffer, hashAlgorithm);

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

        public void SetLeftNode(MerkleNode node, HashAlgorithm hashAlgorithm)
        {
            LeftNode = node;
            LeftNode.Parent = this;
            ComputeHash(hashAlgorithm);
        }
  
        public void SetRightNode(MerkleNode node)  
        {  
            RightNode = node;  
            RightNode.Parent = this;  
  
            if (LeftNode != null)  
            {  
                ComputeHash();  
            }  
        }

        public void SetRightNode(MerkleNode node, HashAlgorithm hashAlgorithm)
        {
            RightNode = node;
            RightNode.Parent = this;

            if (LeftNode != null)
            {
                ComputeHash(hashAlgorithm);
            }
        }
  
        /// <summary>  
        /// True if we have enough data to verify our hash, particularly if we have child nodes.  
        /// </summary>  
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
            if (LeftNode == null)  
            {  
                Hash = MerkleHash.Create(new byte[0]);
                return;  
            }  
              
            Hash = RightNode == null ?  
                LeftNode.Hash :   
                MerkleHash.Create(LeftNode.Hash.Value.Concat(RightNode.Hash.Value).ToArray());  
            Parent?.ComputeHash();
        }

        protected void ComputeHash(HashAlgorithm hashAlgorithm)
        {
            if (LeftNode == null)
            {
                Hash = MerkleHash.Create(new byte[0]);
                return;
            }

            Hash = RightNode == null ?
                LeftNode.Hash :
                MerkleHash.Create(LeftNode.Hash.Value.Concat(RightNode.Hash.Value).ToArray(), hashAlgorithm);
            
            if (Parent != null && Parent._hashAlgorithm != null)
            {
                Parent.ComputeHash(Parent._hashAlgorithm);
            }
            else
            {
                Parent?.ComputeHash();
            }
        }
    }  
}