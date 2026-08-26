using System;

namespace Clifton.Blockchain
{
    public class MerkleProofHash
    {
        public enum Branch
        {
            Left,
            Right,
            OldRoot,       // legacy consistency-proof marker.
            Consistency,   // node in a size-aware consistency proof.
        }

        public MerkleHash Hash { get; protected set; } = null!;
        public Branch Direction { get; protected set; }

        public MerkleProofHash(MerkleHash hash, Branch direction)
        {
            Hash = hash;
            Direction = direction;
        }

        public override string ToString()
        {
            return Hash.ToString();
        }
    }
}
