using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Halda {
    //// TODO: udelat z toho structuru
    public class DoubleLinkedList {
        public Node First;
        public Node Last;
        public bool IsEmpty => First == null;

        public void Insert(Node node) {
            if (First == null) {
                First = node;
                Last = node;
            } else {
                Debug.Assert(Last != null);
                Debug.Assert(Last.Right == null);
                Debug.Assert(node.Left == null);
                Debug.Assert(node.Right == null);

                Last.Right = node;
                Last = node;
            }
        }

        public Node Dequeue() {
            Debug.Assert(First != null);
            Debug.Assert(First.Left == null);

            var retval = First;

            First = First.Right;
            First.Left = null;

            return retval;
        }

        public void Merge(DoubleLinkedList other) {
            Debug.Assert(Last != null);
            Debug.Assert(other.First != null);
            Debug.Assert(Last.Right == null);
            Debug.Assert(other.First.Left == null);

            Last.Right = other.First;
            other.First.Left = Last;
        }
    }

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
            var x = this.Right;
            var y = other.Right;

            this.Right = other;
            other.Left = this;

            x.Left = y;
            y.Right = x;
        }

        public Node Remove() {
            if (this.Right == this && this.Left == this) {
                return null;
            }


            this.Left.Right = Right;
            this.Right.Left = Left;

            var ret = Left;

            Left = null;
            Right = null;

            return ret;
        }

        private static int count = 0;

        public void PrintDotgraph() {
            using (var file = new StreamWriter($"graph{count}.dot")) {
                file.WriteLine("digraph G {");

                var visited = new HashSet<int>();
                var stack = new Stack<Node>();

                stack.Push(this);

                while (stack.Count > 0) {
                    var node = stack.Pop();

                    if (visited.Contains(node.Value)) continue;
                    visited.Add(node.Value);

                    file.WriteLine($"\"{node.Value}\" [fillcolor = pink, style=filled]");

                    if (node.Parent != null) {
                        stack.Push(node.Parent);
                        file.WriteLine($"{node.Value} -> {node.Parent.Value} [color=\"yellow\"]");
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

        private string DebugString() {
            var builder = new StringBuilder();

            if (Left != null && Right != null) {
                builder.Append($"{Left.Value} <-> {Value} <-> {Right.Value}");
            } else {
                builder.Append($"{Value}");
            }


            return builder.ToString();
        }
    }

    class FibHeap {
        private Node _trees;
        private Node _minNode = null;
        private int _count = 0;

        public void Insert(int value) {
            InsertNode(Node.Root(value));
        }

        public void InsertNode(Node node) {
            Debug.Assert(node.Left == node);
            Debug.Assert(node.Right == node);

            _count++;
            _trees.Merge(node);


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

            var node = extracted.Children;
            var first = node;

            do {
                node.Parent = null;
                InsertNode(node);
                node = node.Right;
            } while (node != first);

            Consolidate();

            return retval;
        }

        public void Decrease(Node node, int value) {
            node.Value -= value;

            if (node.Parent == null || node.Parent.Value < node.Value) return;

            Cut(node);
        }

        private void Consolidate() {
            var buckets = new Node[(int) Math.Round(Math.Log(_count))];

            do {
                var node = _trees;
                _trees = _trees.Remove();

                if (buckets[node.Rank] == null) {
                    buckets[node.Rank] = node;
                } else {
                    buckets[node.Rank].Merge(node);
                }
                
            } while (_trees != null);

            for (int i = 0; i < buckets.Length; i++) {
                while (buckets[i] != null) {
                    var item1 = buckets[i];
                    buckets[i] = buckets[i].Remove();

                    if (buckets[i] != null) {
                        var item2 = buckets[i];
                        buckets[i] = buckets[i].Remove();

                        item1.Merge(item2);
                        buckets[i+1].Merge(item1);
                    } else {
                        if (_trees == null) {
                            _trees = item1;
                        } else {
                            _trees.Merge(item1);
                        }
                    }
                }
            }
        }

        private void Cut(Node x) {
            while (true) {
                var o = x.Parent;

                x.Parent = null;
                x.Left.Right = x.Right;
                x.Right.Left = x.Left;
                x.Left = null;
                x.Right = null;
                x.Marked = false;

                x.Marked = false;
                // TODO: todle ceknout

                // TODO: zakomentovano!!!!!!!!!!!!!!!!!
                //_trees.Insert(x);

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
            ;
        }
    }

    class Program {
        static void Main(string[] args) {
            var n1 = new Node(2);
            n1.Merge(new Node(3));
            n1.Merge(new Node(4));
            n1.Merge(new Node(5));
            n1.Merge(new Node(6));

            n1.PrintDotgraph();

            n1.Remove().PrintDotgraph();
            n1.PrintDotgraph();


            //n1.Merge(new Node(4));

            //var n2 = new Node(5);
            //n2.Merge(new Node(6));

            //n1.Merge(n2);

            //n1.PrintDotgraph();

            //n1.Remove().PrintDotgraph();
            //n1.PrintDotgraph();
        }
    }

    public static class LinkedListExtensions {
        public static IEnumerable<LinkedListNode<T>> EnumerateNodes<T>(this LinkedList<T> list) {
            var node = list.First;
            while (node != null) {
                yield return node;
                node = node.Next;
            }
        }
    }
}