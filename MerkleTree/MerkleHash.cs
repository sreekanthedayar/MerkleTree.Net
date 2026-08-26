using System;  
using System.Buffers;
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
            Value = Array.Empty<byte>();
        }  
  
        public static MerkleHash Create(byte[] buffer)  
        {  
            MerkleHash hash = new MerkleHash();  
            hash.ComputeHash(buffer);  
  
            return hash;  
        }

        /// <summary>
        /// Creates a MerkleHash from a span of bytes using the default SHA256 algorithm.
        /// This is an optimized method to be used with stack-allocated buffers.
        /// </summary>
        public static MerkleHash Create(ReadOnlySpan<byte> buffer)
        {
            MerkleHash hash = new MerkleHash();
            hash.ComputeHash(buffer);
            return hash;
        }

        public static MerkleHash Create(byte[] buffer, HashAlgorithm hashAlgorithm)
        {
            MerkleHash hash = new MerkleHash();
            hash.ComputeHash(buffer, hashAlgorithm);

            return hash;
        }
  
        /// <summary>
        /// Creates a MerkleHash from a span of bytes using a specified hash algorithm.
        /// This is an optimized method to be used with stack-allocated buffers.
        /// </summary>
        public static MerkleHash Create(ReadOnlySpan<byte> buffer, HashAlgorithm hashAlgorithm)
        {
            MerkleHash hash = new MerkleHash();
            hash.ComputeHash(buffer, hashAlgorithm);
            return hash;
        }

        /// <summary>
        /// Creates a MerkleHash from an already-computed digest without hashing it again.
        /// </summary>
        public static MerkleHash FromDigest(ReadOnlySpan<byte> digest)
        {
            if (digest.Length == 0)
            {
                throw new MerkleException("Digest cannot be empty.");
            }

            MerkleHash hash = new MerkleHash();
            hash.SetHash(digest.ToArray());
            return hash;
        }

        public static MerkleHash Create(string buffer)  
        {  
            return Create(Encoding.UTF8.GetBytes(buffer));  
        }

        public static MerkleHash Create(string buffer, HashAlgorithm hashAlgorithm)
        {
            return Create(Encoding.UTF8.GetBytes(buffer), hashAlgorithm);
        }
  
        public static MerkleHash Create(MerkleHash left, MerkleHash right)  
        {
            return CreateCombined(left, right, null);
        }

        public static MerkleHash Create(MerkleHash left, MerkleHash right, HashAlgorithm hashAlgorithm)
        {
            return CreateCombined(left, right, hashAlgorithm);
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
            return ToHex();
        }

        /// <summary>
        /// Converts the hash to a hexadecimal string using HexEncoder.
        /// </summary>
        public string ToHex()
        {
            return HexEncoder.Encode(Value);
        }

        /// <summary>
        /// Creates a MerkleHash from a hexadecimal string.
        /// </summary>
        public static MerkleHash FromHex(string hexString)
        {
            if (string.IsNullOrWhiteSpace(hexString))
            {
                throw new ArgumentException("Hex string cannot be null or empty", nameof(hexString));
            }

            try
            {
                byte[] bytes = HexEncoder.Decode(hexString);
                MerkleHash hash = new MerkleHash();
                hash.SetHash(bytes);
                return hash;
            }
            catch (FormatException ex)
            {
                throw new MerkleException($"Invalid hex string format: {ex.Message}");
            }
        }
  
        public void ComputeHash(byte[] buffer)  
        {  
            using (SHA256 sha256 = SHA256.Create())  
            {  
                SetHash(sha256.ComputeHash(buffer));  
            }  
        }

        public void ComputeHash(ReadOnlySpan<byte> buffer)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                // Use TryComputeHash for zero-allocation path when available
                Span<byte> hashOutput = stackalloc byte[Constants.HASH_LENGTH];
                if (sha256.TryComputeHash(buffer, hashOutput, out int bytesWritten))
                {
                    if (bytesWritten != Constants.HASH_LENGTH)
                    {
                        throw new MerkleException($"Hash algorithm produced unexpected output length: {bytesWritten}");
                    }
                    SetHash(hashOutput.ToArray());
                }
                else
                {
                    // Fallback to ArrayPool if TryComputeHash fails (shouldn't happen with SHA256)
                    byte[]? rentedArray = null;
                    try
                    {
                        rentedArray = ArrayPool<byte>.Shared.Rent(buffer.Length);
                        buffer.CopyTo(rentedArray);
                        SetHash(sha256.ComputeHash(rentedArray, 0, buffer.Length));
                    }
                    finally
                    {
                        if (rentedArray != null)
                        {
                            ArrayPool<byte>.Shared.Return(rentedArray, clearArray: true);
                        }
                    }
                }
            }
        }

        public void ComputeHash(byte[] buffer, HashAlgorithm hashAlgorithm)
        {
            if (hashAlgorithm == null)
            {
                throw new ArgumentNullException(nameof(hashAlgorithm));
            }

            SetHash(hashAlgorithm.ComputeHash(buffer));
        }
  
        public void ComputeHash(ReadOnlySpan<byte> buffer, HashAlgorithm hashAlgorithm)
        {
            if (hashAlgorithm == null)
            {
                throw new ArgumentNullException(nameof(hashAlgorithm));
            }

            SetHash(hashAlgorithm.ComputeHash(buffer.ToArray()));
        }

        public void SetHash(byte[] hash)  
        {  
            MerkleTree.Contract(() => hash is not null && hash.Length > 0, "Hash cannot be empty.");
            Value = hash;  
        }

        private static MerkleHash CreateCombined(MerkleHash left, MerkleHash right, HashAlgorithm? hashAlgorithm)
        {
            int leftLength = left.Value.Length;
            int rightLength = right.Value.Length;
            int combinedLength = leftLength + rightLength;

            if (combinedLength <= 256)
            {
                Span<byte> buffer = stackalloc byte[combinedLength];
                left.Value.CopyTo(buffer);
                right.Value.CopyTo(buffer.Slice(leftLength));
                return hashAlgorithm == null ? Create(buffer) : Create(buffer, hashAlgorithm);
            }

            byte[] combined = new byte[combinedLength];
            left.Value.CopyTo(combined, 0);
            right.Value.CopyTo(combined, leftLength);
            return hashAlgorithm == null ? Create(combined) : Create(combined, hashAlgorithm);
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
