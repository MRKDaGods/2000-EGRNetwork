using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace MRK {
    public abstract class EGRFileSysIO<T> {
        protected string m_Root;
        protected ReaderWriterLockSlim m_Lock;
        readonly static List<string> ms_EmptyBuffer;

        static EGRFileSysIO() {
            ms_EmptyBuffer = new List<string>();
        }

        public EGRFileSysIO(string dir) {
            m_Root = dir;
            if (!Directory.Exists(dir))
                CreateRecursiveDir(dir);

            m_Lock = new ReaderWriterLockSlim();
        }

        void CreateRecursiveDir(string dir) {
            int start = 0;
            while (start < dir.Length) {
                int sepIdx = dir.IndexOf('\\', start);
                if (sepIdx == -1)
                    sepIdx = dir.Length - 1;

                string _dir = dir.Substring(0, sepIdx + 1);
                if (!Directory.Exists(_dir))
                    Directory.CreateDirectory(_dir);

                start = sepIdx + 1;
            }
        }

        protected abstract string GetDataFile(T obj);

        protected abstract void Write(T obj, BinaryWriter w);

        protected abstract T Read(BinaryReader r);

        protected virtual void DeleteIndex(T obj) {
        }

        public void Write(T obj) {
            try {
                m_Lock.EnterWriteLock();

                string dir = $"{m_Root}\\{GetDataFile(obj)}";
                string owner = Path.GetDirectoryName(dir);
                if (!Directory.Exists(owner))
                    CreateRecursiveDir(owner);

                using (FileStream fstream = new FileStream(dir, FileMode.Create))
                using (BinaryWriter writer = new BinaryWriter(fstream)) {
                    Write(obj, writer);
                    writer.Close();
                }
            }
            catch {
            }
            finally {
                m_Lock.ExitWriteLock();
            }
        }

        public T Read(string dir) {
            dir = $"{m_Root}\\{dir}";
            if (!File.Exists(dir))
                return default;

            T obj = default;

            try {
                m_Lock.EnterReadLock();

                using (FileStream fstream = new FileStream(dir, FileMode.Open))
                using (BinaryReader reader = new BinaryReader(fstream)) {
                    obj = Read(reader);
                    reader.Close();
                }
            }
            catch {
                //hmm?
            }
            finally {
                m_Lock.ExitReadLock();
            }

            return obj;
        }

        public void Delete(T obj) {
            try {
                m_Lock.EnterWriteLock();

                string path = $"{m_Root}\\{GetDataFile(obj)}";
                if (File.Exists(path))
                    File.Delete(path);

                DeleteIndex(obj);
            }
            catch {
            }
            finally {
                m_Lock.ExitWriteLock();
            }
        }

        //MUST ADD \ for subdir
        public IEnumerable<string> GetFiles(string sub = "") {
            string dir = m_Root + sub;
            if (!Directory.Exists(dir))
                return ms_EmptyBuffer;

            return Directory.EnumerateFiles(m_Root + sub);
        }

        public bool Exists(T obj) {
            return File.Exists($"{m_Root}\\{GetDataFile(obj)}");
        }

        public bool DirExists(string sub) {
            return Directory.Exists($"{m_Root}\\{sub}");
        }
    }
}
