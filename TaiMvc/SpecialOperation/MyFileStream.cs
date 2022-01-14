using Microsoft.AspNetCore.Mvc;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;

namespace TaiMvc.SpecialOperation
{
    public class MyFileStream : FileStream
    {

        private readonly Stopwatch _stopWatch = new();

        public MyFileStream(string path, FileMode mode) : base(path, mode)
        {
            _stopWatch.Start();
        }

        public override void Close()
        {
            _stopWatch.Stop();
            System.Diagnostics.Debug.WriteLine("Time: " + _stopWatch.ElapsedMilliseconds.ToString() + "ms");
            base.Close();
        }

    }
}
