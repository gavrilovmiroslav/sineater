using Newtonsoft.Json;
using System;

namespace SINEATER.Localization
{
    [JsonConverter(typeof(LocalizedStringConverter))]
    public class LocalizedString
    {
        public LocalizedString(string s)
        {
            _tmp = "#" + s;
            _cached = _tmp;
        }
        public LocalizedString(LocaIDs id) 
        {
            _id = id;
            Loca.LocalizationChanged += OnLocaChanged;
        }

        public LocaIDs ID => _id;

        private LocaIDs _id;
        private string _tmp = "";
        string? _cached = null;

        public override string ToString()
        {
            return _cached != null ? _cached : _cached = Loca.GetString(_id);
        }

        private void OnLocaChanged(object? sender, EventArgs e)
        {
            _cached = null;
        }

        ~LocalizedString()
        {
            Loca.LocalizationChanged -= OnLocaChanged;
        }
    }
}
