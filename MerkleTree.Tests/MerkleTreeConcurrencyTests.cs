using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Clifton.Blockchain;
using Xunit;

namespace MerkleTree.Tests
{
    public class MerkleTreeConcurrencyTests
    {
        [Fact]
        public void ConcurrentAppendAndBuild_MultipleThreads_Succeeds()
        {
            // Arrange
            int numThreads = 4;
            int numLeavesPerThread = 100;
            var tree = new Clifton.Blockchain.MerkleTree();
            var allLeaves = new ConcurrentBag<MerkleHash>();
            var exceptions = new ConcurrentBag<Exception>();

            // Act
            Thread[] threads = new Thread[numThreads];
            for (int i = 0; i < numThreads; i++)
            {
                threads[i] = new Thread(() =>
                {
                    try
                    {
                        var threadLeaves = new MerkleHash[numLeavesPerThread];
                        for (int j = 0; j < numLeavesPerThread; j++)
                        {
                            var leaf = MerkleHash.Create(Guid.NewGuid().ToString());
                            threadLeaves[j] = leaf;
                            allLeaves.Add(leaf);
                        }

                        // The MerkleTree class is not designed to be thread-safe for concurrent writes.
                        // A lock is required to ensure that appends from different threads do not corrupt the internal state.
                        lock (tree)
                        {
                            tree.AppendLeaves(threadLeaves);
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                });
                threads[i].Start();
            }

            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            // Assert
            Assert.Empty(exceptions); // Ensure no threads threw an exception.

            tree.BuildTree();
            Assert.NotNull(tree.RootNode);
            Assert.Equal(numThreads * numLeavesPerThread, allLeaves.Count);

            // Verify that a random leaf that was added can be successfully audited.
            var randomLeaf = allLeaves.First();
            var proof = tree.AuditProof(randomLeaf);
            Assert.NotEmpty(proof);
            Assert.True(tree.VerifyAuditWithAlgorithm(tree.RootNode.Hash, randomLeaf, proof));
        }
    }
}