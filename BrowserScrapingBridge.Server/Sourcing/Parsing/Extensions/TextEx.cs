#nullable disable warnings
using HtmlAgilityPack;
using System.Collections.Specialized;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace BrowserScrapingBridge.Server.Sourcing.Parsing.Extensions
{
    public static class TextEx
    {
        [Flags]
        public enum XPathOption
        {
            InnerHtml = 0x0,
            OuterHtml = 0x1,
            InnerText = 0x2
        }

        [Flags]
        public enum SplitOption
        {
            None = 0x1,
            StartWord = 0x2,
            EndWord = 0x3,
            StartEndWord = 0x4
        }
        public enum HtmlTagRemoveFlag
        {
            FULL = 0,
            TARGET = 1
        }
        public static string ToRemoveUnescape(this string text)
        {
            string result = null;
            try
            {
                if (text != null) { result = Regex.Replace(text, @"\t|\n|\r", ""); }
            }
            catch (Exception exception) { exception.Show(); }
            return result;
        }
        public static string ToUniCodeDecode(this string text)
        {
            string result = null;
            try
            {
                if (text != null)
                {
                    //var regex = new Regex(@"\\u(?<Value>[a-zA-Z0-9]{4})", RegexOptions.Compiled);
                    //result = regex.Replace(text, x => ((char)int.Parse(x.Groups["Value"].Value, NumberStyles.HexNumber)).ToString());

                    result = Regex.Replace(text, @"\\u(?<Value>[a-zA-Z0-9]{4})", match => ((char)int.Parse(match.Groups["Value"].Value, NumberStyles.HexNumber)).ToString());
                }
            }
            catch (Exception exception) { exception.Show(); }
            return result;
        }
        public static string RemoveSpace(this string text)
        {
            string result = null;
            try
            {
                result = Regex.Replace(text, @"[<][a-z|A-Z|/](.|)*?[>]", "");
                string[] splited = result.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                result = string.Join(" ", splited);
                if (result.Contains("\r") == true) { result = result.Replace("\r", ""); }
                if (result.Contains("\n") == true) { result = result.Replace("\n", ""); }
                if (result.Contains("\t") == true) { result = result.Replace("\t", ""); }
                if (result.Contains("\u0000") == true) { result = result.Replace("\u0000", ""); }
                if (result.Contains("\0") == true) { result = result.Replace("\0", ""); }
                result = result.Trim();
            }
            catch (Exception exception) { exception.Show(); }
            return result;
        }
        public static Uri ToUri(this string text)
        {
            try
            {
                return new Uri(text);
            }
            catch (Exception exception) { exception.Show(); }
            return null;
        }
        public static List<string> ToDivision(this string text, string standard)
        {
            try
            {
                return text.Split(new string[] { standard }, StringSplitOptions.None).ToList();
            }
            catch (Exception exception) { exception.Show(); }
            return null;
        }
        public static string SingleSplit(this string text, string startWord = null, string endWord = null, SplitOption splitOption = SplitOption.None)
        {
            string text2 = null;
            try
            {
                if (text != null && startWord != null && text.Contains(startWord))
                {
                    text2 = text.Substring(text.IndexOf(startWord) + startWord.Length);
                }

                if (text != null && endWord != null && text2 != null)
                {
                    text2 = ((!text2.Contains(endWord)) ? null : text2.Substring(0, text2.IndexOf(endWord)));
                }

                switch (splitOption)
                {
                    case SplitOption.StartWord:
                        text2 = startWord + text2;
                        break;
                    case SplitOption.EndWord:
                        text2 += endWord;
                        break;
                    case SplitOption.StartEndWord:
                        text2 = startWord + text2 + endWord;
                        break;
                }
            }
            catch (Exception exception)
            {
                exception.Show();
            }

            return text2;
        }
        public static List<string> MultiSplit(this string text, string startWord, string endWord, SplitOption splitOption = SplitOption.None)
        {
            List<string> list = new List<string>();
            try
            {
                if (text != null)
                {
                    while (text.Contains(startWord))
                    {
                        text = text.Substring(text.IndexOf(startWord) + startWord.Length);
                        if (text.Contains(endWord))
                        {
                            string item = text.Substring(0, text.IndexOf(endWord));
                            list.Add(item);
                            continue;
                        }

                        break;
                    }

                    switch (splitOption)
                    {
                        case SplitOption.StartWord:
                            list = list.Select((string x) => startWord + x).ToList();
                            break;
                        case SplitOption.EndWord:
                            list = list.Select((string x) => x + endWord).ToList();
                            break;
                        case SplitOption.StartEndWord:
                            list = list.Select((string x) => startWord + x + endWord).ToList();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }

            return list;
        }
        public static string HtmlTagRemove(this string text, string tag, HtmlTagRemoveFlag flag)
        {
            switch (flag)
            {
                case HtmlTagRemoveFlag.FULL:
                    return Regex.Replace(text, $@"<{tag}[^>]*>[\s\S]*?[^<]*</{tag}>", string.Empty);
                case HtmlTagRemoveFlag.TARGET:
                    return Regex.Replace(text, $@"<{tag}[\s\S]*?>", string.Empty);
                default:
                    return text;
            }
        }
        public static string HtmlTagAttrReplace(this string text, string xpath, string value, string property = "")
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(text);
            var imgTags = doc.DocumentNode.SelectNodes(xpath);
            if (imgTags != null)
            {
                foreach (var img in imgTags)
                {
                    var srcAttribute = img.Attributes[property];
                    img.SetAttributeValue(property, value);
                    //img.Attributes[property].Value = value;
                }

                string modifiedHtml = doc.DocumentNode.OuterHtml;
                return modifiedHtml;
            }
            else return text;
        }
        public static List<HtmlNode> GetHtmlNodes(this string text, string xPath)
        {
            List<HtmlNode> result = new List<HtmlNode>();
            try
            {
                if (text != null && xPath != null)
                {
                    HtmlDocument htmlDocument = new HtmlDocument();
                    htmlDocument.LoadHtml(text);
                    HtmlNodeCollection htmlNodeCollection = htmlDocument.DocumentNode.SelectNodes(xPath);
                    if (htmlNodeCollection != null)
                    {
                        List<HtmlNode> list = htmlNodeCollection.Cast<HtmlNode>().ToList();
                        if (list != null && list.Count > 0)
                        {
                            result = list;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                exception.Show();
            }

            return result;
        }
        public static List<HtmlAttribute> GetHtmlAttributes(this string text, string xPath)
        {
            List<HtmlAttribute> result = new List<HtmlAttribute>();
            try
            {
                if (text != null && xPath != null)
                {
                    List<HtmlNode> htmlNodes = text.GetHtmlNodes(xPath);
                    if (htmlNodes != null && htmlNodes.Count > 0)
                    {
                        List<HtmlAttribute> tempHtmlAttributes = new List<HtmlAttribute>();
                        htmlNodes.ForEach(delegate (HtmlNode x)
                        {
                            List<HtmlAttribute> list = x.Attributes.ToList();
                            if (list != null && list.Count > 0)
                            {
                                tempHtmlAttributes.AddRange(list);
                            }
                        });
                        if (tempHtmlAttributes != null && tempHtmlAttributes.Count > 0)
                        {
                            result = tempHtmlAttributes;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                exception.Show();
            }

            return result;
        }
        public static string HtmlTagReplace(this string text, string xpath, string value, string property = "")
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(text);
            var imgTags = doc.DocumentNode.SelectNodes(xpath);
            if (imgTags != null)
            {
                foreach (var img in imgTags)
                {
                    var srcAttribute = img.Attributes[property];
                    if (srcAttribute != null && string.IsNullOrWhiteSpace(srcAttribute.Value) == false)
                    {
                        img.Attributes[property].Value = value;
                    }
                }

                string modifiedHtml = doc.DocumentNode.OuterHtml;
                return modifiedHtml;
            }
            else return text;
        }
        public static List<string> GetValues(this string text, string xPath, string htmlAttributeName)
        {
            List<string> result = new List<string>();
            try
            {
                if (text != null && xPath != null && htmlAttributeName != null)
                {
                    List<HtmlAttribute> htmlAttributes = text.GetHtmlAttributes(xPath);
                    if (htmlAttributes != null && htmlAttributes.Count > 0)
                    {
                        List<string> values = new List<string>();
                        htmlAttributes = htmlAttributes.FindAll((HtmlAttribute x) => x.Name == htmlAttributeName);
                        if (htmlAttributes != null && htmlAttributes.Count > 0)
                        {
                            htmlAttributes.ForEach(delegate (HtmlAttribute x)
                            {
                                values.Add(x.Value);
                            });
                        }

                        if (values != null && values.Count > 0)
                        {
                            result = values;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                exception.Show();
            }

            return result;
        }
        public static List<string> GetValues(this string text, string xPath, XPathOption xPathOption)
        {
            List<string> result = new List<string>();
            try
            {
                if (text != null && xPath != null)
                {
                    List<HtmlNode> htmlNodes = text.GetHtmlNodes(xPath);
                    if (htmlNodes != null && htmlNodes.Count > 0)
                    {
                        List<string> values = new List<string>();
                        htmlNodes.ForEach(delegate (HtmlNode x)
                        {
                            switch (xPathOption)
                            {
                                case XPathOption.InnerHtml:
                                    values.Add(x.InnerHtml);
                                    break;
                                case XPathOption.OuterHtml:
                                    values.Add(x.OuterHtml);
                                    break;
                                case XPathOption.InnerText:
                                    values.Add(x.InnerText);
                                    break;
                            }
                        });
                        if (values != null && values.Count > 0)
                        {
                            result = values;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                exception.Show();
            }

            return result;
        }
        public static string GetValue(this string text, string xPath, string htmlAttributeName)
        {
            string result = null;
            try
            {
                List<string> values = text.GetValues(xPath, htmlAttributeName);
                if (values != null && values.Count > 0)
                {
                    result = values[0];
                }
            }
            catch (Exception exception)
            {
                exception.Show();
            }

            return result;
        }
        public static string GetValue(this string text, string xPath, XPathOption xPathOption = XPathOption.InnerHtml)
        {
            string result = null;
            try
            {
                List<string> values = text.GetValues(xPath, xPathOption);
                if (values != null && values.Count > 0)
                {
                    result = values[0];
                }
            }
            catch (Exception exception)
            {
                exception.Show();
            }

            return result;
        }
        public static string SetTextResize(this string text, int length)
        {
            string result = text;
            int textLength = text.Length;
            if (textLength > length) result = text.Substring(0, length);

            return result;
        }
        public static string RemoveSpecialCharacters(this string text)
        {
            if (text != null && text != "")
            {
                text = Regex.Replace(text, "\\t|\\n|\\r", "");
                text = Regex.Replace(text, "[^a-zA-Z0-9가-힣_ぁ-ゔァ-ヴー々〆〤一-龥]", "", RegexOptions.Singleline);
            }

            if (text.Contains(","))
            {
                text = text.Replace(",", " ");
            }

            if (text.Contains(","))
            {
                text = text.Replace(",", " ");
            }

            if (text.Contains("!"))
            {
                text = text.Replace("!", "");
            }

            if (text.Contains("\\"))
            {
                text = text.Replace("\\", " ");
            }

            if (text.Contains("#"))
            {
                text = text.Replace("#", "");
            }

            if (text.Contains("%"))
            {
                text = text.Replace("%", "");
            }

            if (text.Contains("^"))
            {
                text = text.Replace("^", "");
            }

            if (text.Contains("&"))
            {
                text = text.Replace("&", "");
            }

            if (text.Contains("*"))
            {
                text = text.Replace("*", "");
            }

            if (text.Contains("`"))
            {
                text = text.Replace("`", " ");
            }

            if (text.Contains("~"))
            {
                text = text.Replace("~", "");
            }

            if (text.Contains("-"))
            {
                text = text.Replace("-", " ");
            }

            if (text.Contains("_"))
            {
                text = text.Replace("_", " ");
            }

            if (text.Contains("|"))
            {
                text = text.Replace("|", "");
            }

            if (text.Contains("?"))
            {
                text = text.Replace("?", "");
            }

            if (text.Contains("-"))
            {
                text = text.Replace("-", " ");
            }

            if (text.Contains("+"))
            {
                text = text.Replace("+", "");
            }

            if (text.Contains(":"))
            {
                text = text.Replace(":", "");
            }

            if (text.Contains(";"))
            {
                text = text.Replace(";", "");
            }

            if (text.Contains("\""))
            {
                text = text.Replace("\"", "");
            }

            if (text.Contains("<"))
            {
                text = text.Replace("<", "");
            }

            if (text.Contains(">"))
            {
                text = text.Replace(">", "");
            }

            if (text.Contains("&amp;"))
            {
                text = text.Replace("&amp;", "");
            }

            return text;
        }
        public enum LanguageType
        {
            UNKNOWN = 0,
            KOR = 1,
            ENG = 2,
            CHA = 3,
            JPN = 4,
            GER = 5
        }
        public static LanguageType GetLanguageInfo(this string text)
        {
            int koreanCount = text.Count(c => IsKorean(c));
            int chineseCount = text.Count(c => IsChinese(c));
            int englishCount = text.Count(c => IsEnglish(c));
            int japaneseCount = text.Count(c => IsJapanese(c));
            int germanCount = text.Count(c => IsGerman(c));
            if (koreanCount >= chineseCount && koreanCount >= englishCount && koreanCount >= japaneseCount && koreanCount >= germanCount)
            {
                return LanguageType.KOR;
            }
            else if (chineseCount >= koreanCount && chineseCount >= englishCount && chineseCount >= japaneseCount && chineseCount >= germanCount)
            {
                return LanguageType.CHA;
            }
            else if (englishCount >= koreanCount && englishCount >= chineseCount && englishCount >= japaneseCount && englishCount >= germanCount)
            {
                return LanguageType.ENG;
            }
            else if (japaneseCount >= koreanCount && japaneseCount >= chineseCount && japaneseCount >= englishCount && japaneseCount >= germanCount)
            {
                return LanguageType.JPN;
            }
            else if (germanCount >= koreanCount && germanCount >= chineseCount && germanCount >= englishCount && germanCount >= japaneseCount)
            {
                return LanguageType.GER;
            }
            else
            {
                return LanguageType.UNKNOWN;
            }
        }
        private static bool IsKorean(char c)
        {
            // 한글 자모 (Hangul Jamo): U+1100 ~ U+11FF
            // 한글 호환 자모 (Hangul Compatibility Jamo): U+3130 ~ U+318F
            // 한글 음절 (Hangul Syllables): U+AC00 ~ U+D7AF
            return (c >= '\u1100' && c <= '\u11FF') || (c >= '\u3130' && c <= '\u318F') || (c >= '\uAC00' && c <= '\uD7AF');
        }
        private static bool IsChinese(char c)
        {
            // CJK 통합 한자 (CJK Unified Ideographs): U+4E00 ~ U+9FFF
            // CJK 통합 한자 보충 (CJK Unified Ideographs Extension): U+3400 ~ U+4DBF
            return (c >= '\u4E00' && c <= '\u9FFF') || (c >= '\u3400' && c <= '\u4DBF');
        }
        private static bool IsEnglish(char c)
        {
            // 기본 라틴 문자 (Basic Latin): U+0000 ~ U+007F
            return (c >= '\u0000' && c <= '\u007F' && c != '\u0020' && (c < '\u0030' && c > '\u0039'));
        }
        private static bool IsJapanese(char c)
        {
            // 히라가나 (Hiragana): U+3040 ~ U+309F
            // 가타카나 (Katakana): U+30A0 ~ U+30FF
            // 한자 (CJK Unified Ideographs): U+4E00 ~ U+9FFF (중국어와 겹침)
            return (c >= '\u3040' && c <= '\u309F') || (c >= '\u30A0' && c <= '\u30FF') || IsChinese(c);
        }
        private static bool IsGerman(char c)
        {
            // 독일어는 라틴 문자에 속함 (기본 라틴 문자 + 확장된 라틴 문자)
            // 기본 라틴 문자 (Basic Latin): U+0000 ~ U+007F
            // 라틴-1 보충 (Latin-1 Supplement): U+0080 ~ U+00FF
            return (c >= '\u0000' && c <= '\u007F' && c != '\u0020' && (c < '\u0030' && c > '\u0039')) || (c >= '\u0080' && c <= '\u00FF');
        }
        public static LanguageType ConvertLanguageType(this string from)
        {
            switch (from)
            {
                case "ko":
                    return LanguageType.KOR;
                case "en":
                    return LanguageType.ENG;
                case "zh-CN":
                    return LanguageType.CHA;
                case "ja":
                    return LanguageType.JPN;
                case "de":
                    return LanguageType.GER;
                default: return LanguageType.UNKNOWN;
            }
        }
        [Obsolete("사이트별 이름 매핑 사용 권장")]
        public static string ConvertMarketNameToKor(this string text)
        {
            switch (text)
            {
                case "SmartStore": return "스마트스토어";
                case "SmartStore_API": return "스마트스토어";
                case "SmartStoreAPI": return "스마트스토어";
                case "Coupang": return "쿠팡";
                case "Auction": return "옥션";
                case "Auction_API": return "옥션";
                case "Gmarket": return "지마켓";
                case "Gmarket_API": return "지마켓";
                case "Elevenst": return "11번가";
                case "Interpark": return "인터파크";
                case "Interpark_API": return "인터파크";
                case "WeMakePrice": return "위메프";
                case "Tmon": return "티몬";
                case "LotteOn": return "롯데온";
                case "LotteOn_API": return "롯데온";
                case "LotteOnAPI": return "롯데온";
                case "Qoo10": return "큐텐";
                default: return text;
            }
        }
        [Obsolete("사이트별 이름 매핑 사용 권장")]
        public static string ConvertSourcingNameToKor(this string text)
        {
            switch (text)
            {
                case "Ali-Express": return "알리 익스프레스";
                case "AliExpress-KR": return "알리 익스프레스";
                case "1688": return "1688";
                case "Taobao": return "타오바오";
                case "Amazon-US": return "아마존 US";
                case "Amazon-JP": return "아마존 JP";
                case "Amazon-DE": return "아마존 DE";
                case "Amazon-AU": return "아마존 AU";
                case "Amazon-FR": return "아마존 FR";
                case "Breuninger": return "브로이닝어";
                case "Domeme": return "도매매";
                case "Rakuten": return "라쿠텐";
                case "ShoppingNaver": return "네이버쇼핑";
                case "ShoppingNaver-Logistics": return "네이버n배송";
                case "ShoppingNaver-Directfarm": return "네이버산지직송";
                case "Mytheresa": return "마이테레사";
                case "Farfetch": return "파페치";
                case "Walmart": return "월마트";
                case "Ebay": return "이베이";
                case "VVIC": return "VVIC";
                case "Oliveyoung": return "올리브영";
                case "PocaMarket": return "포카마켓";
                case "LotteOnCol": return "롯데온";
                case "Gmarket": return "지마켓스타배송";
                case "SSGCol": return "신세계";
                case "FashionPlus": return "패션플러스";
                case "IB": return "이태리부티크명품";
                case "JPF": return "일본식품";
                case "OU": return "어그(해외)";
                case "CKG": return "캘빈클라인골프";
                case "SLT": return "건강기능식품";
                case "TW": return "테이블웨어(식기)";
                case "DG": return "국내재고명품";
                case "DU": return "어그(국내)";
                case "WATCH": return "명품시계/주얼리";
                case "SmartStoreGoods": return "스마트스토어가져오기";
                case "CoupangGoods": return "쿠팡가져오기";
                case "ShopSimon": return "샵사이먼";
                case "Musinsa": return "무신사";
                default: return text;
            }
        }
        public static string ToSplit(this string text, string standard, int index)
        {
            try
            {
                string result = text.Split(new string[] { standard }, StringSplitOptions.None).ToList()[index];
                return result;
            }
            catch (Exception ex) { }
            return null;
        }
        public static string SHA1HashCrypt(this string val)
        {
            using (SHA1 sha1 = SHA1.Create())
            {
                byte[] hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(val));

                StringBuilder stringBuilder = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    stringBuilder.Append(b.ToString("x2"));
                }

                string result = stringBuilder.ToString();

                return result;
            }
        }
        public static List<string> GetExistWords(this List<string> texts, List<string> targets)
        {
            List<string> existWords = texts.Intersect(targets, StringComparer.OrdinalIgnoreCase).ToList();

            return existWords.IsNotNullGeneric() ? existWords : null;
        }
        public static List<string> GetExistWords(this string text, List<string> targets)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            if (targets == null) return null;

            List<string> existWords = new List<string>();

            foreach (string target in targets)
            {
                string target_desc = Regex.Replace(target, @"[^a-zA-Z0-9가-힣\u3040-\u309F\u30A0-\u30FF\u3400-\u4DBF\u4E00-\u9FFF_(){}\[\];,=!<>&|+\-*/%:.?x×]", string.Empty, RegexOptions.Singleline);

                if (target_desc.IsNullOrWhiteSpace()) continue;

                var escapes = new List<char> { '.', '+', '*', '[', ']', '(', ')' };
                for (int i = 0; i < escapes.Count; i++)
                {
                    if (target_desc.Contains(escapes[i]))
                    {
                        target_desc = Regex.Escape(target_desc);
                        break;
                    }
                }

                MatchCollection matches = Regex.Matches(text, target_desc, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    existWords.Add(match.Value);
                }
            }

            return existWords.Count > 0 ? existWords : null;
        }
        public static string RemoveWords(this string text, List<string> targets)
        {
            foreach (string item in targets)
            {
                try
                {
                    var target = @"" + item;
                    if (target == ".") target = "\\.";
                    text = Regex.Replace(text, Regex.Escape(target), "", RegexOptions.IgnoreCase);
                }
                catch (Exception ex)
                {
                    text = text.Replace(item, "");
                }
            }

            return string.IsNullOrWhiteSpace(text) ? "" : text.Replace("  ", " ");
        }
        public static string ReplaceWords(this string text, List<KeyValuePair<string, string>> targets)
        {

            foreach (var item in targets)
            {
                try
                {
                    string target = item.Key;
                    string value = item.Value;
                    text = Regex.Replace(text, Regex.Escape(target), value, RegexOptions.IgnoreCase);
                }
                catch (Exception err)
                {
                }
            }


            return string.IsNullOrWhiteSpace(text) ? "" : text.Replace("  ", " ");
        }
        public static string RemoveWordsInnerText(this string text, List<string> targets)
        {
            var textList = text.MultiSplit(">", "<");
            textList.RemoveAll(x => string.IsNullOrWhiteSpace(x));

            foreach (var innerText in textList)
            {
                var transText = innerText + "";
                foreach (string item in targets)
                {
                    var target = @"" + item;
                    if (target == ".") target = "\\.";
                    transText = Regex.Replace(Regex.Escape(innerText), Regex.Escape(target), "", RegexOptions.IgnoreCase);
                }
                text = text.Replace($">{innerText}<", $">{transText}<");
            }
            return string.IsNullOrWhiteSpace(text) ? "" : text.Replace("  ", " ");
        }
        public static string ReplaceWordsInnerText(this string text, List<KeyValuePair<string, string>> targets)
        {
            var textList = text.MultiSplit(">", "<");
            textList.RemoveAll(x => string.IsNullOrWhiteSpace(x));

            foreach (var innerText in textList)
            {
                var transText = innerText + "";
                foreach (var item in targets)
                {
                    string target = item.Key;
                    string value = item.Value;
                    if (target == ".") target = "\\.";
                    transText = Regex.Replace(Regex.Escape(innerText), Regex.Escape(innerText), Regex.Escape(value), RegexOptions.IgnoreCase);
                }
                text = text.Replace($">{innerText}<", $">{transText}<");
            }

            return string.IsNullOrWhiteSpace(text) ? "" : text.Replace("  ", " ");
        }
        public static bool IsNullOrWhiteSpace(this string text)
        {
            return string.IsNullOrWhiteSpace(text);
        }
        public static string ComputeMD5(this string text)
        {
            using (MD5 md5 = MD5.Create())
            {
                return BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(text)))
                            .Replace("-", "");
            }
        }
        public static string ToQueryString(this NameValueCollection parameters)
        {
            if (parameters == null || parameters.Count == 0)
                return "";
            var queryString = HttpUtility.ParseQueryString("");
            queryString.Add(parameters);

            return queryString.ToString();
        }
        public static string NumberToAlphabet(this int number)
        {
            string result = "";

            while (number > 0)
            {
                int remainder = (number - 1) % 26;
                char digit = (char)('A' + remainder);
                result = digit + result;

                number = (number - 1) / 26; // 몫을 구합니다.
            }
            return result;
        }
        public static string NumberToAlphabet(this long number)
        {
            string result = "";

            while (number > 0)
            {
                long remainder = (number - 1) % 26;
                char digit = (char)('A' + remainder);
                result = digit + result;

                number = (number - 1) / 26; // 몫을 구합니다.
            }
            return result;
        }
        public static List<int> FindBinaryPatterns(int position)
        {
            List<int> result = new List<int>();

            for (int i = 0; i < 256; i++) // Iterate through all 4-bit numbers (0 to 15)
            {
                if (((i >> position) & 1) == 1) // Check if the desired bit is 1
                {
                    result.Add(i);
                }
            }

            return result;
        }
        public static string GetParamValue(this string url, string key)
        {
            var urlArr = url.Split('?');
            if (urlArr.Length == 2)
            {
                var param = urlArr[1];
                var paramArr = param.Split('&');
                foreach (var data in paramArr)
                {
                    if (data.Split('=')[0] == key)
                    {
                        return data.Split('=')[1];
                        //return HttpUtility.UrlEncode(data.Split('=')[1], Encoding.UTF8);
                    }
                }
            }
            return "";
        }
        public static string GetRealNumber(this string number)
        {
            string tempValue = "0";
            try
            {
                Regex pattern = new Regex(@"[0-9]+(\.|\,)[0-9]+");
                Match match = pattern.Match(number);

                tempValue = match.Value;
            }
            catch (Exception exception)
            {

            }
            return tempValue;
        }
        private static byte[] ToBytes(string byteData, string encoding = "gbk")
        {
            System.Text.ASCIIEncoding asencoding = new System.Text.ASCIIEncoding();
            return Encoding.GetEncoding(encoding).GetBytes(byteData);
        }
        public static string ToHex(this string strData, string encoding = "gbk")
        {
            string resultHex = "";
            byte[] arr_byteStr = ToBytes(strData, encoding);

            foreach (byte byteStr in arr_byteStr)
            {
                resultHex += string.Format("{0:x2}", byteStr);
            }
            return resultHex;
        }

        public static string GetMainPage(this string site)
        {
            switch (site)
            {
                case "AliExpress-KR":
                    return "https://www.aliexpress.com/";
                case "Taobao":
                    return "https://taobao.com/";
                case "1688":
                    return "https://www.1688.com/";
                case "Amazon-US":
                    return "https://amazon.com/";
                case "Amazon-JP":
                    return "https://amazon.co.jp/";
                case "Amazon-DE":
                    return "https://www.amazon.de/";
                case "Amazon-AU":
                    return "https://www.amazon.com.au/";
                case "Amazon-FR":
                    return "https://www.amazon.fr/";
                case "Rakuten":
                    return "https://www.rakuten.co.jp/";
                default:
                    return "";
            }
        }
        public static string GetCountryCode(this string site)
        {
            switch (site)
            {
                case "AliExpress-KR":
                    return "CN";
                case "Taobao":
                    return "CN";
                case "1688":
                    return "CN";
                case "Amazon-US":
                    return "US";
                case "Amazon-JP":
                    return "JP";
                case "Amazon-DE":
                    return "DE";
                case "Amazon-AU":
                    return "AU";
                case "Amazon-FR":
                    return "FR";
                case "Rakuten":
                    return "JP";
                default:
                    return "";
            }
        }
        public static string GetCountryName(this string site)
        {
            switch (site)
            {
                case "AliExpress-KR":
                    return "중국";
                case "Taobao":
                    return "중국";
                case "1688":
                    return "중국";
                case "Amazon-US":
                    return "미국";
                case "Amazon-JP":
                    return "일본";
                case "Amazon-DE":
                    return "독일";
                case "Amazon-AU":
                    return "호주";
                case "Amazon-FR":
                    return "프랑스";
                case "Rakuten":
                    return "일본";
                default:
                    return "";
            }
        }
        public static string ReplaceWordsCaseSensitive(this string text, List<string> targets, List<string> values, List<int> isCaseSensitiveFlags)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                try
                {
                    string pattern = Regex.Escape(targets[i]);
                    string replacement = values[i];
                    var options = isCaseSensitiveFlags[i] == 1
                        ? RegexOptions.None
                        : RegexOptions.IgnoreCase;

                    text = Regex.Replace(text, pattern, replacement, options);
                }
                catch (Exception)
                {
                }
            }

            return string.IsNullOrWhiteSpace(text) ? "" : text.Replace("  ", " ");
        }
    }
}
