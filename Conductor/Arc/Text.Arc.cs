// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.WebSockets;
using System.Windows.Documents;
using System.Xml.Linq;

#pragma warning disable SA1201 // Elements should appear in the correct order
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1611 // Element parameters should be documented
#pragma warning disable SA1615 // Element return value should be documented
#pragma warning disable SA1629 // Documentation text should end with a period
#pragma warning disable SA1649 // File name should match first type name

namespace Arc.Text
{
    public class TimeHMS
    { // time format, hour/minute/second
        public bool Circular { get; set; } = false;

        public bool IsValid => this.hour >= 0 && this.minute >= 0 && this.second >= 0;

        private int hour;

        public int Hour
        {
            get
            {
                return this.hour;
            }

            set
            {
                if (value < 0)
                {
                    this.hour = -1;
                }
                else if (value >= 24 && this.Circular)
                {
                    this.hour = -1;
                }
                else
                {
                    this.hour = value;
                }
            }
        }

        private int minute;

        public int Minute
        {
            get
            {
                return this.minute;
            }

            set
            {
                if (value >= 0 && value < 60)
                {
                    this.minute = value;
                }
                else
                {
                    this.minute = -1;
                }
            }
        }

        private int second;

        public int Second
        {
            get
            {
                return this.second;
            }

            set
            {
                if (value >= 0 && value < 60)
                {
                    this.second = value;
                }
                else
                {
                    this.second = -1;
                }
            }
        }

        public bool IncrementHour()
        {
            if (this.hour >= 23 && this.Circular)
            {
                this.hour %= 24;
                return true;
            }
            else if (this.hour >= 99)
            {
                return false;
            }
            else if (this.hour >= 0)
            {
                this.hour++;
                return true;
            }

            return false;
        }

        public bool DecrementHour()
        {
            if (this.hour == 0)
            {
                if (this.Circular)
                {
                    this.hour = 23;
                    return true;
                }
            }
            else if (this.hour > 0)
            {
                this.hour--;
                return true;
            }

            return false;
        }

        public bool IncrementMinute()
        {
            if (this.minute == 59)
            {
                if (this.IncrementHour())
                {
                    this.minute = 0;
                    return true;
                }
            }
            else if (this.minute >= 0)
            {
                this.minute++;
                return true;
            }

            return false;
        }

        public bool DecrementMinute()
        {
            if (this.minute == 0)
            {
                if (this.DecrementHour())
                {
                    this.minute = 59;
                    return true;
                }
            }
            else if (this.minute > 0)
            {
                this.minute--;
                return true;
            }

            return false;
        }

        public bool IncrementSecond()
        {
            if (this.second == 59)
            {
                if (this.IncrementMinute())
                {
                    this.second = 0;
                    return true;
                }
            }
            else if (this.second >= 0)
            {
                this.second++;
                return true;
            }

            return false;
        }

        public bool DecrementSecond()
        {
            if (this.second == 0)
            {
                if (this.DecrementMinute())
                {
                    this.second = 59;
                    return true;
                }
            }
            else if (this.second > 0)
            {
                this.second--;
                return true;
            }

            return false;
        }

        public TimeHMS()
        {
        }

        public TimeHMS(int hour, int minute, int second)
        {
            this.SetTime(hour, minute, second);
        }

        public TimeHMS(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException();
            }

            int position = 0;
            this.Hour = Methods.TimeStringToInt(text, ref position);
            this.Minute = Methods.TimeStringToInt(text, ref position);
            this.Second = Methods.TimeStringToInt(text, ref position);
        }

        public void SetTime(int hour, int minute, int second)
        {
            this.Hour = hour;
            this.Minute = minute;
            this.Second = second;
        }

        public override string ToString()
        {
            return Methods.IntToTimeString(this.hour, this.minute, this.second);
        }
    }

    public class C4
    {
        public static C4 Instance { get; } = new C4();

        public uint MaxSize => 16 * 1024 * 1024; // max data size, 16MB

        private object cs; // critical section
        private uint maxName = 256; // max name length
        private uint maxString = 16 * 1024; // max string length

        // data
        private Dictionary<string, string>? currentCultureData; // Current culture data. Dictionary<name, string>
        private Dictionary<string, string>? defaultCultureData; // Default culture data. Dictionary<name, string>
        private Dictionary<string, Dictionary<string, string>> cultures; // Culture and data. Dictionary<culture "ja", data>
        private string errorText = "C4 error"; // Error message (when no text found).
        private string? currentCulture; // Current culture

        public CultureInfo? CurrentCulture { get; private set; } // 現在のカルチャー

        public C4()
        {
            this.cs = new object(); // critical section

            this.currentCultureData = null;
            this.defaultCultureData = null;
            this.cultures = new Dictionary<string, Dictionary<string, string>>();
        }

        /// <summary>
        /// nameの文字列を取得する。取得できなかったら、nameを返す。
        /// </summary>
        /// <param name="name">文字列の名称</param>
        /// <returns>string.</returns>
        public string this[string? name]
        {
            get
            {
                string? text = null;
                if (name == null)
                {
                    return this.errorText;
                }

                lock (this.cs)
                {
                    this.currentCultureData?.TryGetValue(name, out text); // current culture
                    if (text != null)
                    {
                        return text;
                    }

                    this.defaultCultureData?.TryGetValue(name, out text); // default culture
                    if (text != null)
                    {
                        return text;
                    }

                    return name; // this.errorText;
                }
            }

            set
            {
                if (name == null)
                {
                    return;
                }

                lock (this.cs)
                {
                    if (this.currentCultureData != null)
                    {
                        this.currentCultureData[name] = value;
                    }
                    else if (this.defaultCultureData != null)
                    {
                        this.defaultCultureData[name] = value;
                    }
                }
            }
        }

        /// <summary>
        /// nameの文字列を取得する。取得できなかったら、nullを返す。
        /// </summary>
        /// <param name="name">文字列の名称</param>
        /// <returns>string</returns>
        public string? Get(string name)
        {
            string? text = null;
            if (name == null)
            {
                return null;
            }

            lock (this.cs)
            {
                this.currentCultureData?.TryGetValue(name, out text); // current culture
                if (text != null)
                {
                    return text;
                }

                this.defaultCultureData?.TryGetValue(name, out text); // default culture
                if (text != null)
                {
                    return text;
                }

                return null;
            }
        }

        public void Load(string culture, string filename, bool clearFlag = false)
        { // clearFlag 1:clear data
            // Exception: ArgumentException, FileNotFoundException, OverflowException
            // Exception: InvalidDataException
            if (filename == null)
            {
                throw new ArgumentException();
            }

            try
            {
                using (var fs = File.OpenRead(filename))
                {
                    lock (this.cs)
                    {
                        this.Load(culture, fs, clearFlag);
                    }
                }
            }
            catch (FileNotFoundException e)
            {
                throw e;
            }
        }

        public void LoadStream(string culture, Stream stream, bool clearFlag = false)
        { // flag 1:clear data
            // Exception: ArgumentException, FileNotFoundException, OverflowException
            // Exception: InvalidDataException
            if (stream == null)
            {
                throw new ArgumentException();
            }

            lock (this.cs)
            {
                this.Load(culture, stream, clearFlag);
            }
        }

#if !NETFX_CORE
        public void LoadAssembly(string culture, string assemblyname, bool clearFlag = false)
        { // flag 1:clear data
          // Exception: ArgumentException, FileNotFoundException
          // Exception: InvalidDataException
            if (assemblyname == null)
            {
                throw new ArgumentException();
            }

            var asm = System.Reflection.Assembly.GetExecutingAssembly();

            // var list = asm.GetManifestResourceNames();
            using (var stream = asm.GetManifestResourceStream(asm.GetName().Name + "." + assemblyname))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException();
                }

                lock (this.cs)
                {
                    this.Load(culture, stream, clearFlag);
                }
            }
        }
#endif

        public void SetDefaultCulture(string default_culture)
        {// set default culture. cultureが見つからない場合は、KeyNotFoundException
            if (default_culture == null)
            {
                throw new ArgumentException();
            }

            lock (this.cs)
            {
                this.defaultCultureData = this.cultures[default_culture];
                if (this.currentCultureData == null)
                {
                    this.currentCultureData = this.defaultCultureData;
                }
            }
        }

        public void SetCulture(string culture)
        { // set current culture. cultureが見つからない場合は、KeyNotFoundException
            if (culture == null)
            {
                throw new ArgumentException();
            }

            // CultureInfo
            string ci;
            switch (culture)
            {
                case "ja":
                    ci = "ja-JP";
                    break;
                case "en":
                    ci = "en-US";
                    break;
                default:
                    ci = culture;
                    break;
            }

            this.CurrentCulture = new CultureInfo(ci);

            lock (this.cs)
            {
                try
                {
                    this.currentCultureData = this.cultures[culture];
                    this.currentCulture = culture;
                }
                catch (KeyNotFoundException e)
                {
                    throw e;
                }
            }
        }

        /// <summary>
        /// Get current culture.
        /// </summary>
        /// <param name="culture">culture</param>
        public void GetCulture(out string? culture)
        {
            lock (this.cs)
            {
                culture = this.currentCulture;
            }
        }

        public void SetErrorText(string text)
        { // set error text.  データが見つからないときのメッセージを設定する。
            if (text == null)
            {
                throw new ArgumentException();
            }

            lock (this.cs)
            {
                this.errorText = text;
            }
        }

        private void Load(string culture, Stream stream, bool clearFlag)
        { // load xml
            if (stream.Length > this.MaxSize)
            {
                throw new OverflowException();
            }

            if (culture.Length > this.maxName)
            {
                throw new OverflowException();
            }

            Dictionary<string, string>? data;
            this.cultures.TryGetValue(culture, out data); // get culture data
            if (data == null)
            {
                data = new Dictionary<string, string>();
                this.cultures[culture] = data;
            }
            else
            {
                if (clearFlag)
                { // clear
                    data.Clear();
                }
            }

            // var doc = new XmlDocument();
            try
            {
                // doc.Load(stream);
                var root = XElement.Load(stream);
                foreach (var x in root.Elements("string"))
                {
                    this.LoadString2(data, x);
                }
            }
            catch
            {
                throw new InvalidDataException();
            }

            return;
        }

        private void LoadString2(Dictionary<string, string> data, XElement element)
        {
            // get text
            /*foreach(var node in element.Nodes())
            {
                if ((node.NodeType == XmlNodeType.Text) || (node.NodeType == XmlNodeType.CDATA))
                {
                    text = node.Value;
                    goto _load_string_get;
                }
            }*/
            var text = element.Value;

            // get name
            var name = element.Attribute("name")?.Value;
            if (name == null)
            {// no name
                return;
            }

            // check text length
            if (text.Length > this.maxString)
            {
                return;
            }

            // set
            data[name] = text;
        }
    }

    public static class Kana
    {
        /// <summary>
        /// 「全角」英数字を「半角」、「半角カタカナ」を「全角カタカナ」に変換する。
        /// </summary>
        /// <param name="str">string</param>
        /// <returns>return</returns>
        public static string FormatKana(string str)
        {
            char[] cs = str.ToCharArray();
            int f = cs.Length;

            for (int i = 0; i < f; i++)
            {
                char c = cs[i];

                // ！(0xFF01) ～ ～(0xFF5E)
                if (c >= '！' && c <= '～')
                {
                    cs[i] = (char)(c - 0xFEE0);
                }

                // 全角スペース(0x3000) -> 半角スペース(0x0020)
                else if (c == '　')
                {
                    cs[i] = ' ';
                }

                // ｦ(0xFF66) ～ ﾟ(0xFF9F)
                else if (c >= 'ｦ' && c <= 'ﾟ')
                {
                    char m = ConvertToZenkakuKanaChar(c);
                    if (m != '\0')
                    {
                        cs[i] = m;
                    }
                }
            }

            return new string(cs);
        }

        /// <summary>
        /// 全角英数字および記号を半角に変換します。
        /// </summary>
        public static string ToHankaku(string str)
        {
            char[] cs = str.ToCharArray();
            int f = cs.Length;

            for (int i = 0; i < f; i++)
            {
                char c = cs[i];

                // ！(0xFF01) ～ ～(0xFF5E)
                if (c >= '！' && c <= '～')
                {
                    cs[i] = (char)(c - 0xFEE0);
                }

                // 全角スペース(0x3000) -> 半角スペース(0x0020)
                else if (c == '　')
                {
                    cs[i] = ' ';
                }
            }

            return new string(cs);
        }

        /// <summary>
        /// 半角カタカナを全角カタカナに変換します。
        /// </summary>
        public static string ToZenkakuKana(string str)
        {
            char[] cs = str.ToCharArray();
            int f = str.Length;

            for (int i = 0; i < f; i++)
            {
                char c = cs[i];

                // ｦ(0xFF66) ～ ﾟ(0xFF9F)
                if (c >= 'ｦ' && c <= 'ﾟ')
                {
                    char m = ConvertToZenkakuKanaChar(c);
                    if (m != '\0')
                    {
                        cs[i] = m;
                    }
                }
            }

            return new string(cs);
        }

        private static char ConvertToZenkakuKanaChar(char hankakuChar)
        {
            switch (hankakuChar)
            {
                case 'ｦ':
                    return 'ヲ';
                case 'ｧ':
                    return 'ァ';
                case 'ｨ':
                    return 'ィ';
                case 'ｩ':
                    return 'ゥ';
                case 'ｪ':
                    return 'ェ';
                case 'ｫ':
                    return 'ォ';
                case 'ｰ':
                    return 'ー';
                case 'ｱ':
                    return 'ア';
                case 'ｲ':
                    return 'イ';
                case 'ｳ':
                    return 'ウ';
                case 'ｴ':
                    return 'エ';
                case 'ｵ':
                    return 'オ';
                case 'ｶ':
                    return 'カ';
                case 'ｷ':
                    return 'キ';
                case 'ｸ':
                    return 'ク';
                case 'ｹ':
                    return 'ケ';
                case 'ｺ':
                    return 'コ';
                case 'ｻ':
                    return 'サ';
                case 'ｼ':
                    return 'シ';
                case 'ｽ':
                    return 'ス';
                case 'ｾ':
                    return 'セ';
                case 'ｿ':
                    return 'ソ';
                case 'ﾀ':
                    return 'タ';
                case 'ﾁ':
                    return 'チ';
                case 'ﾂ':
                    return 'ツ';
                case 'ﾃ':
                    return 'テ';
                case 'ﾄ':
                    return 'ト';
                case 'ﾅ':
                    return 'ナ';
                case 'ﾆ':
                    return 'ニ';
                case 'ﾇ':
                    return 'ヌ';
                case 'ﾈ':
                    return 'ネ';
                case 'ﾉ':
                    return 'ノ';
                case 'ﾊ':
                    return 'ハ';
                case 'ﾋ':
                    return 'ヒ';
                case 'ﾌ':
                    return 'フ';
                case 'ﾍ':
                    return 'ヘ';
                case 'ﾎ':
                    return 'ホ';
                case 'ﾏ':
                    return 'マ';
                case 'ﾐ':
                    return 'ミ';
                case 'ﾑ':
                    return 'ム';
                case 'ﾒ':
                    return 'メ';
                case 'ﾓ':
                    return 'モ';
                case 'ﾔ':
                    return 'ヤ';
                case 'ﾕ':
                    return 'ユ';
                case 'ﾖ':
                    return 'ヨ';
                case 'ﾗ':
                    return 'ラ';
                case 'ﾘ':
                    return 'リ';
                case 'ﾙ':
                    return 'ル';
                case 'ﾚ':
                    return 'レ';
                case 'ﾛ':
                    return 'ロ';
                case 'ﾜ':
                    return 'ワ';
                case 'ﾝ':
                    return 'ン';
                case 'ﾞ':
                    return '゛';
                case 'ﾟ':
                    return '゜';
                case 'ｬ':
                    return 'ャ';
                case 'ｭ':
                    return 'ュ';
                case 'ｮ':
                    return 'ョ';
                case 'ｯ':
                    return 'ッ';

                default:
                    return '\0';
            }
        }
    }

    public static class Trans64
    {
        private static readonly char[] Padding = { '=' };

        /// <summary>
        /// バイト列を文字列 "a_B4x-a"に変換する。Base64 +を-に、/を_に、末尾の=を削除する。
        /// </summary>
        public static string ByteToString(byte[] b)
        {
            return System.Convert.ToBase64String(b).TrimEnd(Padding).Replace('+', '-').Replace('/', '_');
        }

        /// <summary>
        /// 文字列をバイト列に変換する。
        /// </summary>
        public static byte[] StringToByte(string str)
        {
            string incoming = str.Replace('_', '/').Replace('-', '+');
            switch (str.Length % 4)
            {
                case 2: incoming += "=="; break;
                case 3: incoming += "="; break;
            }

            return Convert.FromBase64String(incoming);
        }
    }

    /// <summary>
    /// Arc.Text Methods.
    /// </summary>
    public static partial class Methods
    {
        /// <summary>
        /// 両端の空白とダブルクォーテーション、最後の'\'と'/'を消去する.
        /// </summary>
        public static string TrimDoubleQuotesAndSpaceAndBackslash(string str)
        {
            char[] trimchar = { '\\', '/' };
            return str.Replace('"', ' ').Trim().TrimEnd(trimchar);
        }

        /// <summary>
        /// 両端の空白とダブルクォーテーションを消去する.
        /// </summary>
        public static string TrimDoubleQuotesAndSpace(string str)
        {
            return str.Replace('"', ' ').Trim();
        }

        /// <summary>
        /// 文字列中の空白・タブを全て削除する.
        /// </summary>
        public static string TrimWhiteSpace(string str)
        { // return new string(str.ToCharArray().Where(c => !Char.IsWhiteSpace(c)).ToArray());
            var len = str.Length;
            var src = str.ToCharArray();
            int dstIdx = 0;
            for (int i = 0; i < len; i++)
            {
                var ch = src[i];
                switch (ch)
                {
                    case '\u0020':
                    case '\u00A0':
                    case '\u1680':
                    case '\u2000':
                    case '\u2001':
                    case '\u2002':
                    case '\u2003':
                    case '\u2004':
                    case '\u2005':
                    case '\u2006':
                    case '\u2007':
                    case '\u2008':
                    case '\u2009':
                    case '\u200A':
                    case '\u202F':
                    case '\u205F':
                    case '\u3000':
                    case '\u2028':
                    case '\u2029':
                    case '\u0009':
                    case '\u000A':
                    case '\u000B':
                    case '\u000C':
                    case '\u000D':
                    case '\u0085':
                        continue;
                    default:
                        src[dstIdx++] = ch;
                        break;
                }
            }

            return new string(src, 0, dstIdx);
        }

        public static char GetHexValue(int i)
        { // int to char
            if (i < 10)
            {
                return (char)(i + '0'); // 0-9
            }
            else if (i < 16)
            {
                return (char)(i - 10 + 'A'); // A-F
            }
            else
            {
                return ' '; // space
            }
        }

        /// <summary>
        /// バイト配列から16進数の文字列を生成します。
        /// </summary>
        public static string ByteToHex(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException();
            }

            char[] c = new char[bytes.Length * 2];
            int c_index = 0;

            for (int n = 0; n < bytes.Length; n++)
            {
                byte x = bytes[n];
                c[c_index++] = GetHexValue(x >> 4);
                c[c_index++] = GetHexValue(x & 15);
            }

            return new string(c, 0, c_index);
        }

        private static byte[] hexToByte = new byte[128] // 16進数の文字からバイトに変換するテーブル 0-127
        {
#pragma warning disable SA1001 // Commas should be spaced correctly
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,
            0,1,2,3,4,5,6,7,8,9,255,255,255,255,255,255,
            255,10,11,12,13,14,15,255,255,255,255,255,255,255,255,255,
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,
            255,10,11,12,13,14,15,255,255,255,255,255,255,255,255,255,
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,
#pragma warning restore SA1001 // Commas should be spaced correctly
        };

        /// <summary>
        /// 16進数の文字列からバイト配列を生成します。
        /// </summary>
        public static byte[] HexToByte(string hex)
        {
            if (hex.Length % 2 == 1)
            {
                throw new InvalidDataException();
            }

            byte[] arr = new byte[hex.Length >> 1];

            for (int i = 0; i < hex.Length; i += 2)
            {
                byte x, y;
                if (hex[i] >= 128)
                {
                    throw new InvalidDataException();
                }

                x = hexToByte[hex[i]];
                if (x == 255)
                {
                    throw new InvalidDataException();
                }

                if (hex[i + 1] >= 128)
                {
                    throw new InvalidDataException();
                }

                y = hexToByte[hex[i + 1]];
                if (y == 255)
                {
                    throw new InvalidDataException();
                }

                arr[i >> 1] = (byte)((x << 4) + y);
            }

            return arr;
        }

        /// <summary>
        /// size(long)を単位付き文字列に変換する。
        /// 1.2 MB, 23.4 MB, 456 MB, 5.6GB.
        /// </summary>
        public static string SizeToString(long size)
        {
            long n, m;
            uint u;
            string[] units = { " B", " KB", " MB", " GB", " TB", " PB", " EB", " ZB", " YB" };

            u = 0;
            m = 0;
            while (size > 1000)
            {
                n = size / 1024;
                m = (size - (n * 1024)) / 100; // 小数点1桁
                size = n;
                u++; // increase unit
            }

            if (size < 100 && u > 0)
            {
                return size.ToString() + '.' + m.ToString() + units[u];
            }
            else
            {
                return size.ToString() + units[u];
            }
        }

        public static string UIntToString(uint u)
        {
            return ByteToString(BitConverter.GetBytes(u));
        }

        /// <summary>
        /// バイト列を文字列に変換する。"FFFFFFFF-FFFFFFFF-"
        /// </summary>
        public static string ByteToString(byte[] b)
        {
            int number, remaining, b_index, c_index;

            if (b == null)
            {
                throw new ArgumentNullException();
            }

            number = b.Length >> 2;
            remaining = b.Length & 3;

            char[] c = new char[(number * 9) + (remaining * 2)];
            byte x;

            b_index = 0;
            c_index = 0;
            while (number-- > 0)
            {
                x = b[b_index++];
                c[c_index++] = GetHexValue(x >> 4);
                c[c_index++] = GetHexValue(x & 15);
                x = b[b_index++];
                c[c_index++] = GetHexValue(x >> 4);
                c[c_index++] = GetHexValue(x & 15);
                x = b[b_index++];
                c[c_index++] = GetHexValue(x >> 4);
                c[c_index++] = GetHexValue(x & 15);
                x = b[b_index++];
                c[c_index++] = GetHexValue(x >> 4);
                c[c_index++] = GetHexValue(x & 15);
                c[c_index++] = '-';
            }

            if ((remaining == 0) && (c_index > 0))
            {
                c_index--; // remove '-'
            }

            while (remaining-- > 0)
            {
                x = b[b_index++];
                c[c_index++] = GetHexValue(x >> 4);
                c[c_index++] = GetHexValue(x & 15);
            }

            return new string(c, 0, c_index);
        }

        /// <summary>
        /// バイト列を文字列に変換する。spacerを指定できる。0の場合は間隔なし。"FF-FF-FF-FF"
        /// </summary>
        public static string ByteToString2(byte[] b, int offset, int count, char spacer)
        {
            if (b == null)
            {
                throw new ArgumentNullException();
            }

            if (offset < 0 || count < 0)
            {
                throw new ArgumentException();
            }

            if (b.Length < (offset + count))
            {
                throw new ArgumentException();
            }

            char[] c = new char[b.Length * 3];
            byte x;
            int c_index = 0;

            while (count-- > 0)
            {
                x = b[offset++];
                c[c_index++] = GetHexValue(x >> 4);
                c[c_index++] = GetHexValue(x & 15);
                if (spacer != 0)
                {
                    c[c_index++] = spacer;
                }
            }

            if (spacer != 0 && c_index > 0)
            {
                c_index--; // remove spacer
            }

            return new string(c, 0, c_index);
        }

        public static bool IsOnly_Digits(string text)
        { // 数字のみの文字列の場合、trueを返す。
            foreach (var x in text)
            {
                if (x >= '0' && x <= '9')
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        public static bool IsOnly_DigitsOrComma(string text)
        { // 数字かカンマ（.）のみの文字列の場合、trueを返す。
            foreach (var x in text)
            {
                if (x >= '0' && x <= '9')
                {
                    continue;
                }
                else if (x == '.')
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        public static bool IsOnly_DigitsOrColon(string text)
        { // 数字コロン（:）のみの文字列の場合、trueを返す。
            foreach (var x in text)
            {
                if (x >= '0' && x <= '9')
                {
                    continue;
                }
                else if (x == ':')
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        public static bool IsOnly_DigitsOrCommaOrColon(string text)
        { // 数字かカンマ（.）かコロン（:）のみの文字列の場合、trueを返す。
            foreach (var x in text)
            {
                if (x >= '0' && x <= '9')
                {
                    continue;
                }
                else if (x == '.')
                {
                    continue;
                }
                else if (x == ':')
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        public static bool CheckInvalidFileNameChars(string text)
        { // ファイル名に使用できない文字が含まれていないか、確認する。true:ok, false:invalid
            char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
            if (text.IndexOfAny(invalidChars) < 0)
            {
                return true;
            }

            return false;
        }

        public static int TimeStringToInt(string text, ref int position)
        { // "12:34:56" hour:min:sec表示の文字列を数字に変換する。
            if (text == null)
            {
                throw new ArgumentNullException();
            }

            if (position < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            char c;
            int x = -1;

            // while ((position < text.Length) && (text[position] == '+' || text[position] == ' ')) position++;
            while (position < text.Length)
            {
                c = text[position++];
                if (c >= '0' && c <= '9')
                {
                    if (x >= 100_000_000)
                    {
                        x = 1000_000_000; // limit
                    }
                    else if (x == -1)
                    {
                        x = c - '0'; // first
                    }
                    else
                    {
                        x = (x * 10) + (c - '0');
                    }
                }
                else if (c == '+' || c == ' ')
                { // ignore plus/space
                    continue;
                }
                else if (c == ':')
                {
                    return x;
                }
                else
                {
                    return -1; // invalid character
                }
            }

            return x;
        }

        public static string IntToTimeString(int hour, int min, int sec)
        { // "12:34:56" 数字をhour:min:sec表示の文字列に変換する。
            if (hour < -1 || hour >= 24 || min < -1 || min >= 60 || sec < -1 || sec >= 60)
            {
                return "00:00:00";
            }

            int x;
            char[] text = new char[8];
            x = hour / 10;
            hour %= 10;
            text[0] = (char)('0' + x);
            text[1] = (char)('0' + hour);
            text[2] = ':';
            x = min / 10;
            min %= 10;
            text[3] = (char)('0' + x);
            text[4] = (char)('0' + min);
            text[5] = ':';
            x = sec / 10;
            sec %= 10;
            text[6] = (char)('0' + x);
            text[7] = (char)('0' + sec);
            return new string(text);
        }
    }
}
