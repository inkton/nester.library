/*
    Copyright (c) 2017 Inkton.

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the "Software"),
    to deal in the Software without restriction, including without limitation
    the rights to use, copy, modify, merge, publish, distribute, sublicense,
    and/or sell copies of the Software, and to permit persons to whom the Software
    is furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
    OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
    CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE
    OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.IO;
using Xamarin.Forms;

namespace Inkton.Nester.Helpers
{
    public interface ILogService
    {
        string Path { get; set; }
        long MaxSize { get; set; }
        long MaxFiles { get; set; }
        Severity Severity { get; set; }
        void Trace(
            string info,
            string location = "",
            Severity severity = Severity.SeverityInfo);
    }

    public class LogService
    {
        private string _path;

        public LogService()
        {
            Path = System.IO.Path.Combine(
             System.IO.Path.GetTempPath(),
                Application.Current.ClassId + "-log");
        }

        public string Path
        {
            set
            {
                _path = value;
                if (!Directory.Exists(_path))
                {
                    Directory.CreateDirectory(_path);
                }
            }
            get
            {
                return _path;
            }
        }

        public long MaxSize { get; set; } = 1024 * 256;

        public long MaxFiles { get; set; } = 3;

        public Severity Severity { get; set; } = Severity.SeverityInfo;

        public void Trace(
            string info,
            string location = "",
            Severity severity = Severity.SeverityInfo)
        {
            if (severity <= Severity)
            {
                return;
            }

            try
            {
                string path = this.Path + @"\current.log";
                StreamWriter writer = null;

                if (File.Exists(path))
                {
                    FileInfo fi = new FileInfo(path);

                    if (fi.Length >= MaxSize)
                    {
                        string newPath = this.Path + string.Format(@"\{0}",
                                System.DateTime.UtcNow.ToString("yyyy-MM-ddTHHZ"));

                        File.Move(path, newPath);

                        string[] files = Directory.GetFiles(
                            this.Path, "*.*", SearchOption.TopDirectoryOnly);

                        if (files.Length > MaxFiles)
                        {
                            string oldestFile = null;
                            DateTime earliestTime = DateTime.Now;

                            foreach (string file in files)
                            {
                                if (!file.EndsWith(".log", StringComparison.CurrentCulture))
                                    try
                                    {
                                        DateTime fileTime = DateTime.Parse(
                                            file, null, System.Globalization.DateTimeStyles.RoundtripKind);

                                        if (oldestFile == null ||
                                                fileTime < earliestTime)
                                        {
                                            earliestTime = fileTime;
                                            oldestFile = file;
                                        }
                                    }
                                    catch (FormatException e)
                                    {
                                        writer = File.AppendText(path);
                                        writer.WriteLine("'{0}', 'LogService.Trace'", e.Message);
                                        return;
                                    }
                            }

                            if (oldestFile != null)
                            {
                                File.Delete(this.Path + @"\" + oldestFile);
                            }
                        }
                    }
                }

                WaitForFile(path);
                writer = File.AppendText(path);
                writer.WriteLine(@"{0}, {1}\n", info, location);
            }
            catch (Exception e) 
            {
                System.Console.Write(e.Message); 
            }
        }

        /// <summary>
        /// Blocks until the file is not locked any more.
        /// </summary>
        /// <param name="fullPath"></param>
        private bool WaitForFile(string fullPath)
        {
            // Thank you -> https://stackoverflow.com/questions/41290/file-access-strategy-in-a-multi-threaded-environment-web-app#41559

            int numTries = 0;
            while (true)
            {
                ++numTries;
                try
                {
                    // Attempt to open the file exclusively.
                    using (FileStream fs = new FileStream(fullPath,
                        FileMode.Open, FileAccess.ReadWrite,
                        FileShare.None, 100))
                    {
                        fs.ReadByte();

                        // If we got this far the file is ready
                        break;
                    }
                }
                catch (Exception)
                {
                    //Log.LogWarning(
                    //   "WaitForFile {0} failed to get an exclusive lock: {1}",
                    //    fullPath, ex.ToString());

                    if (numTries > 10)
                    {
                        //Log.LogWarning(
                        //    "WaitForFile {0} giving up after 10 tries",
                        //    fullPath);
                        return false;
                    }

                    // Wait for the lock to be released
                    System.Threading.Thread.Sleep(500);
                }
            }

            //Log.LogTrace("WaitForFile {0} returning true after {1} tries",
            //    fullPath, numTries);
            return true;
        }
    }
}
