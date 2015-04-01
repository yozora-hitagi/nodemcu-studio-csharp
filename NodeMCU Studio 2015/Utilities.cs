using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Resources;

namespace NodeMCU_Studio_2015
{
    static class Utilities
    {
        public static void ResourceToList(String resourcePath, ICollection<string> list)
        {
            var sri = Application.GetResourceStream(new Uri(resourcePath, UriKind.Relative));
            if (sri == null) return;

            using (var sr = new StreamReader(sri.Stream))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    list.Add(Regex.Unescape(line));
                }
            }
        }

        public static string Escape(string command)
        {
            var output = new StringBuilder(command.Length);
            foreach (var c in command)
            {
                switch (c)
                {
                    case '\a':
                        output.AppendFormat("{0}{1}", '\\', 'a');
                        break;

                    case '\b':
                        output.AppendFormat("{0}{1}", '\\', 'b');
                        break;

                    case '\f':
                        output.AppendFormat("{0}{1}", '\\', 'f');
                        break;

                    case '\n':
                        output.AppendFormat("{0}{1}", '\\', 'n');
                        break;

                    case '\r':
                        output.AppendFormat("{0}{1}", '\\', 'r');
                        break;

                    case '\t':
                        output.AppendFormat("{0}{1}", '\\', 't');
                        break;

                    case '\v':
                        output.AppendFormat("{0}{1}", '\\', 'v');
                        break;

                    case '\'':
                        output.AppendFormat("{0}{1}", '\\', '\'');
                        break;

                    case '\"':
                        output.AppendFormat("{0}{1}", '\\', '\"');
                        break;

                    case '\\':
                        output.AppendFormat("{0}{1}", '\\', '\\');
                        break;

                    default:
                        output.Append(c);
                        break;
                }
            }

            return output.ToString();
        }
    }
}
