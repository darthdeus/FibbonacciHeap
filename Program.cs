#define SKIP_PRINT

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Halda {
    [DebuggerDisplay("{Key}[{Value}]")]
    public class Node {
        public int Key;
        public int Value;
        public int Rank = 0;
        public bool Marked = false;
        public bool IsRoot => Parent == null;

        public Node Children;
        public Node Parent;
        public Node Right;
        public Node Left;

        public bool IsSingletonList => Left == this && Right == this;
        public string DebugString => $"{Key}[{Value}]";

        public Node(int key, int value) {
            Key = key;
            Left = this;
            Right = this;
            Value = value;
        }

        public void Merge(Node other) {
            Debug.Assert(other.IsSingletonList || other.Parent == null);

            if (Parent != null) {
                Debug.Assert(other.IsSingletonList);
            }

            var x = this.Right;
            var y = other.Right;

            this.Right = other;
            other.Left = this;

            x.Left = y;
            y.Right = x;

            if (Parent != null) {
                other.Parent = Parent;
            } else {
                // TODO: vsechny ostatni nody maj parent == null
                Debug.Assert(other.IsRoot);
            }

            //// TODO: poradne prozkoumat
            //if (Parent != null) {
            //    Parent.Rank++;
            //}
        }

        public Node MergeBinomTres(Node other) {
            Debug.Assert(IsRoot);
            Debug.Assert(other.IsRoot);
            Debug.Assert(Rank == other.Rank);

            int initialRank = Rank;

            if (Key > other.Key) {
                other.AddChild(this);

                Debug.Assert(other.Rank == initialRank + 1);
                return other;
            } else {
                AddChild(other);

                Debug.Assert(this.Rank == initialRank + 1);
                return this;
            }
        }

        public void AddChild(Node other) {
            Debug.Assert(other.Parent == null);
            Rank++;

            other.Parent = this;

            if (Children == null) {
                Children = other;
            } else {
                Children.Merge(other);
            }
        }

        public Node Remove() {
            Debug.Assert(Left == this || Right != this);
            Debug.Assert(Left != this || Right == this);
            Debug.Assert(Left == Right || (Left != this && Right != this));

            if (Parent != null) {
                Parent.Rank--;

                if (Parent.Children == this) {
                    Parent.Children = IsSingletonList ? null : Right;
                }
            }

            Parent = null;

            // TODO: zamyslet se
            if (IsSingletonList) {
                return null;
            }

            Left.Right = Right;
            Right.Left = Left;

            var retval = Right;

            // Kazdy prvek je cyklicky seznam
            Left = this;
            Right = this;

            return retval;
        }

        private static int count = 0;

        public void PrintDotgraph(string label) {
#if SKIP_PRINT
            return;
#endif
            using (var file = new StreamWriter($"graph{count}.dot")) {
                file.WriteLine("digraph G {");
                file.WriteLine($"label=\"{label}\"");

                var visited = new HashSet<int>();
                var stack = new Stack<Node>();

                stack.Push(this);

                while (stack.Count > 0) {
                    var node = stack.Pop();

                    if (visited.Contains(node.Value)) continue;
                    visited.Add(node.Value);

                    string color = node.IsRoot ? "lightblue" : "pink";

                    file.WriteLine($"\"{node.DebugString}\" [fillcolor = {color}, style=filled]");

                    if (node.Parent != null) {
                        stack.Push(node.Parent);
                        file.WriteLine($"\"{node.DebugString}\" -> \"{node.Parent.DebugString}\" [color=\"green\"]");
                    }

                    if (node.Children != null) {
                        stack.Push(node.Children);
                        //file.WriteLine($"{node.Key} -> {node.Children.Key} [color=\"green\"]");
                    }

                    if (node.Right != null) {
                        stack.Push(node.Right);
                        file.WriteLine($"\"{node.DebugString}\" -> \"{node.Right.DebugString}\" [color=\"red\"]");
                    }
                    if (node.Left != null) {
                        stack.Push(node.Left);
                        file.WriteLine($"\"{node.DebugString}\" -> \"{node.Left.DebugString}\" [color=\"blue\"]");
                    }
                }

                file.WriteLine("}");
            }

            Process.Start("dot", $"-Tpng -o graph{count}.png graph{count}.dot");
            count++;
        }
    }

    class FibHeap {
        private Node _trees;
        private Node _minNode = null;
        private int _count = 0;

        public Node Insert(int key, int value) {
            var node = new Node(key, value);
            InsertNode(node);
            return node;
        }

        public void InsertNode(Node node) {
            Debug.Assert(node.IsSingletonList);

            if (_trees == null) {
                _trees = node;
            } else {
                _trees.Merge(node);
            }

            _count++;

            if (_minNode == null || _minNode.Key > node.Key) {
                _minNode = node;
            }
        }

        public void Merge(FibHeap other) {
            Debug.Assert(_trees != null);
            Debug.Assert(other._trees != null);
            Debug.Assert(_minNode != null);
            Debug.Assert(other._minNode != null);

            _count += other._count;

            _trees.Merge(other._trees);

            if (other._minNode.Key < _minNode.Key) {
                _minNode = other._minNode;
            }
        }

        public Node Min() {
            return _minNode;
        }

        public int ExtractMin() {
            var min = Min();
            int retval = min.Key;
            _count--;

            _trees = min.Remove();

            int iter = 0;
            while (min.Children != null) {
                PrintDotgraph($"Extract min {iter++}");

                var child = min.Children;
                min.Children = child.Remove();

                InsertNode(child);
            }

            //do {
            //    if (child == null) break;

            //    Debug.Assert(child != null);
            //    child.Parent = null;
            //    child.Remove();

            //    InsertNode(child);
            //    child = child.Right;
            //} while (child != first);

            PrintDotgraph($"Before consolidate");
            Consolidate();
            PrintDotgraph($"After consolidate");

            return retval;
        }

        public void Decrease(Node node, int key) {
            node.Key = key;

            if (_minNode != null && _minNode.Key > node.Key) {
                _minNode = node;
            }

            if (node.Parent == null || node.Parent.Key < node.Key) return;


            Cut(node);
        }

        public void Consolidate() {
            var buckets = new Node[(int) Math.Round(Math.Log(_count)) + 10];

            // TODO: extract min

            do {
                var node = _trees;
                _trees = _trees.Remove();

                if (buckets[node.Rank] == null) {
                    buckets[node.Rank] = node;
                } else {
                    buckets[node.Rank].Merge(node);
                }
            } while (_trees != null);

            _minNode = null;

            for (int i = 0; i < buckets.Length; i++) {
                while (buckets[i] != null) {
                    var item1 = buckets[i];
                    buckets[i] = buckets[i].Remove();

                    if (buckets[i] != null) {
                        var item2 = buckets[i];
                        buckets[i] = buckets[i].Remove();

                        Debug.Assert(item1.Rank == item2.Rank);
                        //Console.WriteLine($"First: {item1.Rank}, Second: {item2.Rank}");

                        var bigger = item1.MergeBinomTres(item2);
                        Debug.Assert(bigger.Rank == i + 1);

                        if (buckets[i + 1] != null) {
                            buckets[i + 1].Merge(bigger);
                        } else {
                            buckets[i + 1] = bigger;
                        }
                    } else {
                        if (_minNode == null) {
                            _minNode = item1;
                        } else if (_minNode.Key > item1.Key) {
                            _minNode = item1;
                        }

                        if (_trees == null) {
                            _trees = item1;
                        } else {
                            _trees.Merge(item1);
                        }
                    }
                }
            }
        }

        public void Cut(Node x) {
            int iter = 0;
            while (true) {
                iter++;
                Debug.Assert(x.Parent != null);
                var o = x.Parent;

                x.Remove();

                x.Marked = false;

                _trees.Merge(x);

                if (o.Marked) {
                    x = o;
                } else if (o.IsRoot) {
                    break;
                } else {
                    o.Marked = true;
                    break;
                }
            }
        }

        public void PrintDotgraph(string label) {
            if (_trees != null) {
                _trees.PrintDotgraph(label);
            }
        }
    }

    class Program {
        static void Main(string[] args) {
            string line;

            var heap = new FibHeap();

            Node[] idmap = null;

            //var str = Console.In;
            var str = new StreamReader("test.txt");
            int cmdCount = 0;

            while ((line = str.ReadLine()) != null) {
                cmdCount++;
                heap.PrintDotgraph($"#{cmdCount} Before command {line} ... min {heap.Min()?.Key}");
                Console.WriteLine($"{cmdCount} {line}");

                switch (line[0]) {
                    case '#':
                        heap = new FibHeap();
                        idmap = new Node[int.Parse(line.Substring(2))];
                        break;

                    case 'I': {
                        var nums = line.Substring(4).Split(' ');

                        Debug.Assert(nums.Length == 2);

                        int E = int.Parse(nums[0]);
                        int K = int.Parse(nums[1]);

                        idmap[E] = heap.Insert(K, E);

                        break;
                    }

                    case 'D': {
                        if (line[2] == 'L') {
                            heap.ExtractMin();
                        } else if (line[2] == 'C') {
                            var nums = line.Substring(4).Split(' ');

                            Debug.Assert(nums.Length == 2);
                            var index = int.Parse(nums[0]);

                            if (idmap[index] != null) {
                                heap.Decrease(idmap[index], int.Parse(nums[1]));
                            }
                        }
                        break;
                    }
                }

                heap.PrintDotgraph($"#{cmdCount} After command {line} ... min {heap.Min()?.Key}");
            }

            //var heap = new FibHeap();

            //heap.Insert(1);
            //heap.Insert(2);
            //heap.Insert(3);
            //heap.Insert(4);
            //heap.Insert(5);

            //heap.Consolidate();

            //heap.PrintDotgraph();
        }

        //static void Test1() {
        //    var n1 = new Node(2);
        //    n1.Merge(new Node(3));
        //    n1.Merge(new Node(4));
        //    n1.Merge(new Node(5));
        //    n1.Merge(new Node(6));

        //    n1.PrintDotgraph();

        //    n1.Remove().PrintDotgraph();
        //    n1.PrintDotgraph();
        //}

        //static void Test2() {
        //    var n1 = new Node(2);
        //    n1.Merge(new Node(3));
        //    n1.Merge(new Node(4));
        //    n1.PrintDotgraph();

        //    var n2 = new Node(5);
        //    n2.Merge(new Node(6));
        //    n2.PrintDotgraph();

        //    n1.Merge(n2);

        //    n1.PrintDotgraph();

        //    n1.Remove().PrintDotgraph();
        //    n1.PrintDotgraph();
        //}
    }
}