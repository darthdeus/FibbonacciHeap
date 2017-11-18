#define SKIP_PRINT
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Halda {
    [DebuggerDisplay("{Value}")]
    public class Node {
        public int Value;
        public int Rank = 0;
        public bool Marked = false;
        public bool IsRoot => Parent == null;

        public Node Children;
        public Node Parent;
        public Node Right;
        public Node Left;

        public Node(int value) {
            Value = value;
            Left = this;
            Right = this;
        }

        public static Node Root(int value) {
            return new Node(value);
        }

        public void Merge(Node other) {
            Debug.Assert(other.Left == other.Right || other.Parent == null);

            var x = this.Right;
            var y = other.Right;

            this.Right = other;
            other.Left = this;

            x.Left = y;
            y.Right = x;

            // TODO: a co ty ostatni?
            other.Parent = Parent;

            // TODO: poradne prozkoumat
            if (Parent != null) {
                Parent.Rank++;
            }
        }

        public Node MergeBinomTres(Node other) {
            Debug.Assert(IsRoot);
            Debug.Assert(other.IsRoot);
            Debug.Assert(Rank == other.Rank);

            if (Value > other.Value) {
                other.AddChild(this);
                Parent = other;
                return other;
            } else {
                AddChild(other);
                other.Parent = this;
                return this;
            }
        }

        public void AddChild(Node other) {
            Rank++;
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
                    if (Right == this) {
                        Parent.Children = null;
                    } else {
                        Parent.Children = Right;
                    }
                }
            }

            // TODO: zamyslet se
            if (Right == this && Left == this) {
                return null;
            }

            Left.Right = Right;
            Right.Left = Left;

            var retval = Right;

            // Kazdy prvek je cyklicky seznam
            Left = this;
            Right = this;
            Parent = null;

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

                    file.WriteLine($"\"{node.Value}\" [fillcolor = {color}, style=filled]");

                    if (node.Parent != null) {
                        stack.Push(node.Parent);
                        file.WriteLine($"{node.Value} -> {node.Parent.Value} [color=\"green\"]");
                    }

                    if (node.Children != null) {
                        stack.Push(node.Children);
                        file.WriteLine($"{node.Value} -> {node.Children.Value} [color=\"green\"]");
                    }

                    if (node.Right != null) {
                        stack.Push(node.Right);
                        file.WriteLine($"{node.Value} -> {node.Right.Value} [color=\"red\"]");
                    }
                    if (node.Left != null) {
                        stack.Push(node.Left);
                        file.WriteLine($"{node.Value} -> {node.Left.Value} [color=\"blue\"]");
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

        public Node Insert(int value) {
            var node = Node.Root(value);
            InsertNode(node);
            return node;
        }

        public void InsertNode(Node node) {
            Debug.Assert(node.Left == node);
            Debug.Assert(node.Right == node);

            if (_trees == null) {
                _trees = node;
            } else {
                _trees.Merge(node);
            }

            _count++;

            if (_minNode == null || _minNode.Value > node.Value) {
                _minNode = node;
            }
        }

        public void Merge(FibHeap other) {
            Debug.Assert(_minNode != null);
            Debug.Assert(other._minNode != null);

            _count += other._count;

            _trees.Merge(other._trees);

            if (other._minNode.Value < _minNode.Value) {
                _minNode = other._minNode;
            }
        }

        public Node Min() {
            return _minNode;
        }

        public int ExtractMin() {
            var extracted = Min();
            int retval = extracted.Value;
            _count--;

            _trees = extracted.Remove();

            int iter = 0;
            while (extracted.Children != null) {
                PrintDotgraph($"Extract min {iter++}");

                var child = extracted.Children;
                extracted.Children = child.Remove();

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

        public void Decrease(Node node, int value) {
            node.Value = value;

            if (node.Parent == null || node.Parent.Value < node.Value) return;

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

                        var bigger = item1.MergeBinomTres(item2);

                        if (buckets[i + 1] != null) {
                            buckets[i + 1].Merge(bigger);
                        } else {
                            buckets[i + 1] = bigger;
                        }
                    } else {
                        if (_minNode == null) {
                            _minNode = item1;
                        } else if (_minNode.Value > item1.Value) {
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
            while (true) {
                var o = x.Parent;

                x.Parent = null;
                if (o.Children == x) {
                    o.Children = x.Remove();
                } else {
                    x.Remove();
                }

                x.Marked = false;

                _trees.Merge(x);

                if (o.Marked) {
                    // TODO: fuj rekurze
                    x = o;
                } else if (!o.IsRoot) {
                    o.Marked = true;
                    break;
                } else {
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
                heap.PrintDotgraph($"#{cmdCount} Before command {line}");
                Console.WriteLine(line);

                switch (line[0]) {
                    case '#':
                        heap = new FibHeap();
                        idmap = new Node[int.Parse(line.Substring(2))];
                        break;

                    case 'I': {
                        var nums = line.Substring(4).Split(' ');

                        Debug.Assert(nums.Length == 2);

                        idmap[int.Parse(nums[0])] = heap.Insert(int.Parse(nums[1]));

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

                heap.PrintDotgraph($"#{cmdCount} After command {line}");
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