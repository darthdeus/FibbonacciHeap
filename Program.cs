﻿//#define CHECK_PARSER

#define CONSOLE
//#define PRINT_GRAPH

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Halda {
    [DebuggerDisplay("{Key}[{Identifier}]")]
    public class Node {
        public int Key;
        public int Identifier;
        public int Rank = 0;
        public bool Marked = false;
        public bool IsRoot => Parent == null;

        public Node Children;
        public Node Parent;
        public Node Right;
        public Node Left;

        public bool IsSingletonList => Left == this && Right == this;
        public string DebugString => $"{Key}[{Identifier}]";

        public Node(int key, int identifier) {
            Key = key;
            Left = this;
            Right = this;
            Identifier = identifier;
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

        public static int PrintCount = 0;

        public void ForcePrintDotgraph(string label) {
            using (var file = new StreamWriter($"graph{PrintCount}.dot")) {
                file.WriteLine("digraph G {");
                file.WriteLine($"label=\"{label}\"");

                var visited = new HashSet<int>();
                var stack = new Stack<Node>();

                stack.Push(this);

                while (stack.Count > 0) {
                    var node = stack.Pop();

                    if (visited.Contains(node.Identifier)) continue;
                    visited.Add(node.Identifier);

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

            Process.Start("dot", $"-Tpng -o graph{PrintCount}.png graph{PrintCount}.dot");
            PrintCount++;
        }

        public void PrintDotgraph(string label) {
#if PRINT_GRAPH
            ForcePrintDotgraph(label);
#endif
        }
    }

    class FibHeap {
        public long Kroky = 0;
        private Node _trees;
        private Node _minNode = null;
        private int _count = 0;
        private bool _isNaive;

        public FibHeap(bool isNaive) {
            _isNaive = isNaive;
        }

        public Node Insert(int key, int identifier) {
            var node = new Node(key, identifier);
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

        public long ExtractMin() {
            Kroky = 0;

            var min = Min();
            int retval = min.Key;
            _count--;

            _trees = min.Remove();

            if (_trees == null) {
                _minNode = null;
            }

            long iter = 0;
            while (min.Children != null) {
#if SKIP_PRINT
                PrintDotgraph($"Extract min {iter++}");
#endif

                var child = min.Children;
                min.Children = child.Remove();

                Kroky++;
                InsertNode(child);
            }

            PrintDotgraph("Before consolidate");
            Consolidate();
            PrintDotgraph("After consolidate");

            // Pro jednoduchost vracime pocet kroku ExtractMin, misto vysledneho minima
            // to by slo ziskat pomoci `retval`.
            return Kroky;
        }

        public void Decrease(Node node, int key) {
            if (node.Key < key) return;

            node.Key = key;

            if (_minNode != null && _minNode.Key > node.Key) {
                _minNode = node;
            }

            if (node.Parent == null || node.Parent.Key < node.Key) return;

            Cut(node);
        }

        public void Consolidate() {
            if (_trees == null) {
                return;
            }

            var buckets = new Node[32];

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

                        Kroky++;
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
            Debug.Assert(x.Parent != null);
            int iter = 0;

            while (true) {
                iter++;


                if (x.IsRoot) {
                    Console.WriteLine(_count);
                    Debugger.Break();
                }

                Debug.Assert(!x.IsRoot);
                var o = x.Parent;

                x.Remove();

                x.Marked = false;

                _trees.Merge(x);

                if (o.IsRoot && o.Marked) {
                    o.Marked = false;
                }

                if (o.Marked) {
                    x = o;
                } else if (o.IsRoot) {
                    break;
                } else {
                    if (!_isNaive) {
                        o.Marked = true;
                    }

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

            FibHeap heap = null;
            Node[] idmap = null;

            //var str = Console.In;
            var str = new StreamReader("test.txt");
            int cmdCount = 0;

            long sum = 0, count = 0;

#if CONSOLE
            if (args.Length != 2) {
                Console.WriteLine("Output file name and heap type is required.");
                return;
            }

            if (args[0].Length != 1) {
                Console.WriteLine("Heap type can only be 's' or 'n'");
            }

            string outfile = args[1];
            bool isNaive = args[0][0] == 'n';
#else
            string outfile = "vs-out.txt";
            bool isNaive = false;
#endif
            Console.WriteLine($"Running naive = {isNaive}");

            int currentSize = 0;

            using (var outGraph = new StreamWriter(outfile)) {
                while ((line = str.ReadLine()) != null) {
                    cmdCount++;
#if PRINT_GRAPH
                    heap.PrintDotgraph($"#{cmdCount} Before command {line} ... min {heap.Min()?.Key}");
#endif

                    if (cmdCount % 100000 == 0) {
                        Console.WriteLine($"{cmdCount} {line}");
                    }

                    switch (line[0]) {
                        case '#':
                            if (count > 0) {
                                outGraph.WriteLine($"{currentSize};{(float) sum / count}");
                            }

                            heap = new FibHeap(isNaive);

                            int num = 0;
                            for (int i = 2; i < line.Length; i++) {
                                num = num * 10 + (line[i] - '0');
                            }

#if CHECK_PARSER
                            int checkNum = int.Parse(line.Substring(2));
                            Debug.Assert(num == checkNum);
#endif

                            currentSize = num;
                            idmap = new Node[num];


                            sum = 0;
                            count = 0;
                            break;

                        case 'I': {
                            int index = 4;

                            int E = 0;
                            int K = 0;

                            for (; line[index] != ' '; index++) {
                                E = E * 10 + (line[index] - '0');
                            }

                            index++;

                            for (; index < line.Length; index++) {
                                K = K * 10 + (line[index] - '0');
                            }

#if CHECK_PARSER
                                var nums = line.Substring(4).Split(' ');
                            Debug.Assert(nums.Length == 2);

                            int checkE = int.Parse(nums[0]);
                            int checkK = int.Parse(nums[1]);

                            Debug.Assert(E == checkE);
                            Debug.Assert(K == checkK);                        
#endif

                            idmap[E] = heap.Insert(K, E);

                            break;
                        }

                        case 'D': {
                            if (line[2] == 'L') {
                                var min = heap.Min();
                                var res = heap.ExtractMin();

                                idmap[min.Identifier] = null;

                                count++;
                                sum += res;
                            } else if (line[2] == 'C') {
                                int index = 4;

                                int E = 0;
                                int val = 0;

                                for (; line[index] != ' '; index++) {
                                    E = E * 10 + (line[index] - '0');
                                }

                                index++;

                                for (; index < line.Length; index++) {
                                    val = val * 10 + (line[index] - '0');
                                }

#if CHECK_PARSER
                                    var nums = line.Substring(4).Split(' ');

                                Debug.Assert(nums.Length == 2);
                                int checkE = int.Parse(nums[0]);
                                int checkVal = int.Parse(nums[1]);

                                Debug.Assert(E == checkE);
                                Debug.Assert(val == checkVal);
#endif

                                if (idmap[E] != null) {
                                    heap.Decrease(idmap[E], val);
                                }
                            }
                            break;
                        }
                    }

#if PRINT_GRAPH
                    heap.PrintDotgraph($"#{cmdCount} After command {line} ... min {heap.Min()?.Key}");
#endif
                }
            }
        }
    }
}