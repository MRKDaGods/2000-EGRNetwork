using System.IO;

namespace MRK {
    public class MRKPlatformUtils {
        public static string LocalizePath(string path) {
            return path.Replace('|', Path.DirectorySeparatorChar);
        }

        public static void CreateRecursiveDirectory(string dir) {
            dir = LocalizePath(dir);

            if (Directory.Exists(dir)) return;

            int start = 0;
            while (start < dir.Length) {
                int sepIdx = dir.IndexOf(Path.DirectorySeparatorChar, start);
                if (sepIdx == -1)
                    sepIdx = dir.Length - 1;

                string _dir = dir.Substring(0, sepIdx + 1);
                if (!Directory.Exists(_dir))
                    Directory.CreateDirectory(_dir);

                start = sepIdx + 1;
            }
        }
    }
}
