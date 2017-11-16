using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;

namespace Halda {
    //// TODO: udelat z toho structuru
    //public class DoubleLinkedList {
    //    public Node First;
    //    public Node Last;
    //    public bool IsEmpty => First == null;

    //    public void Insert(Node node) {
    //        if (First == null) {
    //            First = node;
    //            Last = node;
    //        } else {
    //            Debug.Assert(Last != null);
    //            Debug.Assert(Last.Right == null);
    //            Debug.Assert(node.Left == null);
    //            Debug.Assert(node.Right == null);

    //            Last.Right = node;
    //            Last = node;
    //        }
    //    }

    //    public Node Dequeue() {
    //        Debug.Assert(First != null);
    //        Debug.Assert(First.Left == null);

    //        var retval = First;

    //        First = First.Right;
    //        First.Left = null;

    //        return retval;
    //    }

    //    public void Merge(DoubleLinkedList other) {
    //        Debug.Assert(Last != null);
    //        Debug.Assert(other.First != null);
    //        Debug.Assert(Last.Right == null);
    //        Debug.Assert(other.First.Left == null);

    //        Last.Right = other.First;
    //        other.First.Left = Last;
    //    }
    //}

    public class Link<T> {
        public T Item;
        public Link<T> Next;

        public Link(T item) {
            Item = item;
        }

        public Link(T item, Link<T> next) {
            Item = item;
            Next = next;
        }
    }

    public class Node {
        public int Value;
        public int Rank = 0;
        public bool Marked = false;
        public bool IsRoot => Parent == null;

        public Node Parent;
        public Node Right;
        public Node Left;

        public Node Children;

        public Node(int value) {
            Value = value;
        }

        public static Node Root(int value) {
            return new Node(value);
        }
    }

    class FibHeap {
        private Node _trees;
        private Node _minNode = null;
        private int _count = 0;
        private FibHeap _nextHeap;

        public void Insert(int value) {
            var node = Node.Root(value);

            InsertNode(node);

            if (_minNode == null || _minNode.Value > node.Value) {
                _minNode = node;
            }
        }

        public void InsertNode(Node node) {
            Debug.Assert(node.Left == null);
            Debug.Assert(node.Right == null);
            Debug.Assert(_trees.Left == null);

            node.Right = _trees;
            _trees.Left = node;
        }

        public void Merge(FibHeap other) {
            Debug.Assert(_minNode != null);
            Debug.Assert(other._minNode != null);

            _count += other._count;

            _trees.Right = other._trees;
            other._trees.Left = _trees;

            //_trees.Right = other._trees.Left;
            //other._trees.Left.Right = _trees.Right;

            //_trees.

            //other._trees.Left = _trees;
            //other._trees

            //_trees.Merge(other._trees);
            //other._trees = null;
        }

        public Node Min() {
            return _minNode;
        }

        public int ExtractMin() {
            var extracted = Min();
            int retval = extracted.Value;
            _count--;

            var node = extracted.Children;

            while (node != null) {
                node.Parent = null;
                InsertNode(node);
                node = node.Right;
            }

            Consolidate();

            return retval;
        }

        public void Decrease(Node node, int value) {
            node.Value -= value;

            if (node.Parent == null || node.Parent.Value < node.Value) return;

            Cut(node);
        }

        private void Consolidate() {
            var buckets = new Link<Node>[(int)Math.Round(Math.Log(_count))];

            var node = _trees;

            while (node != null) {
                if (buckets[node.Rank] == null) {
                    buckets[node.Rank] = new Link<Node>(node);
                } else {
                    buckets[node.Rank] = new Link<Node>(node, buckets[node.Rank]);
                }
                node = node.Right;
            }

            for (int i = 0; i < buckets.Length; i++) {
                while (buckets[i] != null) {
                    var item = buckets[i];

                    if (item.Next != null) {
                        
                    }
                }
            }

            foreach (var list in buckets) {

                while (!list.IsEmpty) {
                    var first = list.Dequeue();

                    if (list.IsEmpty) {
                        _trees.Insert(first);
                        break;
                    } else {
                        var second = list.Dequeue();

                        
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

                _trees.Insert(x);

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
            Console.WriteLine("Hello World!");
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