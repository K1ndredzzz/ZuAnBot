using System;
using hyjiacan.py4n;

class Test {
    static void Main() {
        char[] testChars = {'妈', '吗', '马', '嘛', '麻'};
        foreach(var c in testChars) {
            var pinyin = Pinyin4Net.GetPinyin(c, PinyinFormat.WITHOUT_TONE);
            Console.WriteLine($"{c} -> {pinyin}");
        }
    }
}
