using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cf.Daq.Common
{
    public class TeccWellMapper
    {
        private Dictionary<string, Dictionary<int, TeccWellMap>> teccMaps;

        public IList<string> PlateFormats { get { return new[] {PlateFormat.Plate96Well, PlateFormat.Plate24Well}; } }

        public int GetWellCount(string plateFormat)
        {
            return PlateFormat.WellCount(plateFormat);
        }

        public int GetRowCount(string plateFormat)
        {
            return PlateFormat.RowCount(plateFormat);
        }
        public int GetColCount(string plateFormat)
        {
            return PlateFormat.ColumnCount(plateFormat);
        }

        public IList<TeccWellMap> GetMaps(string plateFormat)
        {
            Dictionary<int, TeccWellMap> map;

            if (!teccMaps.TryGetValue(plateFormat, out map))
            {
                throw new KeyNotFoundException("Could not find map for plateformat:" + plateFormat);
            }

            return map.Select(t=>t.Value).ToList();
        }

        public TeccWellMap GetMap(string plateFormat, int teccNumber)
        {
            Dictionary<int, TeccWellMap> map;

            if (!teccMaps.TryGetValue(plateFormat, out map))
            {
                throw new KeyNotFoundException("Could not find map for plateformat:" + plateFormat);
            }

            TeccWellMap mappedWell;

            if (!map.TryGetValue(teccNumber, out mappedWell))
            {
                throw new KeyNotFoundException("Could not find map for well: " + teccNumber);
            }

            return mappedWell;
        }

        public TeccWellMapper()
        {
            BuildTeccMaps();
        }

        private void BuildTeccMaps()
        {
            var map24 = new Dictionary<int, TeccWellMap>(24);

            for (int w = 0; w < 24; w++)
            {
                var loc = PlateFormat.WellNumberToWellLocation(PlateFormat.Plate24Well, w + 1);

                var teccMap = new TeccWellMap
                {
                    TeccNumber = w + 1,
                    WellRow = loc.RowAsAlpha,
                    WellColumn = loc.Column,
                    WellNumber = loc.WellNumber,
                    WellAddress = loc.Address,
                    WellRowNumeric = loc.Row
                };

                map24[teccMap.TeccNumber] = teccMap;
            }

            var map96 = new Dictionary<int, TeccWellMap>(96);

            for (int w = 0; w < 96; w++)
            {

                var loc = PlateFormat.WellNumberToWellLocation(PlateFormat.Plate96Well, w + 1);

                var teccMap = new TeccWellMap
                              {
                                  TeccNumber = w + 1,
                                  WellRow = loc.RowAsAlpha,
                                  WellColumn = loc.Column,
                                  WellNumber = loc.WellNumber,
                                  WellAddress = loc.Address,
                                  WellRowNumeric = loc.Row
                              };

                map96[teccMap.TeccNumber] = teccMap;
            }

            teccMaps = new Dictionary<string, Dictionary<int, TeccWellMap>>
                       {
                           {PlateFormat.Plate24Well, map24},
                           {PlateFormat.Plate96Well, map96},
                       };
        }
    }
}
