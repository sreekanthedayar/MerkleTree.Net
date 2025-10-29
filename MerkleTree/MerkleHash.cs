  
using System;  
using System.Linq;  
using System.Security.Cryptography;  
using System.Text;  
  
namespace Clifton.Blockchain  
{  
    public class MerkleHash  
    {  
        public byte[] Value { get; protected set; }
  
        protected MerkleHash()  
        {
            Value = new byte[Constants.HASH_LENGTH];
        }  
  
        public static MerkleHash Create(byte[] buffer)  
        {  
            MerkleHash hash = new MerkleHash();  
            hash.ComputeHash(buffer);  
  
            return hash;  
        }  
  
        public static MerkleHash Create(string buffer)  
        {  
            return Create(Encoding.UTF8.GetBytes(buffer));  
        }  
  
        public static MerkleHash Create(MerkleHash left, MerkleHash right)  
        {  
            return Create(left.Value.Concat(right.Value).ToArray());  
        }  
  
        public static bool operator ==(MerkleHash h1, MerkleHash h2)  
        {  
            if (ReferenceEquals(h1, h2)) return true;  
            if (h1 is null || h2 is null) return false;  
            return h1.Equals(h2);  
        }  
  
        public static bool operator !=(MerkleHash h1, MerkleHash h2)  
        {  
            return !(h1 == h2);  
        }  
  
        public override int GetHashCode()
        {
            return ((System.Collections.IStructuralEquatable)Value).GetHashCode(System.Collections.Generic.EqualityComparer<byte>.Default);
        }  
  
        public override bool Equals(object? obj)  
        {  
            if (obj is not MerkleHash other)  
            {  
                return false;  
            }  
            
            return Equals(other);  
        }
        public override string ToString()  
        {  
            return BitConverter.ToString(Value).Replace("-", "");  
        }  
  
        public void ComputeHash(byte[] buffer)  
        {  
            using (SHA256 sha256 = SHA256.Create())  
            {  
                SetHash(sha256.ComputeHash(buffer));  
            }  
        }  
  
        public void SetHash(byte[] hash)  
        {  
            MerkleTree.Contract(() => hash.Length == Constants.HASH_LENGTH, "Unexpected hash length.");  
            Value = hash;  
        }  
  
        public bool Equals(byte[] hash)  
        {  
            return Value.SequenceEqual(hash);  
        }  
  
        public bool Equals(MerkleHash hash)  
        {  
            bool ret = false;  
  
            if (((object)hash) != null)  
            {  
                ret = Value.SequenceEqual(hash.Value);  
            }  
  
            return ret;  
        }  
    }  
}