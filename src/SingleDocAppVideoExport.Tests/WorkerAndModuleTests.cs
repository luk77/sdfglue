//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SingleDocAppVideoExport.Gif;
using SingleDocAppVideoExportWin;
using Xunit;

namespace SingleDocAppVideoExport.Tests
{
    public class WorkerAndModuleTests
    {
        private sealed class RecordingSink : IFrameSink
        {
            public  List<string>        Calls       = new List<string>();
            public  List<int>           Indices     = new List<int>();
            public  List<byte>          FirstBytes  = new List<byte>();
            public  int                 FailAt      = -1;
            public  int                 DelayMs     = 0;

            public void Begin(int width, int height, double fps)    { lock (Calls) Calls.Add(String.Format("Begin {0}x{1} {2}", width, height, fps)); }
            public void Finish()                                    { lock (Calls) Calls.Add("Finish"); }
            public void Abort()                                     { lock (Calls) Calls.Add("Abort"); }

            public void AddFrame(VideoFrame frame)
            {
                if (DelayMs > 0)
                    Thread.Sleep(DelayMs);
                if (frame.Index == FailAt)
                    throw new IOException("disk full");
                lock (Calls)
                {
                    Indices.Add(frame.Index);
                    FirstBytes.Add(frame.Pixels[0]);
                }
            }
        }

        private static void WaitFor(Func<bool> condition)
        {
            DateTime end = DateTime.UtcNow.AddSeconds(10);
            while (!condition())
            {
                Assert.True(DateTime.UtcNow < end, "timeout");
                Thread.Sleep(1);
            }
        }

        // Submits n frames (the first byte = the frame number), waiting for free buffers
        private static void SubmitFrames(FrameSinkWorker worker, int n)
        {
            for (int i=0; i<n; i++)
            {
                VideoFrame? frame = null;
                WaitFor(() => (frame = worker.TryRentFrame()) != null || worker.IsCompleted);
                if (frame == null)
                    return;
                frame.Pixels[0] = (byte)i;
                worker.Submit(frame);
            }
        }

        [Fact]
        public void Worker_WritesFramesInOrderAndFinishes()
        {
            RecordingSink sink = new RecordingSink { DelayMs = 1 };
            using FrameSinkWorker worker = new FrameSinkWorker(sink, 4, 3, 30, 3);
            worker.Start();
            SubmitFrames(worker, 50);
            worker.Complete();
            WaitFor(() => worker.IsCompleted);

            Assert.Null(worker.Error);
            Assert.Equal(50, worker.NumOfWrittenFrames);
            Assert.Equal(Enumerable.Range(0, 50), sink.Indices);
            Assert.Equal(Enumerable.Range(0, 50).Select(i => (byte)i), sink.FirstBytes);
            Assert.Equal(new[] { "Begin 4x3 30", "Finish" }, sink.Calls);
        }

        [Fact]
        public void Worker_LimitsFramesInFlight()
        {
            RecordingSink sink = new RecordingSink();
            using FrameSinkWorker worker = new FrameSinkWorker(sink, 2, 2, 30, 2);
            // not started: no buffers
            Assert.Null(worker.TryRentFrame());
            worker.Start();
            Assert.NotNull(worker.TryRentFrame());
            Assert.NotNull(worker.TryRentFrame());
            Assert.Null(worker.TryRentFrame());     // both buffers rented, none submitted
            worker.Abort();
        }

        [Fact]
        public void Worker_ErrorStopsAndAbortsTheSink()
        {
            RecordingSink sink = new RecordingSink { FailAt = 5 };
            using FrameSinkWorker worker = new FrameSinkWorker(sink, 2, 2, 30);
            worker.Start();
            SubmitFrames(worker, 20);
            WaitFor(() => worker.IsCompleted);

            Assert.IsType<IOException>(worker.Error);
            Assert.Equal(5, worker.NumOfWrittenFrames);
            Assert.Equal("Abort", sink.Calls[^1]);
            Assert.DoesNotContain("Finish", sink.Calls);
            Assert.Null(worker.TryRentFrame());
        }

        [Fact]
        public void Worker_AbortDropsQueuedFrames()
        {
            RecordingSink sink = new RecordingSink { DelayMs = 20 };
            using FrameSinkWorker worker = new FrameSinkWorker(sink, 2, 2, 30, 4);
            worker.Start();
            SubmitFrames(worker, 4);
            worker.Abort();

            Assert.True(worker.IsCompleted);
            Assert.True(worker.NumOfWrittenFrames < 4);
            Assert.Equal("Abort", sink.Calls[^1]);
            Assert.DoesNotContain("Finish", sink.Calls);
        }

        [Fact]
        public void Modules_DoNotDependOnApplications()
        {
            string[] allowed = { "System", "Microsoft", "netstandard", "mscorlib", "SingleDocAppVideoExport" };
            foreach (System.Reflection.Assembly a in new[] { typeof(GifEncoder).Assembly, typeof(Mp4Sink).Assembly })
                Assert.All(a.GetReferencedAssemblies(), r => Assert.Contains(allowed, p => r.Name != null && r.Name.StartsWith(p, StringComparison.Ordinal)));
            // the GIF module has no other module dependency
            Assert.DoesNotContain(typeof(GifEncoder).Assembly.GetReferencedAssemblies(), r => r.Name != null && r.Name.StartsWith("SingleDocApp", StringComparison.Ordinal));
        }
    }
}
