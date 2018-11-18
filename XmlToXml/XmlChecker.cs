using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Reflection;
using System.Xml.Linq;

namespace XmlToXml
{
    abstract class XmlChecker
    {

        public enum ErrorCode
        {
            [Description("Content is empty")] NullString,
            [Description("Format error")] FormatError,
        };
        public abstract bool Check(String xml);

        protected void Err(ErrorCode err)
        {
            err_code = err;
            err_desc = GetDescription(err);
        }

        public static string GetDescription(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());
            DescriptionAttribute[] attributes =
                  (DescriptionAttribute[])fi.GetCustomAttributes(
                  typeof(DescriptionAttribute), false);
            return (attributes.Length > 0) ? attributes[0].Description : value.ToString();
        }

        public String err_desc
        {
            get; protected set;
        }
        public ErrorCode err_code
        {
            get; protected set;
        }
        public String detailed_err {
            get; protected set;
        }
    }

    class SimpleXmlChecker: XmlChecker
    {
        public SimpleXmlChecker(int ignore_root =1)  // 0 none 1 ignore_err 2 force_add_xml
        {
            b_ignore_root = ignore_root>=1;
            b_default_add_root = ignore_root >= 2;
        }

        public bool TryCheckXml(string xml)
        {
            if (b_default_add_root)
            {
                string xml2 = "<xml>" + xml + "</xml>";
                bool res = FormatXml(xml2);
                if (res)
                {
                    var xmlroot = doc.Elements().First();
                    if (xmlroot.Nodes().Count() == 1 && xmlroot.Elements().Count() == 1)
                    {
                        if (xmlroot.Elements().First().Name.ToString() == "xml")
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            return false;
        }
        public override bool Check(string xml)
        {
            if (String.IsNullOrEmpty(xml))
            {
                Err(ErrorCode.NullString);
                return false;
            }

            bool res = TryCheckXml(xml);
            if (!res)
            {
                res = FormatXml(xml);
            }

            if (!res && b_ignore_root && !b_default_add_root)
            {
                ignored = true;
                res = FormatXml("<xml>"+xml+"</xml>");
                if (!res)
                {
                    ignored = false;
                    res = FormatXml(xml);
                }
            } 

            return res;
        }
         
        bool FormatXml(string xml)
        {
            try
            {
                doc = XDocument.Parse(xml);
                return true;
            }
            catch (Exception e)
            {
                Err(ErrorCode.FormatError);
                detailed_err = e.Message;
                return false;
            }
        }
        readonly bool b_ignore_root = false;
        readonly bool b_default_add_root = false;
        bool ignored = false;
        public XDocument doc { get;protected set; }
    }


    class CombineXmlNL
    {
        protected static bool isLetter(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }
        public static bool CombineOldXml(out string result ,string newxml, string oldxml)
        {
            newxml = newxml.Replace("\r\n", "\n");
            oldxml = oldxml.Replace("\r\n", "\n");
            result = "";
            Dictionary<string, int> blank_lines;
            {
                int i = 0;
                int cnn = 0, cnnp = 0;
                Stack<int> et = new Stack<int>();
                char ch = '\0', chp = '\0';
                bool notBlankLine = false;

                int iComm = 0;
                Stack<string> paths = new Stack<string>();
                string pathnow = "/";

                blank_lines = new Dictionary<string, int>();

                for (; i < oldxml.Length; i++)
                {
                    chp = ch;
                    ch = oldxml[i];
                    bool b_not_blank = true;
                    switch (ch)
                    {
                    case '<':
                        et.Push(i);
                        break;
                    case '>':

                        int ei = et.Pop();
                        if (et.Count == 0)
                        {
                            bool isClosed = false, isComm = false;
                            string tagn = oldxml.Substring(ei + 1, i - ei - 1).Trim();
                            if (tagn.Length >= 1)
                            {
                                if (tagn[0] == '!')
                                {
                                    iComm++;
                                    isComm = true;
                                    tagn = "!" + iComm;
                                    isClosed = true;
                                }

                                if (isComm || isLetter(tagn[0]))
                                {
                                    if (!isComm)
                                    {
                                        iComm = 0;
                                    }
                                    cnnp = cnn;
                                    cnn = 0;
                                    if (tagn.Length >= 1 && tagn.Last() == '/')
                                    {
                                        tagn = tagn.Substring(0, tagn.Length - 1).Trim();
                                        isClosed = true;
                                    }
                                    int p = tagn.IndexOf(' ');
                                    if (p >= 0)
                                    {
                                        tagn = tagn.Substring(0, p);
                                    }
                                    paths.Push(tagn);
                                    pathnow += tagn + '/';

                                    if (blank_lines.ContainsKey(pathnow))
                                    {
                                        cnnp = Math.Max(cnnp, blank_lines[pathnow]);
                                    }
                                    blank_lines[pathnow] = cnnp;
                                }

                                if (isClosed || tagn[0] == '/')
                                {
                                    cnnp = cnn;
                                    cnn = 0;
                                    if (tagn[0] == '/')
                                    {
                                        tagn = tagn.Substring(1);
                                    }
                                    if (paths.Pop() != tagn)
                                    {
                                        return false;
                                    }
                                    pathnow = "/";
                                    foreach (var p in paths.Reverse())
                                    {
                                        pathnow += p + '/';
                                    }
                                }
                            }
                        }
                        break;
                    case '\n':
                        b_not_blank = false;
                        if (et.Count == 0 && (chp == '\n' || !notBlankLine))
                        {
                            cnn++;
                        }
                        notBlankLine = false;
                        break;
                    case ' ':
                    case '\t':
                        b_not_blank = false;
                        break;
                    default:

                        break;
                    }  // end of switch
                    if (b_not_blank)
                    {
                        notBlankLine = true;
                    }
                }   // end of For
            }



            {
                int i = 0; int nli = -1; int addedi = 0;
                int cnn = 0, cnnp = 0;
                Stack<int> et = new Stack<int>();
                char ch = '\0', chp = '\0';
                bool notBlankLine = false;

                int iComm = 0;
                Stack<string> paths = new Stack<string>();
                string pathnow = "/";

                for (; i < newxml.Length; i++)
                {
                    chp = ch;
                    ch = newxml[i];
                    bool b_not_blank = true;
                    switch (ch)
                    {
                    case '<':
                        et.Push(i);
                        break;
                    case '>':

                        int ei = et.Pop();
                        if (et.Count == 0)
                        {
                            bool isClosed = false,isComm = false;
                            string tagn = newxml.Substring(ei + 1, i - ei - 1).Trim();
                            if (tagn.Length >= 1)
                            {
                                if (tagn[0] == '!')
                                {
                                    iComm++;
                                    isComm = true;
                                    tagn = "!" + iComm;
                                    isClosed = true;
                                }

                                if (isComm || isLetter(tagn[0]))
                                {
                                    if (!isComm)
                                    {
                                        iComm = 0;
                                    }
                                    cnnp = cnn;
                                    cnn = 0;
                                    if (tagn.Length >= 1 && tagn.Last() == '/')
                                    {
                                        tagn = tagn.Substring(0, tagn.Length - 1).Trim();
                                        isClosed = true;
                                    }
                                    int p = tagn.IndexOf(' ');
                                    if (p >= 0)
                                    {
                                        tagn = tagn.Substring(0, p);
                                    }
                                    paths.Push(tagn);
                                    pathnow += tagn + '/';

                                    if (blank_lines.ContainsKey(pathnow))
                                    {
                                        cnnp = blank_lines[pathnow] - cnnp;
                                        
                                        result = result.Insert(nli+1+ addedi, new string('\n', cnnp));
                                        addedi += cnnp;
                                    }
                                }

                                if (isClosed || tagn[0] == '/')
                                {
                                    cnnp = cnn;
                                    cnn = 0;
                                    if (tagn[0] == '/')
                                    {
                                        tagn = tagn.Substring(1);
                                    }
                                    if (paths.Pop() != tagn)
                                    {
                                        return false;
                                    }
                                    pathnow = "/";
                                    foreach (var p in paths.Reverse())
                                    {
                                        pathnow += p + '/';
                                    }
                                }
                            }
                        }
                        break;
                    case '\n':
                        b_not_blank = false;
                        if (et.Count == 0 && (chp == '\n'|| !notBlankLine))
                        {
                            cnn++;
                        }
                        notBlankLine = false;
                        nli = i;
                        break;
                    case ' ': case '\t':
                        b_not_blank = false;
                        break;
                    default:

                        break;
                    }  // end of switch
                    if (b_not_blank)
                    {
                        notBlankLine = true;
                    }
                    result += ch;
                }   // end of For

            }

            return true;
        }
    }


}
