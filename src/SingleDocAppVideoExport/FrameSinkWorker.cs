//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using System.Collections.Concurrent;

namespace SingleDocAppVideoExport
{
    // Writes frames to an IFrameSink on a background thread, so the main thread (rendering, UI) does not wait for
    // the encoding and the files. The number of frames in flight is limited by a pool of buffers: TryRentFrame returns
    // null when the writer is behind - the caller renders the frame later (e.g. in the next frame of the application).
    //
    // Main thread:  Start -> (TryRentFrame, fill, Submit)* -> Complete -> poll IsCompleted / Error
    //               Abort at any time (cancel); waits for the writer thread.
    public sealed class FrameSinkWorker : IDisposable
    {
        public  const int                           DefaultMaxFramesInFlight    = 4;

        private readonly IFrameSink                 sink_;
        private readonly int                        width_;
        private readonly int                        height_;
        private readonly double                     fps_;
        private readonly int                        maxFramesInFlight_;

        private readonly BlockingCollection<VideoFrame>  queue_     = new BlockingCollection<VideoFrame>();
        private readonly ConcurrentBag<VideoFrame>  freeFrames_     = new ConcurrentBag<VideoFrame>();
        private          int                        numOfAllocatedFrames_   = 0;    // main thread only
        private          int                        nextIndex_              = 0;    // main thread only

        private          Thread?                    thread_;
        private volatile bool                       abortRequested_ = false;
        private volatile bool                       isCompleted_    = false;
        private volatile Exception?                 error_;
        private          int                        numOfWrittenFrames_     = 0;

        public FrameSinkWorker(IFrameSink sink, int width, int height, double fps, int maxFramesInFlight = DefaultMaxFramesInFlight)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException(String.Format("Invalid frame size: {0}x{1}", width, height));
            if (fps <= 0.0)
                throw new ArgumentException("FPS must be greater than 0.");

            sink_               = sink;
            width_              = width;
            height_             = height;
            fps_                = fps;
            maxFramesInFlight_  = Math.Max(1, maxFramesInFlight);
        }

        public int          Width               { get { return width_;  } }
        public int          Height              { get { return height_; } }

        // frames written by the sink (AddFrame returned)
        public int          NumOfWrittenFrames  { get { return Volatile.Read(ref numOfWrittenFrames_); } }

        // the writer thread has ended: finished, aborted or failed (Error)
        public bool         IsCompleted         { get { return isCompleted_; } }

        // the first error of the sink (the writer thread stops on it and aborts the sink)
        public Exception?   Error               { get { return error_; } }

        public void Start()
        {
            if (thread_ != null)
                throw new InvalidOperationException("The worker is already started.");

            thread_ = new Thread(Run) { Name = "FrameSinkWorker", IsBackground = true };
            // COM based encoders (Media Foundation) need the multithreaded apartment
            if (OperatingSystem.IsWindows())
                thread_.SetApartmentState(ApartmentState.MTA);
            thread_.Start();
        }

        // A free buffer for the next frame (its size is Width x Height), null: all buffers are in use or the writer has ended
        public VideoFrame? TryRentFrame()
        {
            if (thread_ == null || isCompleted_ || abortRequested_ || queue_.IsAddingCompleted)
                return null;

            if (freeFrames_.TryTake(out VideoFrame? frame))
                return frame;

            if (numOfAllocatedFrames_ >= maxFramesInFlight_)
                return null;

            numOfAllocatedFrames_++;
            return new VideoFrame(width_, height_);
        }

        // Queues a frame from TryRentFrame (frames are written in the order of the calls)
        public void Submit(VideoFrame frame)
        {
            frame.Index = nextIndex_++;
            try
            {
                queue_.Add(frame);
            }
            catch (InvalidOperationException)
            {
                // the writer has ended (aborted or failed) - the frame is dropped
                freeFrames_.Add(frame);
            }
        }

        // No more frames: the sink is finished after the queued frames are written (poll IsCompleted)
        public void Complete()
        {
            if (!queue_.IsAddingCompleted)
                queue_.CompleteAdding();
        }

        // Drops the queued frames, aborts the sink and waits for the writer thread
        public void Abort()
        {
            abortRequested_ = true;
            Complete();

            if (thread_ != null)
                thread_.Join();
            else
                sink_.Abort();
        }

        public void Dispose()
        {
            if (!isCompleted_)
                Abort();
            queue_.Dispose();
        }

        private void Run()
        {
            try
            {
                sink_.Begin(width_, height_, fps_);

                foreach (VideoFrame frame in queue_.GetConsumingEnumerable())
                {
                    if (!abortRequested_)
                    {
                        sink_.AddFrame(frame);
                        Interlocked.Increment(ref numOfWrittenFrames_);
                    }
                    freeFrames_.Add(frame);
                }

                if (abortRequested_)
                    sink_.Abort();
                else
                    sink_.Finish();
            }
            catch (Exception ex)
            {
                error_ = ex;
                sink_.Abort();
                // no more frames are accepted
                Complete();
            }
            finally
            {
                isCompleted_ = true;
            }
        }
    }
}
