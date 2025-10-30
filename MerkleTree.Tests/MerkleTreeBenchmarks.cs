using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Clifton.Blockchain;

namespace MerkleTree.Tests
{
    [MemoryDiagnoser]
    public class MerkleTreeBenchmarks
    {
        [Params(100, 1000, 10000)]
        public int NumberOfLeaves { get; set; }

        private Clifton.Blockchain.MerkleTree tree = null!;
        private List<MerkleHash> leaves = null!;
        private Clifton.Blockchain.MerkleTree newTreeForConsistency = null!;

        [GlobalSetup]
        public void Setup()
        {
            tree = new Clifton.Blockchain.MerkleTree();
            leaves = new List<MerkleHash>(NumberOfLeaves);
            for (int i = 0; i < NumberOfLeaves; i++)
            {
                var leaf = MerkleHash.Create(Guid.NewGuid().ToString());
                leaves.Add(leaf);
            }
            tree.AppendLeaves(leaves.ToArray());
            // Pre-build the tree for audit proof benchmark to be fair, as audit proofs require a built tree.
            tree.BuildTree();

            // Setup for ConsistencyProof
            // This tree has the same first `NumberOfLeaves` as the `tree` object.
            newTreeForConsistency = new Clifton.Blockchain.MerkleTree();
            newTreeForConsistency.AppendLeaves(leaves.ToArray());
            // Add additional leaves to create a larger tree for the consistency proof.
            for (int i = 0; i < NumberOfLeaves; i++)
            {
                newTreeForConsistency.AppendLeaf(MerkleHash.Create(Guid.NewGuid().ToString()));
            }
            newTreeForConsistency.BuildTree();
        }

        [Benchmark(Description = "BuildTree")]
        public void BuildTreeBenchmark()
        {
            var benchmarkTree = new Clifton.Blockchain.MerkleTree();
            benchmarkTree.AppendLeaves(leaves.ToArray());
            benchmarkTree.BuildTree();
        }

        [Benchmark(Description = "AuditProof")]
        public void AuditProofBenchmark()
        {
            var randomLeaf = leaves[Random.Shared.Next(NumberOfLeaves)];
            tree.AuditProof(randomLeaf);
        }

        [Benchmark(Description = "ConsistencyProof")]
        public void ConsistencyProofBenchmark()
        {
            newTreeForConsistency.ConsistencyProof(NumberOfLeaves);
        }
    }
}