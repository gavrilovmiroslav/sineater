namespace SINEATER.Localization
{
    internal class LocalizedString
    {
        public LocalizedString(LocaIDs id) 
        {
            _id = id;
        }

        private LocaIDs _id;
        string? _cached = null;

        public override string ToString()
        {
            return _cached != null ? _cached : _cached = Loca.GetString(_id);
        }
    }
}
