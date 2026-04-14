using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Devart.Data.Oracle;
using Devart.Data.Oracle.Entity.Configuration;
using PlatePreps;

namespace BreatheCalc.DataAccess
{
    public class PlatePrepsReader
    {
         private static OracleMonitor mon;

         public PlatePrepsReader()
        {
            OracleEntityProviderConfig config = OracleEntityProviderConfig.Instance;
            config.DmlOptions.BatchUpdates.Enabled = true;
            config.DmlOptions.BatchUpdates.BatchSize = 1000;

            if (mon == null)
            {
                mon = new OracleMonitor { IsActive = true };
            }
        }

        public AssayPlate LoadAssayPlate(string barcode) 
        {
            using (var context = new PlatePrepsEntities())
            {
                var existing = context
                    .AssayPlates
                    .Include("AssayWells")
                    .SingleOrDefault(ap=>ap.PlateId == barcode);

                return existing;
            }
        }
    }
}
