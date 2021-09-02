using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using static MRK.EGRLogger;

namespace MRK.WTE {
    public class EGRWTE {
        string m_DatabasePath;
        Context m_CurrentWTEContext;
        List<float> m_PriceMap;

        public EGRWTE(string dbPath) {
            LogInfo($"Initializing WTE");

            m_DatabasePath = dbPath;
            Load();
        }

        void Load() {
            LogInfo($"Loading WTE, dbPath={m_DatabasePath}");

            if (!File.Exists(m_DatabasePath)) {
                goto __fail;
            }

            try {
                using (FileStream fstream = new(m_DatabasePath, FileMode.Open))
                using (BinaryReader reader = new(fstream)) {
                    m_CurrentWTEContext = new Context();
                    m_CurrentWTEContext.BinaryRead(reader);
                    reader.Close();
                }
            }
            catch (Exception ex) {
                LogError($"Failed to read WTE database ({ex})");
                goto __fail;
            }

            LogInfo($"Loaded WTE database, placeCount={m_CurrentWTEContext.Places.Count}");

            string rawPriceMap = EGRMain.Instance.Config["NET_WTE_PRICE_MAP"].String;
            LogInfo($"WTE raw price map={rawPriceMap}");
            m_PriceMap = MRKParser.ParseArray(rawPriceMap);
            LogInfo($"WTE parsed price map={m_PriceMap.StringifyList()}");
            return;

        __fail:
            LogError("Failed to load WTE database");
        }

        public HashSet<Place> Query(byte people, int price, string cuisine) {
            IEnumerable<Place> cuisineQualified = m_CurrentWTEContext.Places.Where(place => {
                if (cuisine == "Any")
                    return true;

                Category cat = place.Categories.Find(cat => cat.Type == CategoryType.PlaceTags);
                if (cat == null)
                    return false;

                foreach (ICategoryChild categoryChild in cat.Children) {
                    PlaceTag pt = (PlaceTag)categoryChild;

                    if (pt.Type == PlaceTagType.Custom) {
                        if (pt.CustomType == cuisine) {
                            return true;
                        }
                    }
                    else if (pt.Type.ToString() == cuisine) {
                        return true;
                    }
                }

                return false;
            });

            float pricePerPerson = m_PriceMap[price] / people;
            return cuisineQualified.Where(place => {
                Category cat = place.Categories.Find(cat => cat.Type == CategoryType.PlacePricing);
                if (cat == null)
                    return false;

                foreach (ICategoryChild categoryChild in cat.Children) {
                    PlacePricing pp = (PlacePricing)categoryChild;
                    if (pp.Type == PlacePricingType.GeneralMinimum) {
                        if (pp.Value <= pricePerPerson) {
                            return true;
                        }
                    }
                }

                return false;
            }).ToHashSet();
        }

        public HashSet<WTEProxyPlace> ProxifyQuery(HashSet<Place> places) {
            if (places == null || places.Count == 0)
                return null;

            HashSet<WTEProxyPlace> proxyPlaces = new();
            foreach (Place p in places) {
                WTEProxyPlace proxyPlace = new();
                proxyPlace.Name = p.Name;
                proxyPlace.IconResource = $"ico_place_{p.CID}";
                proxyPlace.Tags = new List<string>();

                Category tagCategory = p.Categories.Find(c => c.Type == CategoryType.PlaceTags);
                if (tagCategory != null) {
                    foreach (ICategoryChild categoryChild in tagCategory.Children) {
                        PlaceTag pt = (PlaceTag)categoryChild;
                        
                        if (pt.Type == PlaceTagType.Custom) {
                            proxyPlace.Tags.Add(pt.CustomType);
                            continue;
                        }

                        proxyPlace.Tags.Add(pt.Type.ToString());
                    }
                }

                Category pricingCategory = p.Categories.Find(c => c.Type == CategoryType.PlacePricing);
                //check if place has pricing data
                if (pricingCategory != null) {
                    foreach (ICategoryChild categoryChild in pricingCategory.Children) {
                        PlacePricing pp = (PlacePricing)categoryChild;
                        switch (pp.Type) {
                            case PlacePricingType.GeneralMinimum:
                                proxyPlace.GeneralMinimum = pp.Value;
                                break;

                            case PlacePricingType.GeneralMaximum:
                                proxyPlace.GeneralMaximum = pp.Value;
                                break;
                        }
                    }
                }

                proxyPlaces.Add(proxyPlace);
            }

            return proxyPlaces;
        }
    }
}
