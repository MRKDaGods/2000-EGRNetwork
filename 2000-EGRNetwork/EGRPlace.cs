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
            Add(EGRPlaceType.Gym, "gym", "fitness club", "fitness center", "fitness centre");
            Add(EGRPlaceType.Smoking, "smoking", "smoke", "cigarette");
            Add(EGRPlaceType.Religion, "church", "mosque", "مسجد", "كنيسة");
            Add(EGRPlaceType.Cinema, "cinema", "theatre", "cineplex", "movie theatre", "movie theater", "theater");
            Add(EGRPlaceType.Park, "park", "playground");
            Add(EGRPlaceType.Pharmacy, "pharmacy", "drugstore", "drug", "drug store");
            Add(EGRPlaceType.Mall, "mall", "city centre", "city center", "shopping centre", "shopping center", "plaza", "galleria", "strip mall");
            Add(EGRPlaceType.Museum, "museum", "exhibit", "gallery");
            Add(EGRPlaceType.Library, "library", "bibliotheca", "book centre", "book center", "information centre", "information center", "book room");
            Add(EGRPlaceType.Grocery, "grocery", "supermarket", "market", "super market", "minimarker");
            Add(EGRPlaceType.Apparel, "apparel", "clothes", "clothing", "garments", "dress", "outfit", "costume");
            Add(EGRPlaceType.Electronics, "electronic", "electronics", "mobile", "laptop", "mobiles", "laptops", "computer", "computers", "smartphone", "smart phone");
            Add(EGRPlaceType.Sport, "sport", "sports", "sporting", "stadium", "training", "football", "soccer", "tennis", "squash", "basketball", "sport club");
            Add(EGRPlaceType.BeautySupply, "beauty supply", "beauty supplies", "makeup", "beauty shop", "cosmetic", "cosmetics");
            //HOME
            Add(EGRPlaceType.CarDealer, "car dealer", "automotive");
            Add(EGRPlaceType.CarRental, "car rental");
            Add(EGRPlaceType.Convenience, "convenience", "convenience store");
            Add(EGRPlaceType.Hotel, "hotel", "motel");
            Add(EGRPlaceType.ATM, "atm", "atms");
            Add(EGRPlaceType.Gas, "gas station");
            Add(EGRPlaceType.Hospital, "hospital", "medical center", "medical centre", "clinic", "health centre", "health center");
            Add(EGRPlaceType.CarWash, "car wash");
            Add(EGRPlaceType.Parking, "parking", "car station", "bus stop");
            Add(EGRPlaceType.BeautySalons, "beauty salon", "beauty center", "beauty centre", "spa", "hairdresser", "hair dresser", "barber", "salon");
            Add(EGRPlaceType.EVC, "electric vehicle", "charging station");

            Add(EGRPlaceType.Delivery, "delivery");

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
