using System;
using System.Collections.Generic;
using System.Data.Entity.Core.EntityClient;
using System.Linq;
using System.Text;
using AutomationEngineering.CommonTools.ExtensionMethods;
using BreatheModel;
using Devart.Data.Oracle;
using Devart.Data.Oracle.Entity.Configuration;

namespace BreatheCalc.DataAccess
{
    public class BreatheCrud
    {
        private static OracleMonitor mon;

        public BreatheCrud()
        {
            OracleEntityProviderConfig config = OracleEntityProviderConfig.Instance;
            config.DmlOptions.BatchUpdates.Enabled = true;
            config.DmlOptions.BatchUpdates.BatchSize = 1000;

            if (mon == null)
            {
                mon = new OracleMonitor { IsActive = true };
            }
        }

        public void ReplaceExperiment(Experiment exp, IList<Calculation> calcs, IList<ResultValue> resultValues, IList<Location> locations, IList<Measurement> measurements ) 
        {
            using (var context = new BreatheEntities())
            {
                var existing = context.Experiments.SingleOrDefault(e => e.FileName == exp.FileName);

                var connection = (context.Connection as EntityConnection).StoreConnection as OracleConnection;

                context.Connection.Open();

                using(var trans = context.Connection.BeginTransaction())
                {
                    if (existing != null)
                    {
                        context.Experiments.DeleteObject(existing);
                    }

                    context.Experiments.AddObject(exp);

                    InsertMeasurements(connection, measurements);
                    InsertLocations(connection, locations);
                    InsertCalculation(connection, calcs);
                    InsertResultValues(connection, resultValues);
                    context.SaveChanges();

                    trans.Commit();
                }
            }
        }


        public static void InsertLocations(OracleConnection cn, IList<Location> locations)
        {
            var sql = 
                "INSERT INTO LOCATION (LOCATION_ID, EXPERIMENT_ID,PLATE_POSITION, WELL_ADDRESS, WELL_ROW, WELL_COL, CURVE_REF, PRIMARY_LOT, PRIMARY_COMPOUND, PRIMARY_CONC, VALID) " +
                "VALUES (:LOCATION_ID, :EXPERIMENT_ID, :PLATE_POSITION, :WELL_ADDRESS, :WELL_ROW, :WELL_COL, :CURVE_REF, :PRIMARY_LOT, :PRIMARY_COMPOUND, :PRIMARY_CONC, :VALID)";

            var n = locations.Count;

            var locationIds = new Guid[n];
            var experimentIds = new Guid[n];
            var platePositions = new int[n];
            var wellAddresses = new string[n];
            var rows = new string[n];
            var cols = new string[n];
            var curveRefs = new string[n];
            var primaryLots = new decimal?[n];
            var primaryCompounds = new string[n];
            var primaryConc = new Decimal?[n];
            var isValid = new int[n];

            for (int i = 0; i < n; i++)
            {
                var location = locations[i];
                
                locationIds[i] = location.LocationId;
                experimentIds[i] = location.ExperimentId;
                platePositions[i] = location.PlatePosition;
                wellAddresses[i] = location.WellAddress;
                rows[i] = location.WellRow;
                cols[i] = location.WellCol;
                curveRefs[i] = location.CurveRef;
                primaryLots[i] = location.PrimaryLot;
                primaryCompounds[i] = location.PrimaryCompound;
                primaryConc[i] = location.PrimaryConc;
                isValid[i] = location.Valid ? 1 : 0;
            }

            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(new OracleParameter("LOCATION_ID", OracleDbType.Raw, 16)).Value = locationIds;
                cmd.Parameters.Add(new OracleParameter("EXPERIMENT_ID", OracleDbType.Raw, 16)).Value = experimentIds;
                cmd.Parameters.Add(new OracleParameter("PLATE_POSITION", OracleDbType.Number)).Value = platePositions;
                cmd.Parameters.Add(new OracleParameter("WELL_ADDRESS", OracleDbType.VarChar)).Value = wellAddresses;
                cmd.Parameters.Add(new OracleParameter("WELL_ROW", OracleDbType.VarChar)).Value = rows;
                cmd.Parameters.Add(new OracleParameter("WELL_COL", OracleDbType.VarChar)).Value = cols;
                cmd.Parameters.Add(new OracleParameter("CURVE_REF", OracleDbType.VarChar)).Value = curveRefs;
                cmd.Parameters.Add(new OracleParameter("PRIMARY_LOT", OracleDbType.Number)).Value = primaryLots;
                cmd.Parameters.Add(new OracleParameter("PRIMARY_COMPOUND", OracleDbType.VarChar)).Value = primaryCompounds;
                cmd.Parameters.Add(new OracleParameter("PRIMARY_CONC", OracleDbType.Number)).Value = primaryConc;
                cmd.Parameters.Add(new OracleParameter("VALID", OracleDbType.Number,1)).Value = isValid;

                cmd.ExecuteArray(n);
            }
        }

        public static void InsertCalculation(OracleConnection cn, IList<Calculation> calculations)
        {
            var sql = "INSERT INTO CALCULATION (LOCATION_ID, NAME, \"Value\") VALUES (:LOCATION_ID, :NAME, :CALC_VALUE)";

            var groups = calculations.BreakIntoGroupsOfSize(30000);

            foreach (var resultGroup in groups)
            {
                var items = resultGroup.ToList();

                var n = items.Count;

                var locationIds = new Guid[n];
                var names = new string[n];
                var values = new double?[n];

                for (int i = 0; i < n; i++)
                {
                    var calc = items[i];
                    locationIds[i] = calc.LocationId;
                    names[i] = calc.Name;
                    values[i] = calc.Value;
                }

                using (var cmd = new OracleCommand(sql, cn))
                {
                    cmd.Parameters.Add(new OracleParameter("LOCATION_ID", OracleDbType.Raw, 16)).Value = locationIds;
                    cmd.Parameters.Add(new OracleParameter("NAME", OracleDbType.VarChar)).Value = names;
                    cmd.Parameters.Add(new OracleParameter("CALC_VALUE", OracleDbType.Double)).Value = values;

                    cmd.ExecuteArray(n);
                }
            }
        }

        public static void InsertResultValues(OracleConnection cn, IList<ResultValue> resultValues)
        {
            var sql = "INSERT INTO RESULT_VALUE (MEASUREMENT_ID, NAME, Value) VALUES (:MEASUREMENT_ID, :NAME, :CALC_VALUE)";

            var groups = resultValues.BreakIntoGroupsOfSize(30000);

            foreach (var resultGroup in groups)
            {
                var items = resultGroup.ToList();
                var n = items.Count;

                var measurementIds = new Guid[n];
                var names = new string[n];
                var values = new double?[n];

                for (int i = 0; i < n; i++)
                {
                    var calc = items[i];
                    measurementIds[i] = calc.MeasurementId;
                    names[i] = calc.Name;
                    values[i] = calc.Value;
                }

                using (var cmd = new OracleCommand(sql, cn))
                {
                    cmd.Parameters.Add(new OracleParameter("MEASUREMENT_ID", OracleDbType.Raw, 16)).Value = measurementIds;
                    cmd.Parameters.Add(new OracleParameter("NAME", OracleDbType.VarChar)).Value = names;
                    cmd.Parameters.Add(new OracleParameter("CALC_VALUE", OracleDbType.Double)).Value = values;

                    cmd.ExecuteArray(n);
                }
            }
        }

        public static void InsertMeasurements(OracleConnection cn, IList<Measurement> measurements)
        {
            var sql = 
                "INSERT INTO MEASUREMENT (MEASUREMENT_ID, LOCATION_ID, RESULT_INDEX, STAGE_NAME, ELAPSED_TIME_IN_MIN) " +
                "VALUES (:MEASUREMENT_ID, :LOCATION_ID, :RESULT_INDEX, :STAGE_NAME, :ELAPSED_TIME_IN_MIN)";

            var n = measurements.Count;

            var measurementIds = new Guid[n];
            var locationIds = new Guid[n];
            var resultIndex = new long[n];
            var stageName = new string[n];
            var elapsed = new double[n];
           
            for (int i = 0; i < n; i++)
            {
                var measurement = measurements[i];
                measurementIds[i] = measurement.MeasurementId;
                locationIds[i] = measurement.LocationId;
                resultIndex[i] = measurement.ResultIndex;
                stageName[i] = measurement.StageName;
                elapsed[i] = measurement.ElapsedTimeInMin;
            }

            using (var cmd = new OracleCommand(sql, cn))
            {
                cmd.Parameters.Add(new OracleParameter("MEASUREMENT_ID", OracleDbType.Raw, 16)).Value = measurementIds;
                cmd.Parameters.Add(new OracleParameter("LOCATION_ID", OracleDbType.Raw, 16)).Value = locationIds;
                cmd.Parameters.Add(new OracleParameter("RESULT_INDEX", OracleDbType.Number)).Value = resultIndex;
                cmd.Parameters.Add(new OracleParameter("STAGE_NAME", OracleDbType.VarChar)).Value = stageName;
                cmd.Parameters.Add(new OracleParameter("ELAPSED_TIME_IN_MIN", OracleDbType.Double)).Value = elapsed;
                cmd.ExecuteArray(n);
            }
        }

    }
}
