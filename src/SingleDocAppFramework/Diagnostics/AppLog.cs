using System.Diagnostics;
using System.Text;

namespace SingleDocAppFramework.Diagnostics
{
    // In-memory application log.
    // After RedirectConsoleAndTrace() everything written to Console and Trace/Debug
    // goes to this log (displayed by WndLog, optionally saved to a file).
    public static class AppLog
    {
        private static StringBuilder            log_                = new StringBuilder(32000);
        private static TextWriter?              writer_             = null;     // synchronized: FileSystemWatcher etc. may log from other threads
        private static TextWriter?              originalOut_        = null;
        private static TextWriterTraceListener? traceListener_      = null;

        public static void RedirectConsoleAndTrace()
        {
            if (writer_ != null)
                return;

            writer_         = TextWriter.Synchronized(new StringWriter(log_));
            originalOut_    = Console.Out;
            Console.SetOut(writer_);

            // redirect 'Debug' and 'Trace'
            traceListener_  = new TextWriterTraceListener(writer_);
            Trace.Listeners.Add(traceListener_);
            Debug.AutoFlush = true;
        }

        public static void Restore()
        {
            if (writer_ == null)
                return;

            if (traceListener_ != null)
            {
                Trace.Listeners.Remove(traceListener_);
                traceListener_ = null;
            }

            if (originalOut_ != null)
                Console.SetOut(originalOut_);

            writer_.Close();
            writer_         = null;
            originalOut_    = null;
        }

        public static string GetText()
        {
            lock (GetLockObject())
            {
                return log_.ToString();
            }
        }

        public static void Clear()
        {
            lock (GetLockObject())
            {
                log_.Clear();
            }
        }

        public static void SaveTo(string filePath)
        {
            File.WriteAllText(filePath, GetText());
        }

        // TextWriter.Synchronized() locks on the writer instance, so we use the same lock
        private static object GetLockObject()
        {
            return (object?)writer_ ?? log_;
        }
    }
}
