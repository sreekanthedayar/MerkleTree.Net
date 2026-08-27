using System;  
using System.Buffers;
using System.Linq;  
using System.Security.Cryptography;  
using System.Text;  
  
namespace Clifton.Blockchain  
{  
    public class MerkleHash  
    {  
        private static readonly byte[] LeafDomainPrefix = { 0x00 };
        private static readonly byte[] NodeDomainPrefix = { 0x01 };
        private byte[] _value;

        /// <summary>
        /// Gets a copy of the hash digest.
        /// </summary>
        public byte[] Value => _value.ToArray();
  
        protected MerkleHash()  
        {
            _value = Array.Empty<byte>();
        }  
  
        public static MerkleHash Create(byte[] buffer)  
        {  
            MerkleHash hash = new MerkleHash();
            hash.ComputeHashCore(buffer);
  
            return hash;  
        }

        /// <summary>
        /// Creates a MerkleHash from a span of bytes using the default SHA256 algorithm.
        /// This is an optimized method to be used with stack-allocated buffers.
        /// </summary>
        public static MerkleHash Create(ReadOnlySpan<byte> buffer)
        {
            MerkleHash hash = new MerkleHash();
            hash.ComputeHashCore(buffer);
            return hash;
        }

        public static MerkleHash Create(byte[] buffer, HashAlgorithm hashAlgorithm)
        {
            MerkleHash hash = new MerkleHash();
            hash.ComputeHashCore(buffer, hashAlgorithm);

            return hash;
        }
  
        /// <summary>
        /// Creates a MerkleHash from a span of bytes using a specified hash algorithm.
        /// This is an optimized method to be used with stack-allocated buffers.
        /// </summary>
        public static MerkleHash Create(ReadOnlySpan<byte> buffer, HashAlgorithm hashAlgorithm)
        {
            MerkleHash hash = new MerkleHash();
            hash.ComputeHashCore(buffer, hashAlgorithm);
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
            hash.SetHashCore(digest.ToArray());
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
                hash.SetHashCore(bytes);
                return hash;
            }
            catch (FormatException ex)
            {
                throw new MerkleException($"Invalid hex string format: {ex.Message}");
            }
        }
  
        private void ComputeHashCore(byte[] buffer)
        {  
            using (SHA256 sha256 = SHA256.Create())  
            {  
                ComputeSha256HashWithPrefix(buffer, LeafDomainPrefix[0], sha256);
            }  
        }

        private void ComputeHashCore(ReadOnlySpan<byte> buffer)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                ComputeSha256HashWithPrefix(buffer, LeafDomainPrefix[0], sha256);
            }
        }

        private void ComputeSha256HashWithPrefix(
            ReadOnlySpan<byte> buffer,
            byte prefix,
            SHA256 sha256)
        {
            int prefixedLength = buffer.Length + 1;
            byte[]? rentedBuffer = null;
            Span<byte> prefixedBuffer = prefixedLength <= 256
                ? stackalloc byte[prefixedLength]
                : (rentedBuffer = ArrayPool<byte>.Shared.Rent(prefixedLength));

            try
            {
                prefixedBuffer[0] = prefix;
                buffer.CopyTo(prefixedBuffer.Slice(1));

                Span<byte> hashOutput = stackalloc byte[Constants.HASH_LENGTH];
                if (sha256.TryComputeHash(prefixedBuffer.Slice(0, prefixedLength), hashOutput, out int bytesWritten))
                {
                    if (bytesWritten != Constants.HASH_LENGTH)
                    {
                        throw new MerkleException($"Hash algorithm produced unexpected output length: {bytesWritten}");
                    }
                    SetHashCore(hashOutput.ToArray());
                }
                else
                {
                    SetHashCore(sha256.ComputeHash(prefixedBuffer.Slice(0, prefixedLength).ToArray()));
                }
            }
            finally
            {
                if (rentedBuffer != null)
                {
                    ArrayPool<byte>.Shared.Return(rentedBuffer, clearArray: true);
                }
            }
        }

        private void ComputeHashCore(byte[] buffer, HashAlgorithm hashAlgorithm)
        {
            if (hashAlgorithm == null)
            {
                throw new ArgumentNullException(nameof(hashAlgorithm));
            }

            SetHashCore(ComputeHashWithPrefix(hashAlgorithm, LeafDomainPrefix, buffer));
        }
  
        private void ComputeHashCore(ReadOnlySpan<byte> buffer, HashAlgorithm hashAlgorithm)
        {
            if (hashAlgorithm == null)
            {
                throw new ArgumentNullException(nameof(hashAlgorithm));
            }

            SetHashCore(ComputeHashWithPrefix(hashAlgorithm, LeafDomainPrefix, buffer.ToArray()));
        }

        private void SetHashCore(byte[] hash)
        {  
            MerkleTree.Contract(() => hash is not null && hash.Length > 0, "Hash cannot be empty.");
            _value = hash.ToArray();
        }

        private static MerkleHash CreateCombined(MerkleHash left, MerkleHash right, HashAlgorithm? hashAlgorithm)
        {
            MerkleHash hash = new MerkleHash();
            if (hashAlgorithm == null)
            {
                using SHA256 sha256 = SHA256.Create();
                hash.SetHashCore(ComputeHashWithPrefix(sha256, NodeDomainPrefix, left.Value, right.Value));
            }
            else
            {
                hash.SetHashCore(ComputeHashWithPrefix(hashAlgorithm, NodeDomainPrefix, left.Value, right.Value));
            }

            return hash;
        }

        private static byte[] ComputeHashWithPrefix(
            HashAlgorithm hashAlgorithm,
            byte[] prefix,
            byte[] first,
            byte[]? second = null)
        {
            hashAlgorithm.Initialize();
            hashAlgorithm.TransformBlock(prefix, 0, prefix.Length, prefix, 0);
            hashAlgorithm.TransformBlock(first, 0, first.Length, first, 0);

            if (second == null)
            {
                hashAlgorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            }
            else
            {
                hashAlgorithm.TransformFinalBlock(second, 0, second.Length);
            }

            return hashAlgorithm.Hash ?? throw new MerkleException("Hash algorithm did not produce a hash.");
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
