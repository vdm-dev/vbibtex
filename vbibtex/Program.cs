//
//  Copyright (c) 2014 Dmitry Lavygin (vdm.inbox@gmail.com)
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in all
//  copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//  SOFTWARE.
//


using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Xml;

namespace vbibtex
{
    class Program
    {
        class BibEntry
        {
            public string id;
            public string type;
            public Dictionary<string, string> data;

            public BibEntry()
            {
                id = "";
                type = "";                
                data = new Dictionary<string, string>();
            }
        }

        private static string processUrl(string url)
        {
            //return url.Replace("%", "\\%").Replace("$", "\\$").Replace("_", "\\_").Replace("&", "\\&").Replace("#", "\\#");
            return "\\url{" + url + "}";
        }

        private static bool isNative(string text)
        {
            Match result = Regex.Match(text, @"[А-Яа-яёЁ]+", RegexOptions.IgnoreCase);

            return result.Success;
        }

        private static int compareSimple(KeyValuePair<string, string> a, KeyValuePair<string, string> b)
        {
            return a.Value.CompareTo(b.Value);
        }

        private static int compareEntries(BibEntry a, BibEntry b)
        {
            string textA = a.data["title"];
            string textB = b.data["title"];

            if (a.data.ContainsKey("author"))
                textA = a.data["author"] + textA;

            if (b.data.ContainsKey("author"))
                textB = b.data["author"] + textB;

            textA = textA.ToLower();
            textB = textB.ToLower();

            int length = Math.Max(textA.Length, textB.Length);

            int result = 0;

            for (int i = 0; i < length; ++i)
            {
                if (textA[i] != textB[i])
                {
                    bool isLetterA = ((textA[i] >= 'a') && (textA[i] <= 'z')) || ((textA[i] >= 'а') && (textA[i] <= 'я')) || (textA[i] == 'ё');
                    bool isLetterB = ((textB[i] >= 'a') && (textB[i] <= 'z')) || ((textB[i] >= 'а') && (textB[i] <= 'я')) || (textB[i] == 'ё');

                    if (isLetterA && isLetterB)
                    {
                        result = textA[i].CompareTo(textB[i]);
                    }
                    else if (isLetterA && !isLetterB)
                    {
                        result = 1;
                    }
                    else if (!isLetterA && isLetterB)
                    {
                        result = -1;
                    }
                    else
                    {
                        if (textA[i] == ',')
                        {
                            result = -1;
                        }
                        else if (textB[i] == ',')
                        {
                            result = 1;
                        }
                        else
                        {
                            result = 0;
                        }
                    }

                    break;
                }
            }

            if (result == 0)
                result = textB.Length - textA.Length;

            return result;
        }

        private static List<KeyValuePair<string, string>> processEntries(List<BibEntry> entries, bool native = true)
        {
            List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();

            string ptotal = "с.";
            string prange = "С.";
            string vol = "Т.";

            if (!native)
            {
                ptotal = "p.";
                prange = "PP.";
                vol = "Vol.";
            }

            foreach (BibEntry e in entries)
            {
                string[] authors = null;

                if (e.data.ContainsKey("author"))
                    authors = e.data["author"].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                string line = "";

                Console.WriteLine("<INFO> Processing entry \"{0}\"", e.id);

                if (e.data["title"].Length < 1)
                {
                    Console.WriteLine("<ERROR> An entry without title");
                    continue;
                }

                if (e.type == "article")
                {
                    if ((authors == null) || authors.Length < 1)
                    {
                        Console.WriteLine("<ERROR> The entry has no authors");
                        continue;
                    }

                    string firstname = "";
                    string lastname = "";
                    string midname = "";

                    string[] author = authors[0].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    lastname = author[0];

                    if (author.Length < 2)
                    {
                        Console.WriteLine("<ERROR> An author without first name");
                        continue;
                    }

                    firstname = author[1];

                    if (author.Length > 2)
                        midname = author[2] + ' ';

                    line += lastname + ", " + firstname + ' ' + midname;
                    line += e.data["title"] + " / ";

                    for (int i = 0; i < authors.Length; ++i)
                    {
                        midname = "";

                        author = authors[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        lastname = author[0];

                        if (author.Length < 2)
                        {
                            Console.WriteLine("<ERROR> An author without first name");
                            continue;
                        }

                        firstname = author[1];

                        if (author.Length > 2)
                            midname = author[2] + ' ';

                        if (i > 0)
                            line += ", ";

                        line += firstname + ' ' + midname + lastname;
                    }

                    line += " // " + e.data["journal"] + ".~--- ";

                    if (e.data.ContainsKey("address") && (e.data["address"].Length > 0))
                    {
                        if (e.data["address"] == "Москва")
                            e.data["address"] = "М.";

                        line += e.data["address"];

                        if (e.data.ContainsKey("publisher"))
                        {
                            line += " : " + e.data["publisher"];
                        }

                        line += ", ";
                    }
                    else if (e.data.ContainsKey("publisher"))
                    {
                        line += e.data["publisher"] + ", ";
                    }

                    line += e.data["year"] + ".";

                    if (e.data.ContainsKey("volume"))
                    {
                        line += "~--- " + vol + " " + e.data["volume"] + ".";
                    }

                    if (e.data.ContainsKey("number"))
                    {
                        line += "~--- \\No " + e.data["number"] + ".";
                    }

                    if (e.data.ContainsKey("pages"))
                    {
                        line += "~--- " + prange + " " + e.data["pages"] + ".";
                    }

                    if (e.data.ContainsKey("url"))
                    {
                        line += "~--- Режим доступа: " + processUrl(e.data["url"]) + ".";
                    }
                }
                else if ((e.type == "book") || (e.type == "conference"))
                {
                    if ((authors == null) || authors.Length < 1)
                    {
                        Console.WriteLine("<ERROR> The entry has no authors");
                        continue;
                    }

                    string firstname = "";
                    string lastname = "";
                    string midname = "";

                    string[] author = authors[0].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    lastname = author[0];

                    if (author.Length < 2)
                    {
                        Console.WriteLine("<ERROR> An author without first name");
                        continue;
                    }

                    firstname = author[1];

                    if (author.Length > 2)
                        midname = author[2] + ' ';

                    line += lastname + ", " + firstname + ' ' + midname;
                    line += e.data["title"] + " / ";

                    for (int i = 0; i < authors.Length; ++i)
                    {
                        midname = "";

                        author = authors[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        lastname = author[0];

                        if (author.Length < 2)
                        {
                            Console.WriteLine("<ERROR> An author without first name");
                            continue;
                        }

                        firstname = author[1];

                        if (author.Length > 2)
                            midname = author[2] + ' ';

                        if (i > 0)
                            line += ", ";

                        line += firstname + ' ' + midname + lastname;
                    }

                    if (e.type == "conference")
                    {
                        line += " // " + e.data["booktitle"];

                        if (e.data.ContainsKey("series"))
                        {
                            line += ". " + e.data["series"];
                        }
                    }

                    line += ".~--- ";

                    if (e.data.ContainsKey("address") && (e.data["address"].Length > 0))
                    {
                        if (e.data["address"] == "Москва")
                            e.data["address"] = "М.";

                        line += e.data["address"];

                        if (e.data.ContainsKey("publisher"))
                        {
                            line += " : " + e.data["publisher"];
                        }

                        line += ", ";
                    }
                    else if (e.data.ContainsKey("publisher"))
                    {
                        line += e.data["publisher"] + ", ";
                    }

                    line += e.data["year"] + ".";

                    if (e.data.ContainsKey("pages"))
                    {
                        if (e.type != "conference")
                        {
                            line += "~--- " + e.data["pages"] + " " + ptotal;
                        }
                        else
                        {
                            line += "~--- " + prange + " " + e.data["pages"] + ".";
                        }
                    }
                }
                else if (e.type == "manual")
                {
                    line += e.data["title"] + " / " + authors[0];

                    if (!line.EndsWith("."))
                        line += '.';

                    line += "~--- " + e.data["year"] + ".";

                    if (e.data.ContainsKey("pages"))
                    {
                        line += "~--- " + e.data["pages"] + " " + ptotal;
                    }
                }
                else if (e.type == "patent")
                {
                    line += e.data["type"].Replace("%", "\\No~" + e.data["number"]) + ". ";
                    line += e.data["title"] + " / ";

                    string firstname = "";
                    string lastname = "";
                    string midname = "";

                    string[] author;


                    for (int i = 0; i < authors.Length; ++i)
                    {
                        midname = "";

                        author = authors[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        lastname = author[0];

                        if (author.Length < 2)
                        {
                            Console.WriteLine("<ERROR> An author without first name");
                            continue;
                        }

                        firstname = author[1];

                        if (author.Length > 2)
                            midname = author[2] + ' ';

                        if (i > 0)
                            line += ", ";

                        line += lastname + ' ' + firstname + ' ' + midname;
                    }

                    line += "; заявитель и правообладатель " + e.data["assignee"] + ".";
                    line += "~--- \\No~" + e.data["code"] + " ; заявл. " + e.data["dayfiled"] + "." + e.data["monthfiled"] + "." + e.data["yearfiled"] + " ; ";
                    line += "опубл. " + e.data["day"] + "." + e.data["month"] + "." + e.data["year"] + ".";
                }
                else if (e.type == "standard")
                {
                    line += e.data["institution"] + ". " + e.data["title"];
                    line += ".~--- ";

                    if (e.data.ContainsKey("address"))
                    {
                        if (e.data["address"] == "Москва")
                            e.data["address"] = "М.";

                        line += e.data["address"];

                        if (e.data.ContainsKey("publisher"))
                        {
                            line += " : " + e.data["publisher"];
                        }

                        line += ", ";
                    }
                    else if (e.data.ContainsKey("publisher"))
                    {
                        line += e.data["publisher"] + ", ";
                    }

                    line += e.data["year"] + ".";

                    if (e.data.ContainsKey("pages"))
                    {
                        line += "~--- " + e.data["pages"] + " " + ptotal;
                    }
                }
                else if (e.type == "phdthesis")
                {
                    if ((authors == null) || authors.Length < 1)
                    {
                        Console.WriteLine("<ERROR> The entry has no authors");
                        continue;
                    }

                    string[] author = authors[0].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    line += author[0] + ", ";

                    if (author.Length < 2)
                    {
                        Console.WriteLine("<ERROR> An author without first name");
                        continue;
                    }

                    line += author[1] + ' ';

                    if (author.Length > 2)
                        line += author[2] + ' ';

                    line += e.data["title"] + " : " + e.data["type"];

                    if (e.data.ContainsKey("code"))
                        line += " : " + e.data["code"];

                    line += " / " + e.data["fullname"] + ".~--- ";

                    if (e.data.ContainsKey("address"))
                    {
                        line += e.data["address"] + ", ";
                    }

                    line += e.data["year"] + ".";

                    if (e.data.ContainsKey("pages"))
                    {
                        line += "~--- " + e.data["pages"] + " " + ptotal;
                    }
                }
                else if (e.type == "electronic")
                {
                    if ((authors == null) || authors.Length < 1)
                    {
                        Console.WriteLine("<ERROR> The entry has no authors");
                        continue;
                    }

                    if (authors.Length > 1)
                    {
                        string firstname = "";
                        string lastname = "";
                        string midname = "";

                        string[] author = authors[0].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        lastname = author[0];

                        if (author.Length < 2)
                        {
                            Console.WriteLine("<ERROR> An author without first name");
                            continue;
                        }

                        firstname = author[1];

                        if (author.Length > 2)
                            midname = author[2] + ' ';

                        line += lastname + ", " + firstname + ' ' + midname;
                        line += e.data["title"] + " [Электронный ресурс] / ";

                        for (int i = 0; i < authors.Length; ++i)
                        {
                            midname = "";

                            author = authors[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                            lastname = author[0];

                            if (author.Length < 2)
                            {
                                Console.WriteLine("<ERROR> An author without first name");
                                continue;
                            }

                            firstname = author[1];

                            if (author.Length > 2)
                                midname = author[2] + ' ';

                            if (i > 0)
                                line += ", ";

                            line += firstname + ' ' + midname + lastname;
                        }
                    }
                    else
                    {
                        line += e.data["title"] + " [Электронный ресурс] / " + authors[0];
                    }

                    line += ".~--- Режим доступа: " + processUrl(e.data["url"]) + ".";
                }

                if (!e.data.ContainsKey("url"))
                {
                    if (e.data.ContainsKey("isbn"))
                    {
                        line += "~--- ISBN " + e.data["isbn"] + ".";
                    }
                    else if (e.data.ContainsKey("issn"))
                    {
                        line += "~--- ISSN " + e.data["issn"] + ".";
                    }
                }

                result.Add(new KeyValuePair<string, string>(e.id, line));
            }

            return result;
        }

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("<ERROR> No file to process");
                return;
            }

            string data = "";

            try
            {
                StreamReader file = new StreamReader(args[0], Encoding.GetEncoding(1251));
                data = file.ReadToEnd();
                file.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("<ERROR> {0}", e.ToString());
                return;
            }

            string outputFile = Path.ChangeExtension(args[0], ".tex");

            if (args.Length > 2)
                outputFile = args[1];

            StreamWriter output;

            try
            {
                output = new StreamWriter(outputFile, false, Encoding.GetEncoding(1251));
            }
            catch (Exception e)
            {
                Console.WriteLine("<ERROR> {0}", e.ToString());
                return;
            }

            List<BibEntry> foreign = new List<BibEntry>();
            List<BibEntry> native = new List<BibEntry>();

            BibEntry entry;

            while (data.Length > 0)
            {
                int pos = data.IndexOf('@');

                if (pos < 0)
                    break;

                data = data.Substring(pos + 1);

                pos = data.IndexOf('{');

                if (pos < 0)
                    break;

                entry = new BibEntry();

                entry.type = data.Substring(0, pos).ToLower().Trim();

                data = data.Substring(pos + 1);

                pos = data.IndexOf(',');

                if (pos < 0)
                    break;

                entry.id = data.Substring(0, pos).Trim();

                data = data.Substring(pos + 1);

                int level = 1;

                pos = 0;

                bool writeValue = false;

                string key = "";
                string value = "";

                while (level > 0)
                {
                    if (pos >= data.Length)
                        break;

                    switch (data[pos])
                    {
                        case '\r':
                        case '\n':
                            break;
                        case ',':
                            if (level < 2)
                            {
                                key = key.ToLower().Trim();
                                value = value.Replace('\t', ' ').Trim();
                                entry.data.Add(key, value);
                                key = "";
                                value = "";
                                writeValue = false;
                            }
                            else
                            {
                                goto default;
                            }
                            break;
                        case '=':
                            if (level < 2)
                            {
                                writeValue = true;
                            }
                            else
                            {
                                goto default;
                            }
                            break;
                        case '{':
                            level++;
                            break;
                        case '}':
                            if (level < 2)
                            {
                                key = key.ToLower().Trim();
                                value = value.Replace('\t', ' ').Trim();
                                entry.data.Add(key, value);
                                key = "";
                                value = "";
                                writeValue = false;
                            }
                            level--;
                            break;
                        default:
                            if (writeValue)
                            {
                                value += data[pos];
                            }
                            else
                            {
                                key += data[pos];
                            }
                            break;
                    }

                    pos++;
                }

                if (isNative(entry.data["title"]) || (entry.type == "patent"))
                {
                    native.Add(entry);
                }
                else
                {
                    foreign.Add(entry);
                }

                data = data.Substring(pos + 1);
            }

            //native.Sort(compareEntries);
            //foreign.Sort(compareEntries);

            output.WriteLine(@"\begin{thebibliography}{999}");
            output.WriteLine();

            List<KeyValuePair<string, string>> rNative = processEntries(native, true);
            List<KeyValuePair<string, string>> rForeign = processEntries(foreign, false);

            rNative.Sort(compareSimple);
            rForeign.Sort(compareSimple);

            foreach (KeyValuePair<string, string> pair in rNative)
            {
                output.WriteLine(@"\bibitem{" + pair.Key + @"}");
                output.WriteLine(pair.Value);
                output.WriteLine();
            }

            foreach (KeyValuePair<string, string> pair in rForeign)
            {
                output.WriteLine(@"\bibitem{" + pair.Key + @"}");
                output.WriteLine(pair.Value);
                output.WriteLine();
            }

            output.WriteLine(@"\end{thebibliography}");

            output.Close();
        }
    }
}
