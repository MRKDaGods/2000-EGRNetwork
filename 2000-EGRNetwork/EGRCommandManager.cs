using MRK.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using static System.Console;
using static MRK.Logger;

namespace MRK {
    public class EGRCommandManager : Behaviour {
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
            mInfo.Invoke(null, new object[2] { args, null });
        }

        static T TryGetElement<T>(T[] array, int idx) {
            return array.Length > idx ? array[idx] : default;
        }

        static bool IsStringInvalid(string s) {
            return string.IsNullOrEmpty(s) || string.IsNullOrWhiteSpace(s);
        }

        static void __cmd_exit(string[] args, Network network) {
            LogInfo("Exiting...");

            if (network != null)
                network.Stop();
            EGR.IsRunning = false;
        }

        static void __cmd_genidx(string[] args, Network network) {
            string prop = TryGetElement(args, 0);
            if (IsStringInvalid(prop)) {
                WriteLine("Invalid prop");
                return;
            }

            //types
            switch (prop) {

                case "types":
                    EGR.PlaceManager.CreateTypeIndexFolders();
                    break;

                case "places":
                    EGR.PlaceManager.CreateIndexFolders();
                    break;

                default:
                    WriteLine("Unknown prop");
                    break;

            }
        }

        static void __cmd_idxtypes(string[] args, Network network) {
            List<EGRPlace> places = EGR.PlaceManager.GetAllPlaces();
            foreach (EGRPlace place in places)
                EGR.PlaceManager.GeneratePlaceTypes(place);
        }

        static void __cmd_genchaintemp(string[] args, Network network) {
            string name = TryGetElement(args, 0);
            if (IsStringInvalid(name)) {
                WriteLine("Invalid name");
                return;
            }

            EGR.PlaceManager.GeneratePlaceChainTemplate(name);
        }

        static void __cmd_genchain(string[] args, Network network) {
            string name = TryGetElement(args, 0);
            if (IsStringInvalid(name)) {
                WriteLine("Invalid name");
                return;
            }

            EGR.PlaceManager.GeneratePlaceChainFromTemplate(name);
        }

        static void __cmd_assignchainplaces(string[] args, Network network) {
            string chainName = TryGetElement(args, 0);
            if (IsStringInvalid(chainName)) {
                WriteLine("Invalid chain name");
                return;
            }

            EGRPlaceChain chain = EGR.PlaceManager.GetChain(chainName);
            if (chain == null) {
                WriteLine("Invalid chain");
                return;
            }

            EGR.PlaceManager.AssignPlacesToChain(chain);
        }

        static void __cmd_addtiles(string[] args, Network network) {
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

            //Client.TileManager.AddTilesFromFile(path, tileset);
        }

        static void __cmd_test(string[] args, Network network) {
            //WebClient wc = new WebClient();
            //string res = wc.DownloadString(new Uri("https://api.mapbox.com/directions/v5/mapbox/driving/30.9416872837362%2C30.071121507752316%3B30.957293813885634%2C30.063245685427646?alternatives=true&geometries=geojson&steps=true&access_token=pk.eyJ1IjoiMjAwMGVneXB0IiwiYSI6ImNrbHI5dnlwZTBuNTgyb2xsOTRvdnQyN2QifQ.fOW4YjVUAE5fjwtL9Etajg"));
            //WriteLine(res);
        }

        static void __cmd_genresource(string[] args, Network network) {
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

            //bool res = EGR.CDNNetwork.ResourceManager.CreateResource(resourceName, path);
            //WriteLine("Result=" + res);
        }

        static void __cmd_gettile(string[] args, Network network) {
            string tileset = TryGetElement(args, 0);
            if (IsStringInvalid(tileset)) {
                WriteLine("Invalid tileset");
                return;
            }

            string[] subArgs = new string[4];
            for (int i = 0; i < subArgs.Length; i++) {
                string arg = TryGetElement(args, i + 1);
                if (IsStringInvalid(arg)) {
                    WriteLine($"Invalid subArg {i}");
                    return;
                }

                subArgs[i] = arg;
            }

            EGR.TileManager.GetTile(
                tileset,
                new EGRTileID {
                    Z = int.Parse(subArgs[0]),
                    X = int.Parse(subArgs[1]),
                    Y = int.Parse(subArgs[2])
                },
                subArgs[3] == "1",
                (tile) => {
                    if (tile == null) {
                        WriteLine($"Failed to get tile");
                        return;
                    }

                    WriteLine($"Got tile successfully, [{tile.ID}] size={tile.Data.Length} bytes low={tile.LowResolution}");
                }
            );
        }
    }
}
