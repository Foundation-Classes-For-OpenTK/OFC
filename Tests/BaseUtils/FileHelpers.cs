﻿/*
 * Copyright © 2017-2023 EDDiscovery development team
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this
 * file except in compliance with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software distributed under
 * the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
 * ANY KIND, either express or implied. See the License for the specific language
 * governing permissions and limitations under the License.
 */

using System.IO;
using System.Text;

namespace BaseUtils
{
    public static class FileHelpers
    {
        public static string TryReadAllTextFromFile(string filename, Encoding encoding = null, FileShare fs = FileShare.ReadWrite)
        {
            if (File.Exists(filename))
            {
                try
                {
                    using (Stream s = File.Open(filename, FileMode.Open, FileAccess.Read, fs))
                    {
                        if (encoding == null)
                            encoding = Encoding.UTF8;

                        using (StreamReader sr = new StreamReader(s, encoding, true, 1024))
                            return sr.ReadToEnd();
                    }
                }
                catch
                {
                    return null;
                }
            }
            else
                return null;
        }

        public static string[] TryReadAllLinesFromFile(string filename)
        {
            if (File.Exists(filename))
            {
                try
                {
                    return File.ReadAllLines(filename, Encoding.UTF8);
                }
                catch
                {
                    return null;
                }
            }
            else
                return null;
        }

        public static bool TryAppendToFile(string filename, string content)
        {
            if (File.Exists(filename))
            {
                try
                {
                    File.AppendAllText(filename, content);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            else
                return false;
        }
        public static bool TryWriteToFile(string filename, string content)
        {
            try
            {
                File.WriteAllText(filename, content);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // if erroriftoobig = false, returns top folder if above is too big for directory depth
        public static DirectoryInfo GetDirectoryAbove( this DirectoryInfo di, int above, bool errorifpastroot = false )        
        {
            while( above > 0 && di.Parent != null )
            {
                di = di.Parent;
                above--;
            }

            return (errorifpastroot && above >0 ) ? null : di;
        }

        public static bool DeleteFileNoError(string path)
        {
            try
            {
                File.Delete(path);
                return true;
            }
            catch
            {       // on purpose no error - thats the point of it
                //System.Diagnostics.Debug.WriteLine("Exception " + ex);
                return false;
            }
        }

        public static bool TryCopy(string source, string file, bool overwrite)
        {
            try
            {
                File.Copy(source, file, overwrite);
                return true;
            }
            catch
            {       // on purpose no error - thats the point of it
                //System.Diagnostics.Debug.WriteLine("Exception " + ex);
                return false;
            }
        }

        public static bool CreateDirectoryNoError(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                return true;
            }
            catch
            {       // on purpose no error - thats the point of it
                //System.Diagnostics.Debug.WriteLine("Exception " + ex);
                return false;
            }
        }

        public static string AddSuffixToFilename(this string file, string suffix)
        {
            return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(file), System.IO.Path.GetFileNameWithoutExtension(file) + suffix) + System.IO.Path.GetExtension(file);
        }

        // is file not open for unshared read access - may be because it does not exist note
        public static bool IsFileAvailable(string file)
        {
            try
            {
                FileStream f = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None);
                f.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
