using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRK {
    public enum EGRPlaceType : ushort {
        None = 0,
        Restaurant,
        Delivery,
        Gym,
        Smoking,
        Religion,
        Cinema,
        Park,
        Mall,
        Museum,
        Library,
        Grocery,
        Apparel,
        Electronics,
        Sport,
        BeautySupply,
        Home,
        CarDealer,
        Convenience,
        Hotel,
        ATM,
        Gas,
        Hospital,
        Pharmacy,
        CarWash,
        Parking,
        CarRental,
        BeautySalons,
        EVC,

        MAX
    }

    public class EGRPlace {
        public string Name { get; private set; }
        public string Type { get; private set; }
        public string CID { get; private set; }
        public ulong CIDNum { get; private set; }
        public string Address { get; private set; }
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public string[] Ex { get; private set; }
        public EGRPlaceType[] Types { get; private set; }
        public ulong Chain { get; set; } //is place related to a chain?

        public EGRPlace(string name, string type, string cid, string addr, string lat, string lng, string[] ex, ulong chain = 0) {
            Name = name;
            Type = type;
            CID = cid;
            CIDNum = ulong.Parse(CID);
            Address = addr;
            Latitude = double.Parse(lat);
            Longitude = double.Parse(lng);
            Ex = ex ?? new string[0];
            Types = new EGRPlaceType[1] { EGRPlaceType.None };
            Chain = chain;
        }

        public EGRPlace(string name, string type, string cid, string addr, double lat, double lng, string[] ex, ulong chain = 0) {
            Name = name;
            Type = type;
            CID = cid;
            CIDNum = ulong.Parse(CID);
            Address = addr;
            Latitude = lat;
            Longitude = lng;
            Ex = ex ?? new string[0];
            Types = new EGRPlaceType[1] { EGRPlaceType.None };
            Chain = chain;
        }

        public void SetTypes(EGRPlaceType[] types) {
            Types = types;
        }

        public void AddType(EGRPlaceType type) {
            for (int i = 0; i < Types.Length; i++)
                if (Types[i] == type)
                    return;

            EGRPlaceType[] types = Types;
            Array.Resize(ref types, Types.Length + 1);
            types[types.Length - 1] = type;
            Types = types;
        }
    }

    public class EGRPlaceTypeMatcher {
        static Dictionary<EGRPlaceType, List<string>> ms_Matcher;

        static EGRPlaceTypeMatcher() {
            LoadMatcher();
        }

        public static EGRPlaceType[] Match(EGRPlace place) {
            List<EGRPlaceType> ret = new List<EGRPlaceType> { EGRPlaceType.None };
            foreach (KeyValuePair<EGRPlaceType, List<string>> pair in ms_Matcher) {
                foreach (string m in pair.Value) {
                    string[] strs = new string[1 + place.Ex.Length];
                    strs[0] = place.Type;

                    for (int i = 0; i < place.Ex.Length; i++)
                        strs[i + 1] = place.Ex[i];

                    foreach (string str in strs) {
                        if (str.ToLower().Contains(m)) {
                            if (!ret.Contains(pair.Key))
                                ret.Add(pair.Key);
                        }
                    }
                }
            }

            return ret.ToArray();
        }

        static void Add(EGRPlaceType type, params string[] matches) {
            ms_Matcher[type] = matches.ToList();
        }

        static void LoadMatcher() {
            ms_Matcher = new Dictionary<EGRPlaceType, List<string>>();

            //manual matching
            Add(EGRPlaceType.Restaurant, "restaurant", "fast food", "fast-food");
            Add(EGRPlaceType.Delivery, "delivery");
            Add(EGRPlaceType.Gym, "gym", "fitness club", "fitness center", "fitness centre");
            Add(EGRPlaceType.Smoking, "smoking", "smoke", "cigarette");

            //auto fill empty
            for (EGRPlaceType type = EGRPlaceType.None + 1; type < EGRPlaceType.MAX; type++) {
                if (!ms_Matcher.ContainsKey(type)) {
                    ms_Matcher[type] = new List<string> {
                        type.ToString().ToLower()
                    };
                }
            }
        }
    }

    public class EGRPlaceChain {
        public ulong ID { get; private set; }
        public string Name { get; private set; }
        public string[] Matches { get; private set; }

        public EGRPlaceChain(ulong id, string name, string[] matches) {
            ID = id;
            Name = name;
            Matches = matches;
        }
    }
}
