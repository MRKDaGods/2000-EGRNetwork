using System;
using System.Collections.Generic;

namespace MRK {
    public class MRKParser {
        public static List<float> ParseArray(string input) {
            if (string.IsNullOrEmpty(input))
                return null;

            int fIdx = input.IndexOf('[');
            int eIdx = input.LastIndexOf(']');
            if (fIdx == -1 || eIdx == -1)
                return null;

            string concInput = input.Substring(fIdx + 1, eIdx - fIdx - 1);
            List<float> list = new List<float>();

            string buffer = "";
            int idx = 0;
            while (idx < concInput.Length) {
                char cur = concInput[idx++];
                if (char.IsDigit(cur) || cur == '.') {
                    buffer += cur;
                    continue;
                }

                if (cur == ',') {
                    list.Add(float.Parse(buffer));
                    buffer = "";
                }
            }

            if (buffer.Length > 0) {
                list.Add(float.Parse(buffer));
            }

            return list;
        }

        public static Range? ParseRange(string input) {
            if (string.IsNullOrEmpty(input))
                return null;

            //x..y
            int sepIdx = input.IndexOf("..");
            string begin = input.Substring(0, sepIdx);
            string end = input.Substring(sepIdx + 2);
            return new Range(int.Parse(begin), int.Parse(end));
        }
    }
}
