﻿using System;
using System.Globalization;
using System.Text;
using MoonSharp.Interpreter.Compatibility;

namespace MoonSharp.Interpreter.Tree
{
    internal static class LexerUtils
    {
        public static double ParseNumber(Token T)
        {
            string txt = T.Text;

            if (!double.TryParse(txt, NumberStyles.Float, CultureInfo.InvariantCulture, out double res))
            {
                throw new SyntaxErrorException(T, "malformed number near '{0}'", txt);
            }

            return res;
        }

        public static double ParseHexInteger(Token T)
        {
            string txt = T.Text;
            if ((txt.Length < 2) || (txt[0] != '0' && (char.ToUpper(txt[1]) != 'X')))
            {
                throw new InternalErrorException("hex numbers must start with '0x' near '{0}'.", txt);
            }

            if (!ulong.TryParse(txt.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong res))
            {
                throw new SyntaxErrorException(T, "malformed number near '{0}'", txt);
            }

            return res;
        }

        public static string ReadHexProgressive(string s, ref double d, out int digits)
        {
            digits = 0;

            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];

                if (CharIsHexDigit(c))
                {
                    int v = HexDigit2Value(c);
                    d *= 16.0;
                    d += v;
                    ++digits;
                }
                else
                {
                    return s.Substring(i);
                }
            }

            return string.Empty;
        }

        public static double ParseHexFloat(Token T)
        {
            string s = T.Text;

            try
            {
                if ((s.Length < 2) || (s[0] != '0' && (char.ToUpper(s[1]) != 'X')))
                {
                    throw new InternalErrorException("hex float must start with '0x' near '{0}'", s);
                }

                s = s.Substring(2);

                double value = 0.0;
                int exp = 0;

                s = ReadHexProgressive(s, ref value, out int _);

                if (s.Length > 0 && s[0] == '.')
                {
                    s = s.Substring(1);
                    s = ReadHexProgressive(s, ref value, out exp);
                }

                exp *= -4;

                if (s.Length > 0 && char.ToUpper(s[0]) == 'P')
                {
                    if (s.Length == 1)
                    {
                        throw new SyntaxErrorException(T, "invalid hex float format near '{0}'", s);
                    }

                    s = s.Substring(s[1] == '+' ? 2 : 1);

                    int exp1 = int.Parse(s, CultureInfo.InvariantCulture);

                    exp += exp1;
                }

                return value * Math.Pow(2, exp);
            }
            catch (FormatException)
            {
                throw new SyntaxErrorException(T, "malformed number near '{0}'", s);
            }
        }


        public static int HexDigit2Value(char c)
        {
            if (c >= '0' && c <= '9')
            {
                return c - '0';
            }

            if (c >= 'A' && c <= 'F')
            {
                return 10 + (c - 'A');
            }

            if (c >= 'a' && c <= 'f')
            {
                return 10 + (c - 'a');
            }

            throw new InternalErrorException("invalid hex digit near '{0}'", c);
        }

        public static bool CharIsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        public static bool CharIsHexDigit(char c)
        {
            return CharIsDigit(c) ||
                   c == 'a' || c == 'b' || c == 'c' || c == 'd' || c == 'e' || c == 'f' ||
                   c == 'A' || c == 'B' || c == 'C' || c == 'D' || c == 'E' || c == 'F';
        }

        public static string AdjustLuaLongString(string str)
        {
            if (str.StartsWith("\r\n"))
            {
                str = str.Substring(2);
            }
            else if (str.StartsWith("\n"))
            {
                str = str.Substring(1);
            }

            return str;
        }

        public static string UnescapeLuaString(Token token, string str)
        {
            if (!Framework.Do.StringContainsChar(str, '\\'))
            {
                return str;
            }

            var sb = new StringBuilder();

            bool escape = false;
            bool hex = false;
            int unicodeState = 0;
            string hexPrefix = "";
            string val = "";
            bool zmode = false;

            foreach (char c in str)
            {
                redo:
                if (escape)
                {
                    if (val.Length == 0 && !hex && unicodeState == 0)
                    {
                        if (c == 'a')
                        {
                            sb.Append('\a');
                            escape = false;
                            zmode = false;
                        }
                        else if (c == '\r')
                        {
                        } // this makes \\r\n -> \\n
                        else if (c == '\n')
                        {
                            sb.Append('\n');
                            escape = false;
                        }
                        else if (c == 'b')
                        {
                            sb.Append('\b');
                            escape = false;
                        }
                        else if (c == 'f')
                        {
                            sb.Append('\f');
                            escape = false;
                        }
                        else if (c == 'n')
                        {
                            sb.Append('\n');
                            escape = false;
                        }
                        else if (c == 'r')
                        {
                            sb.Append('\r');
                            escape = false;
                        }
                        else if (c == 't')
                        {
                            sb.Append('\t');
                            escape = false;
                        }
                        else if (c == 'v')
                        {
                            sb.Append('\v');
                            escape = false;
                        }
                        else if (c == '\\')
                        {
                            sb.Append('\\');
                            escape = false;
                            zmode = false;
                        }
                        else if (c == '"')
                        {
                            sb.Append('\"');
                            escape = false;
                            zmode = false;
                        }
                        else if (c == '\'')
                        {
                            sb.Append('\'');
                            escape = false;
                            zmode = false;
                        }
                        else if (c == '/')
                        {
                            sb.Append('/');
                            escape = false;
                            zmode = false;
                        }
                        else if (c == '[')
                        {
                            sb.Append('[');
                            escape = false;
                            zmode = false;
                        }
                        else if (c == ']')
                        {
                            sb.Append(']');
                            escape = false;
                            zmode = false;
                        }
                        else if (c == 'x')
                        {
                            hex = true;
                        }
                        else if (c == 'u')
                        {
                            unicodeState = 1;
                        }
                        else if (c == 'z')
                        {
                            zmode = true;
                            escape = false;
                        }
                        else if (CharIsDigit(c))
                        {
                            val = val + c;
                        }
                        else
                        {
                            throw new SyntaxErrorException(token, "invalid escape sequence near '\\{0}'", c);
                        }
                    }
                    else
                    {
                        if (unicodeState == 1)
                        {
                            if (c != '{')
                            {
                                throw new SyntaxErrorException(token, "'{' expected near '\\u'");
                            }

                            unicodeState = 2;
                        }
                        else if (unicodeState == 2)
                        {
                            if (c == '}')
                            {
                                int i = int.Parse(val, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                                sb.Append(ConvertUtf32ToChar(i));
                                unicodeState = 0;
                                val = string.Empty;
                                escape = false;
                            }
                            else if (val.Length >= 8)
                            {
                                throw new SyntaxErrorException(token,
                                    "'}' missing, or unicode code point too large after '\\u'");
                            }
                            else
                            {
                                val += c;
                            }
                        }
                        else if (hex)
                        {
                            if (CharIsHexDigit(c))
                            {
                                val += c;
                                if (val.Length == 2)
                                {
                                    int i = int.Parse(val, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                                    sb.Append(ConvertUtf32ToChar(i));
                                    zmode = false;
                                    escape = false;
                                }
                            }
                            else
                            {
                                throw new SyntaxErrorException(token, "hexadecimal digit expected near '\\{0}{1}{2}'",
                                    hexPrefix, val, c);
                            }
                        }
                        else if (val.Length > 0)
                        {
                            if (CharIsDigit(c))
                            {
                                val = val + c;
                            }

                            if (val.Length == 3 || !CharIsDigit(c))
                            {
                                int i = int.Parse(val, CultureInfo.InvariantCulture);

                                if (i > 255)
                                {
                                    throw new SyntaxErrorException(token, "decimal escape too large near '\\{0}'", val);
                                }

                                sb.Append(ConvertUtf32ToChar(i));

                                zmode = false;
                                escape = false;

                                if (!CharIsDigit(c))
                                {
                                    goto redo;
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (c == '\\')
                    {
                        escape = true;
                        hex = false;
                        val = "";
                    }
                    else
                    {
                        if (!zmode || !char.IsWhiteSpace(c))
                        {
                            sb.Append(c);
                            zmode = false;
                        }
                    }
                }
            }

            if (escape && !hex && val.Length > 0)
            {
                int i = int.Parse(val, CultureInfo.InvariantCulture);
                sb.Append(ConvertUtf32ToChar(i));
                escape = false;
            }

            if (escape)
            {
                throw new SyntaxErrorException(token, "unfinished string near '\"{0}\"'", sb.ToString());
            }

            return sb.ToString();
        }

        private static string ConvertUtf32ToChar(int i)
        {
            return char.ConvertFromUtf32(i);
        }
    }
}