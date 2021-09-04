namespace MRK {
    public interface IEGRRemoteTileProvider {
        public string GetTileRequest(string tileset, EGRTileID tileID, bool low);
    }
}
