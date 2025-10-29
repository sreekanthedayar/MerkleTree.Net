﻿using System;

namespace Clifton.Blockchain
{
    public class MerkleException : Exception
    {
        public MerkleException(string msg) : base(msg)
        {
        }

        public MerkleException(string msg, Exception innerException) : base(msg, innerException)
        {
        }
    }
}
