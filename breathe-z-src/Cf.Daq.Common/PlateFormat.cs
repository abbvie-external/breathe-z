using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Cf.Daq.Common
{
    struct WellLocation
    {
        public int WellNumber { get; set; }
        public int Column { get; set; }
        public int Row { get; set; }
        public string RowAsAlpha { get { return (char)(64 + Row) + string.Empty; } }
        public string Address { get; set; }
    }

    static class PlateFormat
    {
        private static readonly Regex wellRegEx = new Regex(@"([a-zA-Z]{1,2})([0-9]{1,2})");

        public static readonly string PlateFrozenStorage = "Storage Plate";
        public static readonly string Plate384Well = "384-Well Plate";
        public static readonly string Plate96Well = "96-Well";
        public static readonly string Plate24Well = "24-Well";
        public static readonly string Plate6Well = "6-Well";
        public static readonly string Plate8Well = "8-Wel";
        public static readonly string Tube = "Tube";

        public static int WellCount(string format)
        {
            if (format == Plate384Well)
            {
                return 384;
            }

            if (format == Plate96Well)
            {
                return 96;
            }

            if (format == Plate24Well)
            {
                return 24;
            }

            if (format == Plate8Well)
            {
                return 8;
            }

            if (format == Plate6Well)
            {
                return 6;
            }

            if (format == Tube)
            {
                return 1;
            }

            throw new InvalidOperationException("Could not find plate format:" + format);
        }

        public static WellLocation WellAddressToWellLocation(string plateFormat, string address)
        {
            var wa = address.Trim();

            var matches = wellRegEx.Matches(wa);

            if (matches.Count != 1 || matches[0].Groups.Count != 3)
            {
                throw new InvalidOperationException("Could find row and column info in well address: " + wa);
            }

            var row = (byte)(matches[0].Groups[1].ToString()[0]) - 64;
            var col = int.Parse(matches[0].Groups[2].ToString());
            var num = (col - 1) * RowCount(plateFormat) + row;

            if (num > WellCount(plateFormat))
            {
                var msg = string.Format("Well Address {0} is out of range for {1}", address, plateFormat);
                throw new ApplicationException(msg);
            }

            var checkNoPadding = WellNumberToAddress(plateFormat, num);
            var checkPadding = WellNumberToAddress(plateFormat, num, true);

            if (checkNoPadding != address && checkPadding != address)
            {
                var msg = string.Format("Well Address {0} Yields Number of {1} for {2}", address, num, plateFormat);
                throw new ApplicationException(msg);
            }

            return new WellLocation { Row = row, Column = col, WellNumber = num, Address = checkNoPadding };
        }

        public static string WellNumberToAddress(string format, int wellNumber, bool padColumn = true)
        {
            int rows = RowCount(format);
            int columns = ColumnCount(format);

            return WellNumberToAddress(columns, rows, wellNumber, padColumn);
        }

        public static WellLocation WellNumberToWellLocation(string plateFormat, int wellNumber)
        {
            var address = WellNumberToAddress(plateFormat, wellNumber);
            return WellAddressToWellLocation(plateFormat, address);
        }

        public static int RowCount(string format)
        {
            int rows;

            if (format == Plate384Well)
            {
                rows = 16;
            }
            else if (format == Plate96Well)
            {
                rows = 8;
            }
            else if (format == Plate24Well)
            {
                rows = 4;
            }
            else if (format == Plate8Well)
            {
                rows = 8;
            }
            else if (format == Plate6Well)
            {
                rows = 2;
            }
            else if (format == Tube)
            {
                rows = 1;
            }
            else
            {
                throw new InvalidOperationException("Could not find plate format:" + format);
            }

            return rows;
        }

        public static int ColumnCount(string format)
        {
            int columns;

            if (format == Plate384Well)
            {
                columns = 24;
            }
            else if (format == Plate96Well)
            {
                columns = 12;
            }
            else if (format == Plate24Well)
            {
                columns = 6;
            }
            else if (format == Plate8Well)
            {
                columns = 1;
            }
            else if (format == Plate6Well)
            {
                columns = 3;
            }
            else if (format == Tube)
            {
                columns = 1;
            }
            else
            {
                throw new InvalidOperationException("Could not find plate format:" + format);
            }

            return columns;
        }

        public static string WellNumberToAddress(int cols, int rows, int wellNumber, bool padColumn = false)
        {
            const int starting = 64;
            var row = wellNumber % rows;
            var col = ((wellNumber - 1) / rows) + 1;

            if (row == 0)
            {
                row = rows;
            }

            var rowAddress = Encoding.ASCII.GetChars(new[] { (byte)(starting + row) })[0].ToString();
            return padColumn ? string.Format("{0}{1:00}", rowAddress, col) : rowAddress + col;
        }
    }
}
