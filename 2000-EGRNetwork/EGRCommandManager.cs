using MRK.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using static System.Console;
using static MRK.EGRLogger;

namespace MRK {
    public class EGRCommandManager : EGRBase {
        public static void Execute(string cmd) {
            string[] cmdline = cmd.Trim(' ', '\t').Split(' ');
            if (cmdline.Length == 0 || cmdline[0].Length == 0)
                return;

            MethodInfo mInfo = typeof(EGRCommandManager).GetMethod($"__cmd_{cmdline[0]}", BindingFlags.NonPublic | BindingFlags.Static);
            if (mInfo == null) {
                WriteLine($"Command '{cmdline[0]}' not found");
                return;
            }

            string[] args = new string[cmdline.Length - 1];
            Array.Copy(cmdline, 1, args, 0, args.Length);
            mInfo.Invoke(null, new object[2] { args, EGRMain.Instance.MainNetwork });
        }

        static T TryGetElement<T>(T[] array, int idx) {
            return array.Length > idx ? array[idx] : default;
        }

        static bool IsStringInvalid(string s) {
            return string.IsNullOrEmpty(s) || string.IsNullOrWhiteSpace(s);
        }

        static void __cmd_exit(string[] args, EGRNetwork network) {
            LogInfo("Exiting...");

            network.Stop();
            Client.IsRunning = false;
        }

        static void __cmd_genidx(string[] args, EGRNetwork network) {
            string prop = TryGetElement(args, 0);
            if (IsStringInvalid(prop)) {
                WriteLine("Invalid prop");
                return;
            }

            //types
            switch (prop) {

                case "types":
                    Client.PlaceManager.CreateTypeIndexFolders();
                    break;

                case "places":
                    Client.PlaceManager.CreateIndexFolders();
                    break;

                default:
                    WriteLine("Unknown prop");
                    break;

            }
        }

        static void __cmd_idxtypes(string[] args, EGRNetwork network) {
            List<EGRPlace> places = Client.PlaceManager.GetAllPlaces();
            foreach (EGRPlace place in places)
                Client.PlaceManager.GeneratePlaceTypes(place);
        }

        static void __cmd_genchaintemp(string[] args, EGRNetwork network) {
            string name = TryGetElement(args, 0);
            if (IsStringInvalid(name)) {
                WriteLine("Invalid name");
                return;
            }

            Client.PlaceManager.GeneratePlaceChainTemplate(name);
        }

        static void __cmd_genchain(string[] args, EGRNetwork network) {
            string name = TryGetElement(args, 0);
            if (IsStringInvalid(name)) {
                WriteLine("Invalid name");
                return;
            }

            Client.PlaceManager.GeneratePlaceChainFromTemplate(name);
        }

        static void __cmd_assignchainplaces(string[] args, EGRNetwork network) {
            string chainName = TryGetElement(args, 0);
            if (IsStringInvalid(chainName)) {
                WriteLine("Invalid chain name");
                return;
            }

            EGRPlaceChain chain = Client.PlaceManager.GetChain(chainName);
            if (chain == null) {
                WriteLine("Invalid chain");
                return;
            }

            Client.PlaceManager.AssignPlacesToChain(chain);
        }

        static void __cmd_addtiles(string[] args, EGRNetwork network) {
            string tileset = TryGetElement(args, 0);
            if (IsStringInvalid(tileset)) {
                WriteLine("Invalid tileset");
                return;
            }

            string path = TryGetElement(args, 1);
            if (IsStringInvalid(path)) {
                WriteLine("Invalid path");
                return;
            }

            Client.TileManager.AddTilesFromFile(path, tileset);
        }

        static void __cmd_test(string[] args, EGRNetwork network) {
            WebClient wc = new WebClient();
            string res = wc.DownloadString(new Uri("https://api.mapbox.com/directions/v5/mapbox/driving/30.9416872837362%2C30.071121507752316%3B30.957293813885634%2C30.063245685427646?alternatives=true&geometries=geojson&steps=true&access_token=pk.eyJ1IjoiMjAwMGVneXB0IiwiYSI6ImNrbHI5dnlwZTBuNTgyb2xsOTRvdnQyN2QifQ.fOW4YjVUAE5fjwtL9Etajg"));
            WriteLine(res);
        }

        static void __cmd_genresource(string[] args, EGRNetwork network) {
            string resourceName = TryGetElement(args, 0);
            if (IsStringInvalid(resourceName)) {
                WriteLine("Invalid resource name");
                return;
            }

            string path = TryGetElement(args, 1);
            if (IsStringInvalid(path)) {
                WriteLine("Invalid path");
                return;
            }

            bool res = Client.CDNNetwork.ResourceManager.CreateResource(resourceName, path);
            WriteLine("Result=" + res);
        }
    }
}
