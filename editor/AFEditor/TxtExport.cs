using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AFEditor
{
    /**
     * TXT file export/import
     * 
     * Encoding: SHIFT-JIS
     */
    public class TxtExport
    {
        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int memcmp(byte[] b1, byte[] b2, long count);

        private static bool ByteArrayCompare(byte[] b1, byte[] b2)
        {
            return b1.Length == b2.Length && memcmp(b1, b2, b1.Length) == 0;
        }

        public static int Import(CResourceManager resman, string filename, bool appendSoh = false, bool appendLines = false)
        {
            var shiftJis = Encoding.GetEncoding("SHIFT-JIS");

            // Build fast lookup and find max existing ID
            uint currentMax = resman.dialogue.Count > 0
            //uint currentMax = resman.ui.Count > 0
                ? resman.dialogue.Keys.Max()
                //? resman.ui.Keys.Max()
                : 1;

            var uiMap = new Dictionary<string, PackageText>(resman.dialogue.Count);
            //var uiMap = new Dictionary<string, PackageText>(resman.ui.Count);
            foreach (var kv in resman.dialogue)
            //foreach (var kv in resman.ui)								
            {
                int len = (int)kv.Value.sourceLen - 1;
                string key = shiftJis.GetString(kv.Value.source, 0, len);
                uiMap[key] = kv.Value;
            }

            int replaced = 0;
            using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new StreamReader(fs, shiftJis))
            {
                var engText = new StringBuilder(256);
                var jpnText = new StringBuilder(256);
                bool english = false;

                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (line.Length > 0 && line[0] == 'E')
                    {
                        if (!english && jpnText.Length > 0)
                            english = true;

                        engText.AppendLine(line.Substring(1));
                    }
                    else
                    {
                        if (english)
                        {
                            // Finalize text processing
                            jpnText.Replace("\r\n", "\n");
                            jpnText.Remove(jpnText.Length - 1, 1);
                            string jpnKey = jpnText.ToString();

                            string text = engText.ToString().TrimEnd('\r', '\n');
                            if (appendSoh && !text.EndsWith("{soh}") && !text.EndsWith("{dc3}"))
                            {
                                text += "{soh}";
                            }

                            // Update existing or add new entry
                            if (uiMap.TryGetValue(jpnKey, out var pkg))
                            {
                                pkg.translation = text;
                                replaced++;
                            }
                            else if (appendLines)
                            {
                                // Generate new entry with max ID + 1
                                byte[] jpnBytes = shiftJis.GetBytes(jpnKey);
                                byte[] sourceBytes = new byte[jpnBytes.Length + 1];
                                Array.Copy(jpnBytes, sourceBytes, jpnBytes.Length);
                                sourceBytes[jpnBytes.Length] = 0;

                                uint newKey = currentMax + 1;
                                currentMax = newKey; // Update our current maximum

                                var pkgNew = new PackageText
                                {
                                    source = sourceBytes,
                                    sourceLen = (uint)sourceBytes.Length,
                                    sourceString = jpnKey, // Store original text
                                    translation = text
                                };

                                resman.dialogue.Add(newKey, pkgNew);
                                //resman.ui.Add(newKey, pkgNew);
                                replaced++;
                            }

                            // Reset buffers
                            engText.Length = 0;
                            jpnText.Length = 0;
                            english = false;
                        }
                        jpnText.AppendLine(line);
                    }
                }
            }

            return replaced;
        }

        public static void Export(CResourceManager resman, string filename)
        {
            FileStream fs = new FileStream(filename, FileMode.Create);

            StreamWriter writer = new StreamWriter(fs, Encoding.GetEncoding("SHIFT-JIS"));

            foreach (KeyValuePair<UInt32, PackageText> kv in resman.dialogue)
            //foreach (KeyValuePair<UInt32, PackageText> kv in resman.ui)
            {
                if (kv.Value.sourceLen <= 2) continue;

                fs.Write(kv.Value.source, 0, ((int)kv.Value.sourceLen - 1));
                fs.Flush();
                writer.Write("\r\nE");
                writer.Write(kv.Value.translation.Replace("\r\n", "\n").Replace("\n", "\r\nE"));
                writer.Write("\r\n");
                writer.Flush();
                fs.Flush();
            }

            fs.Close();
        }
    }
}
