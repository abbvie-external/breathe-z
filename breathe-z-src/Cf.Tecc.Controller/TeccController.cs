using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using AutomationEngineering.LoggingHelper;
using AutomationEngineering.CommonTools.ExtensionMethods;
using Cf.Daq.Common;
using System.Diagnostics;
using log4net;

namespace Cf.Tecc.Controller
{
    public class TeccController
    {
        #region Imports
        [DllImport("MTUtilAPI.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public extern static int MTUtil_ScanBus(out IntPtr serials);

        [DllImport("MTUtilAPI.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public extern static int MTUtil_init(int serial, IntPtr appAhHardwares);

        [DllImport("MTUtilAPI.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public extern static int MTUtil_WaitForTeccIdle(int tecc, int timeOutMs);

        [DllImport("MTUtilAPI.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public extern static int MTUtil_SetClampfilter(int tecc, int state);

        [DllImport("MTUtilAPI.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public extern static int MTUtil_SetIVFilter(int tecc, int state);

        [DllImport("MTUtilAPI.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public extern static int MTUtil_SetOscTh(int tecc, int threshold);

        [DllImport("MTUtilAPI.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public extern static int MTUtil_RecordVtRt(int tecc);

        [DllImport("MTUtilAPI.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public extern static int MTUtil_RecordVt(int tecc);
               
        [DllImport("MTUtilAPI.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public extern static int MTUtil_ReadRt(int tecc, float[] resistance);

        [DllImport("MTUtilAPI.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public extern static int MTUtil_ReadVt1(int tecc, float[] voltage);

        [DllImport("MTUtilAPI.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public extern static int MTUtil_ReadVt2(int tecc, float[] voltage);

        [DllImport("MTUtilAPI.dll",SetLastError = true,CallingConvention = CallingConvention.Cdecl)]
        public  extern static IntPtr MTUtil_GetI2cErrorString(int error);

        [DllImport("MTUtilAPI.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public extern static int MTUtil_SetInitialRap(int tecc, float rap);
        
        [DllImport("MTUtilAPI.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public extern static int MTUtil_SetRtxTable(int tecc, float[] rt, byte[] inampGain, float[] iogv, float[] ipp, byte[] ivgain);

        [DllImport("MTUtilAPI.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public extern static int MTUtil_GetErrorStatus(int tecc, byte[] status);

        [DllImport("MTUtilAPI.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public extern static int MTUtil_GetRtx(int tecc, byte[] status);

        [DllImport("MTUtilAPI.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public extern static int MTUtil_ShortVElectrodes(int tecc);

        [DllImport("MTUtilAPI.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public extern static int MTUtil_SetMuxChannel(int tecc, byte channel);
                
        #endregion

        public bool DebugMode { get; set; }
        public byte ExpectedBoardCount { get; set; }
        public float Area { get; private set; }

        public byte RealBoardCount { get; set; }
        public string TeccConfigString { get; set; }

        public string CurrentPlateFormat { get; set; }

        public List<RtxSetting> ValidRtxSettings { get; private set; }

        public TeccWellMapper TeccWellMapper = new TeccWellMapper();

        private static readonly ILog log = LogCreator.ClassLogger();
        
        [FixedAddressValueType]
        private ActiveHardware hardware;
        
        public unsafe ActiveHardware Initialize(int timeOutMs, byte expectedBoardCount, string plateFormat)
        {
            log.Info("Initializing TECC");

            ExpectedBoardCount = expectedBoardCount;
            CurrentPlateFormat = plateFormat;

            if (DebugMode)
            {
                hardware.DeviceCount = expectedBoardCount;
                Task.Delay(2000).Wait();

                log.Info("Dummy TECC initialization complete");

                return new ActiveHardware {DeviceCount= ExpectedBoardCount,Serial = 1 };
            }
            
            bool inited = false;

            for (int i = 0; i < 5; i++)
            {
                IntPtr deviceInfoPtr;

                int devCount = MTUtil_ScanBus(out deviceInfoPtr);

                if (devCount == 0)
                {
                    log.Error("No TECC devices found");
                    continue;
                }

                if (devCount > 1)
                {
                    log.Error("There is more than a single TECC on the bus which is not allowed for this DLL");
                    continue;
                }

                log.Info("1 TECC Found, Attempting Initialization");

                var id = Marshal.ReadInt32(deviceInfoPtr, 0);

                fixed (ActiveHardware* hwAddress = &hardware)
                {
                    var hwPtr = new IntPtr(hwAddress);

                    var ret = MTUtil_init(id, hwPtr);

                    if (ret <= 0)
                    {
                        log.Error($"TECC Initialization failed on MTUtil_init() attempt {i} with code {ret}");
                        Task.Delay(500).Wait();
                    }
                    else
                    {
                        inited = true;
                        log.Info("TECC initialization started");
                        break;
                    }
                }
            }
                 
            if (!inited)
            {
                throw new TeccException("TECC Initialization failed");
            }
                        
            WaitForTecc(timeOutMs);

            RealBoardCount = hardware.DeviceCount;
            
            log.InfoFormat("TECC serial number {0} initialized with {1} devices", hardware.Serial, RealBoardCount);
             
            //see a weird issue where TECC indicates it is done,but it isn't
            Task.Delay(2000).Wait();
            WaitForTecc(30000);
            
            return hardware;
        }

        public void WaitForTecc(int timeOutMs)
        {
            log.Info("Starting TECC Wait");

            const int longWait = 29000;

            var waits = new List<string>();

            var tmr = Stopwatch.StartNew();

            var firstLongWait = -1;

            try
            {
                for (int i = 0; i < RealBoardCount; i++)
                {

                    log.Debug("Wait for tecc started: " + 1);
                    var waitResult = MTUtil_WaitForTeccIdle(i, timeOutMs);
                    log.Debug("Wait for tecc done: " + 1);

                    if (waitResult < 0)
                    {
                        var timeOutMsg = string.Format("TECC #{0} timed out after {1} ms", i, timeOutMs);
                        log.Error(timeOutMsg);
                        throw new TeccException(timeOutMsg);
                    }

                    var waitedOnMessage = string.Format(
                        "Waited on TECC #{0} for {1} s",
                        i + 1, tmr.ElapsedMilliseconds / 1000.0);

                    if (tmr.ElapsedMilliseconds > longWait && firstLongWait == -1)
                    {
                        firstLongWait = i + 1;
                    }

                    waits.Add(waitedOnMessage);
                }
            }
            //catch and log error here to make 100% sure the debug stuff gets flushed
            //to the logger
            catch (Exception ex)
            {
                log.Error("Error waiting for TECC", ex);
                throw;
            }

            if (tmr.ElapsedMilliseconds > longWait)
            {
                var msg = waits.ToDelimitedList("\n");
                log.InfoFormat(
                    "Wait {0}s for TECC was High, First Long Wait on TECC #{1}: \n" + msg,
                    tmr.ElapsedMilliseconds/1000.0, firstLongWait);
            }
        }

        public void Configure(TeccFilterState clampFilterState, TeccFilterState ivFilterState, int oscThreshold, float rapInitial, float area, List<RtxSetting> rtx, byte monitorChannel)
        {
            log.Info("Configuring TECC");

            Area = area;

            ValidRtxSettings = rtx.Where(r => r.Enabled && r.AllValid).OrderBy(r=>r.RtxIndex).ToList();

            var rtxSettings = ValidRtxSettings.ToDelimitedList(r =>
                    string.Format(
                    "({0},{1},{2},{3},{4})", r.ThresholdResistance, r.InAmpGain, r.CurrentOutputGain,
                        r.StimulusAmplitude, r.CurrentToVoltageGain), ", ");

            TeccConfigString =
                string.Format("Clamp Filter={0}, IV Filter={1}, Osc Threshold={2}, RAP={3}, Area={4}, RTX={5}",
                    clampFilterState, ivFilterState, oscThreshold, rapInitial, area, rtxSettings);

            if (DebugMode)
            {
                Task.Delay(2000).Wait();
                return;
            }

            var oldMode = GCSettings.LatencyMode;

            RuntimeHelpers.PrepareConstrainedRegions();

            try
            {
                GCSettings.LatencyMode = GCLatencyMode.LowLatency;

                var rt = new float[16];
                var inampGain = new byte[16];
                var iogv = new float[16];
                var ipp = new float[16];
                var ivGain = new byte[16];

                int element = 0;
                foreach (var rtxSetting in ValidRtxSettings)
                {
                    rt[element] = rtxSetting.ThresholdResistance;
                    inampGain[element] = rtxSetting.InAmpGain;
                    iogv[element] = rtxSetting.CurrentOutputGain * 1000;
                    ipp[element] = rtxSetting.StimulusAmplitude;
                    ivGain[element] = rtxSetting.CurrentToVoltageGain;
                    element++;
                }

                using (log.GetLoggerTimer("Configure TECC"))
                {
                    for (int i = 0; i < RealBoardCount; i++)
                    {
                        var teccNum = " on TECC #" + (i + 1) + " (1 Based)";
                        ThrowErrorOnNonZero(MTUtil_SetClampfilter(i, (int)clampFilterState), "MTUtil_SetClampfilter:" + teccNum);
                        ThrowErrorOnNonZero(MTUtil_SetIVFilter(i, (int)ivFilterState), "MTUtil_SetIVFilter:" + teccNum);
                        ThrowErrorOnNonZero(MTUtil_SetOscTh(i, oscThreshold), "MTUtil_SetOscTh:" + teccNum);
                        ThrowErrorOnNonZero(MTUtil_SetInitialRap(i, rapInitial), "MTUtil_SetInitialRap:" + teccNum);
                        ThrowErrorOnNonZero(MTUtil_SetRtxTable(i, rt, inampGain, iogv, ipp, ivGain), "MTUtil_SetRtxTable:" + teccNum);
                        ThrowErrorOnNonZero(MTUtil_SetMuxChannel(i, monitorChannel), "MTUtil_SetMuxChannel:" + teccNum);
                    }
                }
            }
            finally
            {
                GCSettings.LatencyMode = oldMode;
            }
        }

        public List<ChannelValue> RtRecordAndRead(int timeOutMs, int? debugTimeS, bool shortElectrodes, int debugReadIndex)
        {
            var rand = new Random();

            var debugging = (DebugMode);

            try
            {
                using (log.GetLoggerTimer("RtRecordAndRead"))
                {
                    var channels = new List<ChannelValue>();

                    var r = new float[4];
                    var v1 = new float[4];
                    var v2 = new float[4];
                    var statusAll = new byte[4];
                    var rtxInfo = new byte[8];

                    if (debugging)
                    {
                        var delay = debugTimeS ?? 30;
                        Task.Delay(delay * 1000).Wait();
                    }
                    else
                    {
                        for (int i = 0; i < RealBoardCount; i++)
                        {
                            var teccNum = " on TECC #" + (i + 1) + " (1 Based)";
                            ThrowErrorOnNonZero(MTUtil_RecordVtRt(i), "MTUtil_RecordVtRt" + teccNum);
                        }

                        log.Info("Setup all TECCs for read");

                        Task.Delay(200).Wait();

                        WaitForTecc(timeOutMs);
                    }

                    int wellIndex = 1;
                    for (int i = 0; i < hardware.DeviceCount; i++)
                    {
                        if (debugging)
                        {
                            rtxInfo[0] = 1;
                            rtxInfo[1] = (rand.NextDouble() < 0.051) ? (byte)2 : (byte)1;

                            for (int j = 0; j < 4; j++)
                            {
                                float rExtra = 0;
                                float pExtra = 0;

                                if ((wellIndex % 57 == 0))
                                {
                                    rExtra = -1 * debugReadIndex * 20;
                                }
                                else if ((wellIndex % 21 == 0))
                                {
                                    rExtra = -1 * debugReadIndex * 15;
                                }
                                else if ((wellIndex % 7 == 0))
                                {
                                    rExtra = -1 * debugReadIndex * 10;
                                }

                                if ((wellIndex % 59 == 0))
                                {
                                    pExtra = debugReadIndex*2;
                                }
                                else if ((wellIndex % 18 == 0))
                                {
                                    pExtra = debugReadIndex * 1.5f;
                                }
                                else if ((wellIndex % 9 == 0))
                                {
                                    pExtra = debugReadIndex;
                                }
                                
                                r[j] = (float)(rExtra + 500 + 20 + rand.NextDouble() * 10 - 5);
                                v1[j] = (float)(5 + pExtra + rand.NextDouble() * 1 - 0.5);
                                v2[j] = (float)(5 + pExtra + rand.NextDouble() * 1 - 0.5);

                                for (int b = 0; b < 4; b++)
                                {
                                    if (rand.NextDouble() < 0.0025)
                                    {
                                        statusAll[j] |= (byte)Math.Pow(2, b);
                                    }
                                }

                                wellIndex++;
                            }
                        }
                        else
                        {
                            var teccNum = " on TECC #" + (i + 1) + " (1 Based)";
                            ThrowErrorOnNonZero(MTUtil_ReadRt(i, r), " MTUtil_ReadRt" + teccNum);
                            ThrowErrorOnNonZero(MTUtil_ReadVt1(i, v1), "MTUtil_ReadVt1" + teccNum);
                            ThrowErrorOnNonZero(MTUtil_ReadVt2(i, v2), "MTUtil_ReadVt2" + teccNum);
                            ThrowErrorOnNonZero(MTUtil_GetErrorStatus(i, statusAll), "MTUtil_GetErrorStatus" + teccNum);
                            ThrowErrorOnNonZero(MTUtil_GetRtx(i, rtxInfo), "MTUtil_GetRtx" + teccNum);

                            if (shortElectrodes)
                            {
                                ThrowErrorOnNonZero(MTUtil_ShortVElectrodes(i), "MTUtil_ShortVElectrodes" + teccNum);    
                            }
                        }

                        for (int j = 0; j < 4; j++)
                        {
                            var teccLocation = TeccWellMapper.GetMap(CurrentPlateFormat, channels.Count + 1);
                            var address = teccLocation.WellAddress;

                            var rCorrected = r[j] * Area;
                            var status = new BitArray(new[] { statusAll[j] });

                            var channel = new ChannelValue
                            {
                                Channel = teccLocation.TeccNumber,
                                WellAddress = address,
                                WellNumber = teccLocation.WellNumber,
                                WellColumn = teccLocation.WellColumn,
                                WellRow = teccLocation.WellRow,
                                BoardChannel = j + 1,
                                TeccBoard = i + 1,
                                Resistance = rCorrected,
                                ResistanceRaw = r[j],
                                VoltagePreMv = v1[j],
                                VoltagePostMv = v2[j],
                                StatusOscillation = status.Get(0),
                                StatusVoltageOverflowOnV1 = status.Get(1),
                                StatusVoltageOverflowOnV2 = status.Get(2),
                                StatusCurrentOverflowOnRt = status.Get(3),
                                StatusVoltageOverflowOnRt = status.Get(4),
                                CurrentRtxIndex = (byte)(rtxInfo[0 + j * 2] + 1),
                                NextRtxIndex = (byte)(rtxInfo[1 + j * 2] + 1),
                            };

                            channels.Add(channel);
                        }
                    }
                    return channels.OrderBy(c => c.WellRow).ThenBy(c => c.WellColumn).ToList();
                }
            }
            catch (Exception ex)
            {
                log.Error("Problem with Read", ex);
                return null;
            }
        }

        public List<ChannelValue> VtRecordAndRead(int timeOutMs, int? debugTimeS, bool shortElectrodes, int debugReadIndex)
        {
            var rand = new Random();

            var debugging = (DebugMode);

            try
            {
                using (log.GetLoggerTimer("VtRecordAndRead"))
                {
                    var channels = new List<ChannelValue>();

                    var v = new float[4];
                    var statusAll = new byte[4];

                    if (debugging)
                    {
                        var delay = debugTimeS ?? 30;
                        Task.Delay(delay * 1000).Wait();
                    }
                    else
                    {
                        for (int i = 0; i < RealBoardCount; i++)
                        {
                            var teccNum = " on TECC #" + (i + 1) + " (1 Based)";
                            ThrowErrorOnNonZero(MTUtil_RecordVt(i), "MTUtil_RecordVt" + teccNum);
                        }

                        log.Info("Setup all TECCs for read");

                        Task.Delay(200).Wait();

                        WaitForTecc(timeOutMs);
                    }

                    int wellIndex = 1;
                    for (int i = 0; i < hardware.DeviceCount; i++)
                    {
                        if (debugging)
                        {
                            for (int j = 0; j < 4; j++)
                            {
                                float pExtra = 0;
                                
                                if ((wellIndex % 59 == 0))
                                {
                                    pExtra = debugReadIndex * 2;
                                }
                                else if ((wellIndex % 18 == 0))
                                {
                                    pExtra = debugReadIndex * 1.5f;
                                }
                                else if ((wellIndex % 9 == 0))
                                {
                                    pExtra = debugReadIndex;
                                }
                                
                                v[j] = (float)(5 + pExtra + rand.NextDouble() * 1 - 0.5);
                                
                                for (int b = 0; b < 4; b++)
                                {
                                    if (rand.NextDouble() < 0.0025)
                                    {
                                        statusAll[j] |= (byte)Math.Pow(2, b);
                                    }
                                }

                                wellIndex++;
                            }
                        }
                        else
                        {
                            var teccNum = " on TECC #" + (i + 1) + " (1 Based)";
                            ThrowErrorOnNonZero(MTUtil_ReadVt1(i, v), "MTUtil_ReadVt1" + teccNum);
                            ThrowErrorOnNonZero(MTUtil_GetErrorStatus(i, statusAll), "MTUtil_GetErrorStatus" + teccNum);
                        }

                        for (int j = 0; j < 4; j++)
                        {
                            var teccLocation = TeccWellMapper.GetMap(CurrentPlateFormat, channels.Count + 1);
                            var address = teccLocation.WellAddress;

                            var status = new BitArray(new[] { statusAll[j] });

                            var channel = new ChannelValue
                            {
                                Channel = teccLocation.TeccNumber,
                                WellNumber = teccLocation.WellNumber,
                                WellAddress = address,
                                WellColumn = teccLocation.WellColumn,
                                WellRow = teccLocation.WellRow,
                                BoardChannel = j + 1,
                                TeccBoard = i + 1,
                                Resistance = 0,
                                ResistanceRaw = 0,
                                VoltagePreMv = v[j],
                                VoltagePostMv = v[j],
                                StatusOscillation = status.Get(0),
                                StatusVoltageOverflowOnV1 = status.Get(1),
                                StatusVoltageOverflowOnV2 = status.Get(2),
                                StatusCurrentOverflowOnRt = status.Get(3),
                                StatusVoltageOverflowOnRt = status.Get(4),
                                CurrentRtxIndex = 0,
                                NextRtxIndex = 0,
                            };

                            channels.Add(channel);
                        }
                    }

                    return channels.OrderBy(c => c.WellRow).ThenBy(c => c.WellColumn).ToList();
                }
            }
            catch (Exception ex)
            {
                log.Error("Problem with Read", ex);
                return null;
            }
        }

        private void ThrowErrorOnNonZero(int returnCode, string taskName)
        {
            if (returnCode != 0)
            {
                var errString = "No Error Info for this Code";
                
                try
                {
                    errString = GetErrorCodeFromTecc(returnCode);
                }
                catch (Exception ex)
                {
                    log.ErrorFormat("Could not load error string for '{0}' got exception '{1}'", returnCode, ex.Message);
                }

                var buf = string.Format("TECC Error code {0} when running '{1}' message was '{2}'", returnCode, taskName, errString);

                log.Error(buf);
                throw new TeccException(buf);
            }
        }

        private string GetErrorCodeFromTecc(int returnCode)
        {
            var msg = MTUtil_GetI2cErrorString(returnCode);
            var buf = Marshal.PtrToStringAnsi(msg);
            return buf;
        }
    }
}
