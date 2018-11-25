using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Minotaur.Core;
using NUnit.Framework;

namespace Minotaur.Tests.Core
{
    [TestFixture]
    public class BTreeTests
    {
        [Test]
        public void SearchRangeTest()
        {
            var btree = new BTree<int, int>(2);
            btree.Insert(10, 10);
            Assert.AreEqual(1, btree.Height);

            btree.Search(9, 11).Select(p => p.Key).Check(10);
            btree.Search(8, 9).Select(p => p.Key).Check();
            btree.Search(11, 13).Select(p => p.Key).Check();

            btree.Insert(20, 20);
            btree.Insert(30, 30);
            btree.Insert(40, 40);
            btree.Insert(50, 50);

            btree.Search(9, 11).Select(p => p.Key).Check(10);
            btree.Search(9, 31).Select(p => p.Key).Check(10, 20, 30);
            btree.Search(40, 50).Select(p => p.Key).Check(40, 50);
            btree.Search(0, 100).Select(p => p.Key).Check(10, 20, 30, 40, 50);
            btree.Search(50, 100).Select(p => p.Key).Check(50);
            btree.Search(0, 10).Select(p => p.Key).Check(10);
            btree.Search(10, 20).Select(p => p.Key).Check(10, 20);
            btree.Search(10, 30).Select(p => p.Key).Check(10, 20, 30);
            btree.Search(20, 30).Select(p => p.Key).Check(20, 30);
        }

        [Test]
        public void InsertTest()
        {
            // Build initial tree
            var tree = new BTree<char, object>(3);
            tree.Insert('J', null);
            tree.Insert('K', null);
            tree.Insert('M', null);
            tree.Insert('N', null);
            tree.Insert('O', null);
            tree.Insert('G', null);
            tree.Insert('A', null);
            tree.Insert('C', null);
            tree.Insert('D', null);
            tree.Insert('E', null);
            tree.Insert('P', null);
            tree.Insert('R', null);
            tree.Insert('S', null);
            tree.Insert('X', null);
            tree.Insert('Y', null);
            tree.Insert('Z', null);
            tree.Insert('T', null);
            tree.Insert('U', null);
            tree.Insert('V', null);

            // Insert B
            tree.Insert('B', null);
            Assert.AreEqual(tree.Height, 2);

            // Insert Q
            tree.Insert('Q', null);
            Assert.AreEqual(tree.Height, 2);

            // Insert L
            tree.Insert('L', null);
            Assert.AreEqual(tree.Height, 3);

            // Insert F
            tree.Insert('F', null);
            Assert.AreEqual(tree.Height, 3);

            tree.Search('A', 'Z').Select(p => p.Key)
                .Check('A', 'B', 'C', 'D', 'E', 'F', 'G', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'X', 'Y', 'Z');
        }

        [Test]
        public void DeleteTest()
        {
            var tree = new BTree<char, object>(3);
            tree.Insert('J', null);
            tree.Insert('K', null);
            tree.Insert('M', null);
            tree.Insert('N', null);
            tree.Insert('O', null);
            tree.Insert('G', null);
            tree.Insert('A', null);
            tree.Insert('C', null);
            tree.Insert('D', null);
            tree.Insert('E', null);
            tree.Insert('P', null);
            tree.Insert('R', null);
            tree.Insert('S', null);
            tree.Insert('X', null);
            tree.Insert('Y', null);
            tree.Insert('Z', null);
            tree.Insert('T', null);
            tree.Insert('U', null);
            tree.Insert('V', null);
            tree.Insert('B', null);
            tree.Insert('Q', null);
            tree.Insert('L', null);
            tree.Insert('F', null);

            tree.Delete('F');
            Assert.AreEqual(3, tree.Height);
            tree.Delete('M');
            Assert.AreEqual(3, tree.Height);
            tree.Delete('G');
            Assert.AreEqual(3, tree.Height);
            tree.Delete('D');
            Assert.AreEqual(2, tree.Height);

            tree.Search('A', 'Z').Select(p => p.Key)
                .Check('A', 'B', 'C', 'E', 'J', 'K', 'L', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'X', 'Y', 'Z');
        }
    }

    public static class BTreeChecker
    {
        [DebuggerStepThrough]
        public static void Check<T>(this IEnumerable<T> enumerable, params T[] values)
        {
            enumerable.ToArray().Check(values);
        }

        [DebuggerStepThrough]
        public static void Check<T>(this T[] array, params T[] values)
        {
            Assert.AreEqual(values.Length, array.Length, "Length");
            for (var i = 0; i < array.Length; i++)
                Assert.AreEqual(values[i], array[i], $"Value at {i}");
        }
    }
}
