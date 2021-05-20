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
        const int MAX_PLACE_COUNT = 32;

        readonly string[] m_VisualFormatPrefix;
        string m_RootPath;
        EGRFileSysIOPlace m_IOPlace;
        EGRFileSysIOPlaceChain m_IOChain;

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
        }

        public void Initialize(string path, string root) {
            m_RootPath = root;
            if (!Directory.Exists(root))
                Directory.CreateDirectory(root);

            m_IOPlace = new EGRFileSysIOPlace(m_PlacesPath);
            m_IOChain = new EGRFileSysIOPlaceChain(m_PlacesChainsPath);

            if (!Directory.Exists(m_IndexPath)) {
                Directory.CreateDirectory(m_IndexPath);
                CreateIndexFolders(MIN_LAT, MAX_LAT, MIN_LNG, MAX_LNG);
            }

            if (!Directory.Exists(m_TypeIndexPath)) {
                Directory.CreateDirectory(m_TypeIndexPath);
                CreateTypeIndexFolders();
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
                GeneratePlaceTypes(place); //this writes to fs
                //m_IOPlace.Write(place);
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

        bool PlaceExists(string cid) {
            return m_IOPlace.DirExists(cid);
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

        bool CanIncludePlace(string cid, int desiredZoom, out EGRPlace place) {
            place = m_IOPlace.Read(cid);
            if (place == null)
                return false;

            int zMin = 7, zMax = 21;
            //manual matching for now?
            EGRPlaceType primaryType = place.Types.Length > 1 ? place.Types[1] : EGRPlaceType.None;
            switch (primaryType) {

                default:
                case EGRPlaceType.None:
                    zMin = 15;
                    break;

                case EGRPlaceType.Mall:
                    zMin = 5;
                    zMax = 21;
                    break;

            }

            return desiredZoom >= zMin && desiredZoom <= zMax;
        }

        public List<EGRPlace> GetPlaces(double minLat, double minLng, double maxLat, double maxLng, int zoomLvl, HashSet<string> mask = null) {
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
                        if (ret.Count >= MAX_PLACE_COUNT)
                            break;

                        if (mask != null && mask.Contains(cid)) {
                            LogInfo($"masked cid {cid}");
                            continue;
                        }

                        EGRPlace place;
                        if (CanIncludePlace(cid, zoomLvl, out place)) {
                            //PLACE found
                            ret.Add(place);
                        }
                    }
                }
            }

            return ret;
        }

        public EGRPlace GetPlace(ulong cid) {
            return m_IOPlace.Read(cid.ToString());
        }

        public List<EGRPlace> GetAllPlaces() {
            List<EGRPlace> ret = new List<EGRPlace>();
            foreach (string filename in Directory.EnumerateDirectories(m_PlacesPath)) {
                ret.Add(m_IOPlace.Read(Path.GetFileName(filename)));
            }

            return ret;
        }

        public void CreateTypeIndexFolders() {
            for (EGRPlaceType type = EGRPlaceType.None; type < EGRPlaceType.MAX; type++) {
                Directory.CreateDirectory($"{m_TypeIndexPath}\\{type}");
                LogInfo($"Created type index dir for {type}");
            }
        }

        public void DeletePlaceTypeIndex(EGRPlace place) {
            foreach (EGRPlaceType type in place.Types) {
                string dir = $"{m_TypeIndexPath}\\{type}\\{place.CID}";
                if (File.Exists(dir))
                    File.Delete(dir);
            }
        }

        public void GeneratePlaceTypes(EGRPlace place) {
            LogInfo($"Matching {place.CID} - {place.Name}");
            EGRPlaceType[] matches = EGRPlaceTypeMatcher.Match(place);
            LogInfo($"Found {matches.Length} matches [{string.Join(", ", matches)}]");

            DeletePlaceTypeIndex(place);
            place.SetTypes(matches);
            WritePlaceTypeIndex(place);

            m_IOPlace.Write(place);
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
            m_IOChain.Write(chain);
            m_IOChain.IndexChain(chain);

            LogInfo($"Generated chain from {tempName}");
        }

        public void AssignPlacesToChain(EGRPlaceChain chain) {
            if (chain == null)
                return;

            List<Tuple<int, string>> wordBuffer = new List<Tuple<int, string>>();
            string buf = "";

            List<EGRPlace> places = GetAllPlaces();
            foreach (EGRPlace place in places) {
                //already has a chain?
                if (place.Chain > 0)
                    continue;

                int idx = 0;
                while (idx < place.Name.Length) {
                    char c = place.Name[idx++];
                    if (c == ' ') {
                        if (buf.Length > 0) {
                            wordBuffer.Add(new Tuple<int, string>(idx - buf.Length - 1, buf));
                            buf = "";
                        }

                        continue;
                    }

                    buf += c;
                }

                if (buf.Length > 0) {
                    wordBuffer.Add(new Tuple<int, string>(idx - buf.Length, buf));
                }

                foreach (string match in chain.Matches) {
                    bool _break = false;

                    foreach (Tuple<int, string> word in wordBuffer) {
                        if (place.Name.Substring(word.Item1).ToLower().StartsWith(match)) {
                            //match must end with a space or eos
                            int matchEnding = word.Item1 + match.Length;
                            if (matchEnding == place.Name.Length || place.Name[matchEnding] == ' ') {
                                place.Chain = chain.ID;
                                m_IOPlace.Write(place);
                                _break = true;

                                LogInfo($"Assigned {place.Name} to chain {chain.Name}");
                            }
                        }
                    }

                    if (_break)
                        break;
                }

                buf = "";
                wordBuffer.Clear();
            }
        }

        public EGRPlaceChain GetChain(string name) {
            return m_IOChain.Read(name);
        }
    }
}
