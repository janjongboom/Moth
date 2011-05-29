using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using Moth.Core.Properties;

namespace Moth.Core.Externals
{
    public class CssTidy
    {
        private readonly IOutputCacheProvider _provider;
        private readonly string _filename;

        private readonly object _lock = new object();

        public CssTidy()
        {
            _provider = MothAction.CacheProvider;
            _filename = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "csstidy.exe");
        }

        public string Tidy(string fullLocalPath)
        {
            if (!File.Exists(_filename))
                ExtractFile();

            Process process = new Process();
            ProcessStartInfo si = new ProcessStartInfo(_filename, string.Format("\"{0}\" --silent=true", fullLocalPath));
            si.RedirectStandardOutput = true;
            si.UseShellExecute = false;
            si.CreateNoWindow = true;
            si.WindowStyle = ProcessWindowStyle.Hidden;

            process.StartInfo = si;

            process.Start();

            string s = process.StandardOutput.ReadToEnd();

            process.WaitForExit();

            return s;
        }

        private void ExtractFile()
        {
            lock (_lock)
            {
                if (File.Exists(_filename)) return; // if during lock file is created, continue

                File.WriteAllBytes(_filename, Resources.csstidy);
            }
        }
    }
}