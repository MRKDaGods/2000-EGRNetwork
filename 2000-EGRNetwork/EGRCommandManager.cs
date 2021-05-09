using MRK.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Console;

namespace MRK {
    public class EGRCommandManager {
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
            mInfo.Invoke(null, new object[2] { args, EGRNetwork.Instance });
        }

        static T TryGetElement<T>(T[] array, int idx) {
            return array.Length > idx ? array[idx] : default;
        }

        static bool IsStringInvalid(string s) {
            return string.IsNullOrEmpty(s) || string.IsNullOrWhiteSpace(s);
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
                    network.PlaceManager.CreateTypeIndexFolders();
                    break;

                case "places":
                    network.PlaceManager.CreateIndexFolders();
                    break;

                default:
                    WriteLine("Unknown prop");
                    break;

            }
        }

        static void __cmd_idxtypes(string[] args, EGRNetwork network) {
            List<EGRPlace> places = network.PlaceManager.GetAllPlaces();
            foreach (EGRPlace place in places)
                network.PlaceManager.GeneratePlaceTypes(place);
        }

        static void __cmd_genchaintemp(string[] args, EGRNetwork network) {
            string name = TryGetElement(args, 0);
            if (IsStringInvalid(name)) {
                WriteLine("Invalid name");
                return;
            }

            network.PlaceManager.GeneratePlaceChainTemplate(name);
        }

        static void __cmd_genchain(string[] args, EGRNetwork network) {
            string name = TryGetElement(args, 0);
            if (IsStringInvalid(name)) {
                WriteLine("Invalid name");
                return;
            }

            network.PlaceManager.GeneratePlaceChainFromTemplate(name);
        }

        static void __cmd_assignchainplaces(string[] args, EGRNetwork network) {
            string chainName = TryGetElement(args, 0);
            if (IsStringInvalid(chainName)) {
                WriteLine("Invalid chain name");
                return;
            }

            EGRPlaceChain chain = network.PlaceManager.GetChain(chainName);
            if (chain == null) {
                WriteLine("Invalid chain");
                return;
            }

            network.PlaceManager.AssignPlacesToChain(chain);
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

            network.TileManager.AddTilesFromFile(path, tileset);
        }
    }
}
