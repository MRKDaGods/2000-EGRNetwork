using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using static MRK.EGRLogger;

namespace MRK {
    public class EGRCDNResourceManager : EGRBase {
        const string RESOURCE_NAME = "res";
        const string LOCK_NAME = "lock";

        float m_MaxResourceLockTime;
        int m_MaxResourceSize;

        static string Path => $"{EGRMain.Instance.WorkingDirectory}\\CDN";
        
        public void Initialize(EGRNetworkConfig config) {
            LogInfo("Initializing CDN Resource Manager");

            if (!Directory.Exists(Path)) {
                Directory.CreateDirectory(Path);
            }

            m_MaxResourceLockTime = config["NET_CDN_RESOURCE_MAX_LOCK_TIME"].Float;
            m_MaxResourceSize = config["NET_CDN_RESOURCE_MAX_SIZE"].Int;
        }

        string GetResourcePath(string resource) {
            return $"{Path}\\{resource}";
        }

        bool ResourceExists(string resource) {
            return Directory.Exists(GetResourcePath(resource));
        }

        string GetResourceLockPath(string resource) {
            return $"{GetResourcePath(resource)}\\{LOCK_NAME}";
        }

        bool IsResourceLocked(string resource, bool checkExistance = true) {
            if (checkExistance) {
                if (!ResourceExists(resource))
                    return false;
            }

            FileInfo fileInfo = new(GetResourceLockPath(resource));
            if (!fileInfo.Exists)
                return false;

            if ((float)(DateTime.UtcNow - fileInfo.CreationTimeUtc).TotalSeconds > m_MaxResourceLockTime) {
                fileInfo.Delete();
                return false;
            }

            return true;
        }

        bool TryReadResourceHeader(string path, out EGRCDNResourceHeader header) {
            header = default;

            try {
                using FileStream fs = new(path, FileMode.Open);
                using BinaryReader reader = new(fs);

                bool result = EGRCDNResourceHeader.TryReadHeader(reader, out header);
                reader.Close();
                return result;
            }
            catch {
                return false;
            }
        }

        public bool QueryResource(string resource, byte[] sig, out EGRCDNResource cdnResource) {
            cdnResource = null;

            if (!ResourceExists(resource))
                return false;

            if (IsResourceLocked(resource, false))
                return false;

            string resourceDir = GetResourcePath(resource);
            string path = $"{resourceDir}\\{RESOURCE_NAME}";
            EGRCDNResourceHeader header;
            if (!TryReadResourceHeader(path, out header))
                return false;

            //check header for expiry
            //verify that total size is greater than size in header
            if ((header.Lifetime != -1L && header.CreationDate + header.Lifetime < DateTime.UtcNow.Ticks)
                || header.Size > header.TotalFileSize) {
                //delete resource
                Client.IOScheduler.DeleteDirectory(resourceDir, MRKIOSchedulerPriority.Low);
                return false;
            }

            if (!((ReadOnlySpan<byte>)sig).SequenceEqual(header.Signature)) {
                return false; //invalid sig
            }

            try {
                using FileStream fs = new(path, FileMode.Open);
                using BinaryReader reader = new(fs);

                fs.Position += EGRCDNResourceHeader.PhysicalSize;
                byte[] bytes = reader.ReadBytes((int)header.Size);
                cdnResource = new(header, bytes);

                //add locking

                reader.Close();
            }
            catch {
                return false;
            }

            return true;
        }

        public bool CreateResource(string resourceName, string path) {
            if (ResourceExists(resourceName))
                return false;

            if (!File.Exists(path))
                return false;

            byte[] bytes = File.ReadAllBytes(path); //consider making it async?
            if (bytes.Length > m_MaxResourceSize)
                return false;

            string resourceDir = GetResourcePath(resourceName);
            Directory.CreateDirectory(resourceDir);
            string resourcePath = $"{resourceDir}\\{RESOURCE_NAME}";

            EGRCDNResourceHeader header = new EGRCDNResourceHeader {
                CreationDate = DateTime.UtcNow.Ticks,
                Lifetime = -1L,
                Size = bytes.Length,
                Signature = MRKCryptography.SaltedChecksum(bytes)
            };

            using FileStream fs = new(resourcePath, FileMode.Create);
            using BinaryWriter writer = new(fs);

            EGRCDNResourceHeader.WriteHeader(writer, header);

            writer.Write(bytes);
            writer.Close();

            return true;
        }

        public bool QueryResourceHeader(string resource, out EGRCDNResourceHeader header) {
            header = default;

            if (!ResourceExists(resource))
                return false;

            if (IsResourceLocked(resource, false))
                return false;

            string resourceDir = GetResourcePath(resource);
            string path = $"{resourceDir}\\{RESOURCE_NAME}";
            return TryReadResourceHeader(path, out header);
        }
    }
}
