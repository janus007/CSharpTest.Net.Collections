﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using CSharpTest.Net.Collections;
using CSharpTest.Net.Serialization;
using NUnit.Framework;

namespace CSharpTest.Net.Library.Test
{
    [TestFixture]
    public class ExampleCode
    {
        [Test]
        public void LurchTableDemo()
        {
            //Queue where producer helps when queue is full
            using (var queue = new LurchTable<string, int>(LurchTableOrder.Insertion, 100))
            {
                var stop = new ManualResetEvent(false);
                queue.ItemRemoved += kv => Console.WriteLine("[{0}] - {1}", Thread.CurrentThread.ManagedThreadId, kv.Key);
                //start some threads eating queue:
                var thread = new Thread(() => { while (!stop.WaitOne(0)) queue.Dequeue(); })
                    { Name = "worker", IsBackground = true };
                thread.Start();

                var names = Directory.GetFiles(Path.GetTempPath(), "*", SearchOption.AllDirectories);
                if (names.Length <= 100) throw new Exception("Not enough trash in your temp dir.");
                var loops = Math.Max(1, 10000/names.Length);
                for(int i=0; i < loops; i++)
                    foreach (var name in names)
                        queue[name] = i;

                stop.Set();
                thread.Join();
            }
        }

        [Test]
        public void BPlusTreeDemo()
        {
            var options = new BPlusTree<string, DateTime>.OptionsV2(PrimitiveSerializer.String, PrimitiveSerializer.DateTime);
            options.CalcBTreeOrder(16, 24);
            options.CreateFile = CreatePolicy.Always;
            options.FileName = Path.GetTempFileName();
            using (var tree = new BPlusTree<string, DateTime>(options))
            {
                var tempDir = new DirectoryInfo(Path.GetTempPath());
                foreach (var file in tempDir.GetFiles("*", SearchOption.AllDirectories))
                {
                    tree.Add(file.FullName, file.LastWriteTimeUtc);
                }
            }
            options.CreateFile = CreatePolicy.Never;
            using (var tree = new BPlusTree<string, DateTime>(options))
            {
                var tempDir = new DirectoryInfo(Path.GetTempPath());
                foreach (var file in tempDir.GetFiles("*", SearchOption.AllDirectories))
                {
                    DateTime cmpDate;
                    if (!tree.TryGetValue(file.FullName, out cmpDate))
                        Console.WriteLine("New file: {0}", file.FullName);
                    else if (cmpDate != file.LastWriteTimeUtc)
                        Console.WriteLine("Modified: {0}", file.FullName);
                    tree.Remove(file.FullName);
                }
                foreach (var item in tree)
                {
                    Console.WriteLine("Removed: {0}", item.Key);
                }
            }
        }
    }
}