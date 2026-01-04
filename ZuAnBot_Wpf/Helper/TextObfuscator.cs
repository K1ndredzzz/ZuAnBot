using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using hyjiacan.py4n;

namespace ZuAnBot_Wpf.Helper
{
    /// <summary>
    /// 文字混淆器 - 用于绕过敏感词屏蔽
    /// 基于拼音发音检测和替换
    /// </summary>
    public class TextObfuscator
    {
        // 需要替换的拼音发音列表(不含声调)
        private static readonly HashSet<string> SensitivePinyin = new HashSet<string>
        {
            // 常见敏感发音
            "ma", "ba", "die", "niang", "nai",
            "pi", "gu", "tui",
            "si", "gun", "cao", "ri", "nong", "gan", "gao", "da",
            "sha", "chun", "ben", "jian", "zei", "zha",
            "fei", "la", "ji", "gou", "zhu", "lv",
            "ni", "bi", "wo", "ta"
        };

        /// <summary>
        /// 对文本进行混淆处理 - 基于拼音发音
        /// </summary>
        /// <param name="text">原始文本</param>
        /// <param name="aggressiveMode">激进模式 - 转换所有汉字为拼音</param>
        /// <returns>混淆后的文本</returns>
        public static string Obfuscate(string text, bool aggressiveMode = false)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var result = new StringBuilder();

            foreach (char c in text)
            {
                if (IsChineseCharacter(c))
                {
                    // 获取拼音(不带声调) - 返回数组,取第一个读音
                    var pinyinArray = Pinyin4Net.GetPinyin(c, PinyinFormat.WITHOUT_TONE);
                    string pinyin = pinyinArray != null && pinyinArray.Length > 0
                        ? pinyinArray[0].ToLower()
                        : c.ToString();

                    // DEBUG: 添加日志
                    System.Diagnostics.Debug.WriteLine($"字符: {c}, 拼音: {pinyin}, 是否敏感: {SensitivePinyin.Contains(pinyin)}");

                    if (aggressiveMode)
                    {
                        // 激进模式：所有汉字转拼音
                        if (result.Length > 0 && !result.ToString().EndsWith(" "))
                            result.Append(" "); // 拼音之间加空格
                        result.Append(pinyin);
                    }
                    else
                    {
                        // 普通模式：只转换敏感发音的字
                        if (SensitivePinyin.Contains(pinyin))
                        {
                            // 转换为拼音
                            if (result.Length > 0 && !char.IsWhiteSpace(result[result.Length - 1]))
                                result.Append(" "); // 拼音之间加空格
                            result.Append(pinyin);
                        }
                        else
                        {
                            // 保留原汉字,但也要加空格分隔
                            if (result.Length > 0 && !char.IsWhiteSpace(result[result.Length - 1]))
                                result.Append(" ");
                            result.Append(c);
                        }
                    }
                }
                else
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// 判断是否为中文字符
        /// </summary>
        private static bool IsChineseCharacter(char c)
        {
            return c >= 0x4E00 && c <= 0x9FA5;
        }

        /// <summary>
        /// 添加敏感拼音
        /// </summary>
        public static void AddSensitivePinyin(string pinyin)
        {
            if (!string.IsNullOrWhiteSpace(pinyin))
            {
                SensitivePinyin.Add(pinyin.ToLower());
            }
        }

        /// <summary>
        /// 移除敏感拼音
        /// </summary>
        public static void RemoveSensitivePinyin(string pinyin)
        {
            if (!string.IsNullOrWhiteSpace(pinyin))
            {
                SensitivePinyin.Remove(pinyin.ToLower());
            }
        }

        /// <summary>
        /// 获取当前敏感拼音列表
        /// </summary>
        public static IEnumerable<string> GetSensitivePinyins()
        {
            return SensitivePinyin.AsEnumerable();
        }

        /// <summary>
        /// 清空敏感拼音列表
        /// </summary>
        public static void ClearSensitivePinyins()
        {
            SensitivePinyin.Clear();
        }
    }
}
