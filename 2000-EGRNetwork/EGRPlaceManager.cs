using MRK.Networking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using static MRK.EGRLogger;

namespace MRK {
    public class EGRPlaceManager {
        const double MIN_LAT = 22.05d;
        const double MAX_LAT = 31.90d;
        const double MIN_LNG = 25.13d;
        const double MAX_LNG = 36.84d;

        readonly string[] m_VisualFormatPrefix;
        string m_RootPath;
        ReaderWriterLock m_RWLock;

        string m_PlacesPath => $"{m_RootPath}\\Places";
        string m_IndexPath => $"{m_RootPath}\\PlacesIndex";
        string m_TypeIndexPath => $"{m_RootPath}\\PlacesTypeIndex";
        string m_PlacesChainsPath => $"{m_RootPath}\\PlacesChains";
        string m_PlacesChainsTemplatePath => $"{m_RootPath}\\PlacesChainsTemplates";

        public EGRPlaceManager() {
            m_VisualFormatPrefix = new string[] {
                "Name:",
                "Type:",
                "CID:",
                "Address:",
                "Lat:",
                "Lng:",
                "EX:",

                //CHAIN TEMPLATE
                "CNAME:",
                "ID:",
                "MATCH:"
            };

            m_RWLock = new ReaderWriterLock();
        }

        public void Initialize(string path, string root) {
            if (!Directory.Exists(root))
                Directory.CreateDirectory(root);

            m_RootPath = root;

            if (!Directory.Exists(m_PlacesPath))
                Directory.CreateDirectory(m_PlacesPath);

            if (!Directory.Exists(m_IndexPath)) {
                Directory.CreateDirectory(m_IndexPath);
                CreateIndexFolders(MIN_LAT, MAX_LAT, MIN_LNG, MAX_LNG);
            }

            if (!Directory.Exists(m_TypeIndexPath)) {
                Directory.CreateDirectory(m_TypeIndexPath);
                CreateTypeIndexFolders();
            }

            if (!Directory.Exists(m_PlacesChainsPath)) {
                Directory.CreateDirectory(m_PlacesChainsPath);
            }

            if (!Directory.Exists(m_PlacesChainsTemplatePath)) {
                Directory.CreateDirectory(m_PlacesChainsTemplatePath);
            }

            if (!Directory.Exists(path))
                return;

            foreach (string filename in Directory.EnumerateFiles(path, "*.txt")) {
                //convert from visual data
                string name = "", type = "", cid = "", addr = "", lat = "", lng = "";
                List<string> ex = new List<string>();

                string[] lines = File.ReadAllLines(filename);
                foreach (string line in lines) {
                    if (string.IsNullOrEmpty(line) || string.IsNullOrWhiteSpace(line))
                        continue;

                    string prefix = null;
                    int prefIdx = -1;
                    for (int i = 0; i < m_VisualFormatPrefix.Length; i++) {
                        if (line.StartsWith(m_VisualFormatPrefix[i])) {
                            prefix = m_VisualFormatPrefix[i];
                            prefIdx = i;
                            break;
                        }
                    }

                    if (prefIdx == -1)
                        continue;

                    string val = line.Substring(prefix.Length + 1);

                    switch (prefIdx) {

                        case 0:
                            name = val;
                            break;

                        case 1:
                            type = val;
                            break;

                        case 2:
                            cid = val;
                            break;

                        case 3:
                            addr = val;
                            break;

                        case 4:
                            lat = val;
                            break;

                        case 5:
                            lng = val;
                            break;

                        case 6:
                            ex.Add(val);
                            break;

                    }
                }

                //no overwritting for egr data
                if (PlaceExists(cid))
                    continue;

                EGRPlace place = new EGRPlace(name, type, cid, addr, lat, lng, ex.ToArray());
                Directory.CreateDirectory(GetPlacePath(place));
                WritePlaceInfo(place);
                WritePlaceIndex(place);
                LogInfo($"Added place -> [{place.CID}] {place.Name} - {place.Type}");
            }
        }

        public void CreateIndexFolders() {
            CreateIndexFolders(MIN_LAT, MAX_LAT, MIN_LNG, MAX_LNG);
        }

        public void CreateIndexFolders(double minLat, double maxLat, double minLng, double maxLng) {
            LogInfo($"Indexing minLat={minLat}, maxLat={maxLat}, minLng={minLng}, maxLng={maxLng}");

            int lats = 0;
            for (double lat = minLat; lat <= maxLat; lat += 0.01d) {
                Directory.CreateDirectory($"{m_IndexPath}\\Lat-{lat:F}");
                lats++;
            }

            int lngs = 0;
            for (double lng = minLng; lng <= maxLng; lng += 0.01d) {
                Directory.CreateDirectory($"{m_IndexPath}\\Lng-{lng:F}");
                lngs++;
            }

            LogInfo($"Created {lats} lats and {lngs} lngs");
        }

        string GetPlacePath(string cid) {
            return $"{m_PlacesPath}\\{cid}";
        }

        string GetPlacePath(EGRPlace place) {
            return $"{m_PlacesPath}\\{place.CID}";
        }

        bool PlaceExists(string cid) {
            return Directory.Exists(GetPlacePath(cid));
        }

        void WritePlaceInfo(EGRPlace place) {
            try {
                m_RWLock.AcquireWriterLock(10000);

                using (FileStream fstream = new FileStream($"{GetPlacePath(place)}\\egr0", FileMode.Create))
                using (BinaryWriter writer = new BinaryWriter(fstream)) {
                    writer.Write(place.Name);
                    writer.Write(place.Type);
                    writer.Write(place.CID);
                    writer.Write(place.Address);
                    writer.Write(place.Latitude);
                    writer.Write(place.Longitude);

                    writer.Write(place.Ex.Length);
                    foreach (string ex in place.Ex)
                        writer.Write(ex);

                    writer.Close();
                }
            }
            finally {
                m_RWLock.ReleaseWriterLock();
            }
        }

        EGRPlace ReadPlaceInfo(string cid) {
            try {
                m_RWLock.AcquireReaderLock(10000);

                using (FileStream fstream = new FileStream($"{GetPlacePath(cid)}\\egr0", FileMode.Open))
                using (BinaryReader reader = new BinaryReader(fstream)) {
                    string name = reader.ReadString();
                    string type = reader.ReadString();
                    string _cid = reader.ReadString();
                    string addr = reader.ReadString();
                    double lat = reader.ReadDouble();
                    double lng = reader.ReadDouble();

                    int exLen = reader.ReadInt32();
                    List<string> ex = new List<string>();
                    for (int i = 0; i < exLen; i++)
                        ex.Add(reader.ReadString());

                    reader.Close();
                    return new EGRPlace(name, type, _cid, addr, lat, lng, ex.ToArray());
                }
            }
            finally {
                m_RWLock.ReleaseReaderLock();
            }
        }

        double RoundIndex(double number) {
            double tmp = Math.Pow(10, 2);
            return Math.Truncate(number * tmp) / tmp;
        }

        string GetIndexPosStr(double d) {
            return RoundIndex(d).ToString("F");
        }

        void WritePlaceIndex(EGRPlace place) {
            if (place.Latitude < MIN_LAT || place.Latitude > MAX_LAT || place.Longitude < MIN_LNG || place.Longitude > MAX_LNG)
                return;

            string indexLat = GetIndexPosStr(place.Latitude);
            string indexLng = GetIndexPosStr(place.Longitude);
            File.WriteAllText($"{m_IndexPath}\\Lat-{indexLat}\\{place.CID}", "");
            File.WriteAllText($"{m_IndexPath}\\Lng-{indexLng}\\{place.CID}", "");

            LogInfo($"Indexed {place.CID} [{indexLat}, {indexLng}]");
        }

        List<string> GetCIDsAtLatIndex(double lat) {
            string dir = $"{m_IndexPath}\\Lat-{GetIndexPosStr(lat)}";
            if (!Directory.Exists(dir))
                return null;

            List<string> str = new List<string>();
            foreach (string fname in Directory.EnumerateFiles(dir)) {
                str.Add(Path.GetFileName(fname));
            }

            return str;
        }

        List<string> GetCIDsAtLngIndex(double lng) {
            string dir = $"{m_IndexPath}\\Lng-{GetIndexPosStr(lng)}";
            if (!Directory.Exists(dir))
                return null;

            List<string> str = new List<string>();
            foreach (string fname in Directory.EnumerateFiles(dir)) {
                str.Add(Path.GetFileName(fname));
            }

            return str;
        }

        public List<EGRPlace> GetPlaces(double minLat, double minLng, double maxLat, double maxLng, int zoomLvl) {
            minLat = Math.Max(minLat, MIN_LAT);
            minLng = Math.Max(minLng, MIN_LNG);
            maxLat = Math.Min(maxLat, MAX_LAT);
            maxLng = Math.Min(maxLng, MAX_LNG);

            List<string> qualifying = new List<string>();
            List<EGRPlace> ret = new List<EGRPlace>();

            for (double lat = RoundIndex(minLat); lat <= maxLat; lat += 0.01d) {
                List<string> cids = GetCIDsAtLatIndex(lat);
                if (cids == null)
                    continue;

                if (cids.Count > 0)
                    qualifying.AddRange(cids);
            }

            for (double lng = RoundIndex(minLng); lng <= maxLng; lng += 0.01d) {
                List<string> cids = GetCIDsAtLngIndex(lng);
                if (cids == null)
                    continue;

                foreach (string cid in cids) {
                    if (qualifying.Contains(cid)) {
                        //PLACE found
                        ret.Add(ReadPlaceInfo(cid));
                    }
                }
            }

            return ret;
        }

        public List<EGRPlace> GetAllPlaces() {
            List<EGRPlace> ret = new List<EGRPlace>();
            foreach (string filename in Directory.EnumerateDirectories(m_PlacesPath)) {
                ret.Add(ReadPlaceInfo(Path.GetFileName(filename)));
            }

            return ret;
        }

        public void CreateTypeIndexFolders() {
            for (EGRPlaceType type = EGRPlaceType.None; type < EGRPlaceType.MAX; type++) {
                Directory.CreateDirectory($"{m_TypeIndexPath}\\{type}");
                LogInfo($"Created type index dir for {type}");
            }
        }

        public void GeneratePlaceTypes(EGRPlace place) {
            LogInfo($"Matching {place.CID} - {place.Name}");
            EGRPlaceType[] matches = EGRPlaceTypeMatcher.Match(place);
            LogInfo($"Found {matches.Length} matches [{string.Join(", ", matches)}]");
            //TODO assign
        }

        public void WritePlaceTypeIndex(EGRPlace place) {
            foreach (EGRPlaceType type in place.Types) {
                File.WriteAllText($"{m_TypeIndexPath}\\{type}\\{place.CID}", "");
            }
        }

        public void GeneratePlaceChainTemplate(string name) {
            File.WriteAllText($"{m_PlacesChainsTemplatePath}\\{EGRUtils.FixInvalidString(name)}.txt", $"CNAME: {name}\r\nID: {EGRUtils.GetRandomID()}\r\nMATCH: {name.ToLower()}");
            LogInfo($"Generated place chain template '{name}'");
        }

        public void WritePlaceChain()

        public void GeneratePlaceChainFromTemplate(string tempName) {
            //get template?
            string dir = $"{m_PlacesChainsTemplatePath}\\{EGRUtils.FixInvalidString(tempName)}.txt";
            if (!File.Exists(dir))
                return;

            string name = "", id = "";
            List<string> matches = new List<string>();

            string[] lines = File.ReadAllLines(dir);
            foreach (string line in lines) {
                if (string.IsNullOrEmpty(line) || string.IsNullOrWhiteSpace(line))
                    continue;

                string prefix = null;
                int prefIdx = -1;
                for (int i = 0; i < m_VisualFormatPrefix.Length; i++) {
                    if (line.StartsWith(m_VisualFormatPrefix[i])) {
                        prefix = m_VisualFormatPrefix[i];
                        prefIdx = i;
                        break;
                    }
                }

                if (prefIdx == -1)
                    continue;

                string val = line.Substring(prefix.Length + 1);

                //789
                switch (prefIdx) {

                    case 7:
                        name = val;
                        break;

                    case 8:
                        id = val;
                        break;

                    case 9:
                        matches.Add(val);
                        break;

                }
            }

            EGRPlaceChain chain = new EGRPlaceChain(ulong.Parse(id), name, matches.ToArray());
        }
    }
}
