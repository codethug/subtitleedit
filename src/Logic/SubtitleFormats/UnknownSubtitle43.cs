﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Nikse.SubtitleEdit.Logic.SubtitleFormats
{
    public class UnknownSubtitle43 : SubtitleFormat
    {
        public override string Extension
        {
            get { return ".xml"; }
        }

        public override string Name
        {
            get { return "Unknown 43"; }
        }

        public override bool IsTimeBased
        {
            get { return true; }
        }

        public override bool IsMine(List<string> lines, string fileName)
        {
            var sb = new StringBuilder();
            lines.ForEach(line => sb.AppendLine(line));
            string xmlAsString = sb.ToString().Trim();
            if (xmlAsString.Contains("<subtitle"))
            {
                var xml = new XmlDocument();
                xml.XmlResolver = null;
                try
                {
                    xml.LoadXml(xmlAsString);
                    if (xml.DocumentElement.Name == "parfums")
                    {
                        return xml.DocumentElement.SelectNodes("subtitle").Count > 0;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }
            return false;
        }

        internal static string ConvertToTimeString(TimeCode time)
        {
            return string.Format("{0}:{1:00}", (int)(time.TotalSeconds / 60), (int)(time.TotalSeconds % 60));
        }

        public override string ToText(Subtitle subtitle, string title)
        {
            var xml = new XmlDocument();
            xml.LoadXml("<?xml version=\"1.0\" encoding=\"utf-8\" ?><parfums></parfums>");

            int no = 0;
            foreach (Paragraph p in subtitle.Paragraphs)
            {
                XmlNode paragraph = xml.CreateElement("subtitle");
                string text = p.Text;

                bool first = true;
                foreach (string line in text.Split(Utilities.NewLineChars, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!first)
                    {
                        XmlNode br = xml.CreateElement("br");
                        paragraph.AppendChild(br);
                    }
                    var textNode = xml.CreateTextNode(string.Empty);
                    textNode.InnerText = line;
                    paragraph.AppendChild(textNode);
                    first = false;
                }

                XmlAttribute end = xml.CreateAttribute("end");
                end.InnerText = ConvertToTimeString(p.EndTime);
                paragraph.Attributes.Append(end);

                XmlAttribute start = xml.CreateAttribute("start");
                start.InnerText = ConvertToTimeString(p.StartTime);
                paragraph.Attributes.Append(start);

                xml.DocumentElement.AppendChild(paragraph);
                no++;
            }

            return ToUtf8XmlString(xml).Replace(" xmlns=\"\"", string.Empty);
        }

        public override void LoadSubtitle(Subtitle subtitle, List<string> lines, string fileName)
        {
            _errorCount = 0;

            var sb = new StringBuilder();
            lines.ForEach(line => sb.AppendLine(line));
            var xml = new XmlDocument();
            xml.XmlResolver = null;
            xml.LoadXml(sb.ToString().Trim());

            foreach (XmlNode node in xml.DocumentElement.SelectNodes("subtitle"))
            {
                try
                {
                    var pText = new StringBuilder();
                    foreach (XmlNode innerNode in node.ChildNodes)
                    {
                        switch (innerNode.Name)
                        {
                            case "br":
                                pText.AppendLine();
                                break;
                            case "span":
                                ReadSpan(pText, innerNode);
                                break;

                            default:
                                pText.Append(innerNode.InnerText);
                                break;
                        }
                    }

                    string start = node.Attributes["start"].InnerText;
                    string end = node.Attributes["end"].InnerText;
                    var p = new Paragraph(GetTimeCode(start), GetTimeCode(end), pText.ToString().Replace("   ", " ").Replace("  ", " "));
                    subtitle.Paragraphs.Add(p);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    _errorCount++;
                }
            }
            subtitle.Renumber(1);
        }

        private static void ReadSpan(StringBuilder pText, XmlNode innerNode)
        {
            if (innerNode.HasChildNodes)
            {
                foreach (XmlNode innerInnerNode in innerNode.ChildNodes)
                {
                    if (innerInnerNode.Name == "br")
                    {
                        pText.AppendLine();
                    }
                    else if (innerInnerNode.Name == "span")
                    {
                        ReadSpan(pText, innerInnerNode);
                    }
                    else
                    {
                        pText.Append(innerInnerNode.InnerText);
                    }
                }
            }
            else
            {
                pText.Append(innerNode.InnerText);
            }
        }

        public static TimeCode GetTimeCode(string s)
        {
            string[] arr = s.Split(':');
            string hours, minutes, seconds;
            switch (arr.Length)
            {
                case 2: // mm:ss
                    hours = null;
                    minutes = arr[0];
                    seconds = arr[1];
                    break;
                case 3: // hh:mm:ss
                    hours = arr[0];
                    minutes = arr[1];
                    seconds = arr[2];
                    break;
                default: // ss
                    hours = minutes = null;
                    seconds = s;
                    break;
            }
            return TimeCode.FromTimestampTokens(hours, minutes, seconds, null);
        }

    }
}
