using System;
using NationalInstruments.DAQmx;

namespace BreatheZ.Ui.MotionControl
{
    public class DaqOperations
    {
        private readonly object locker = new object();

        public const string DaqDigIo = "DAQ_DIO";
        public const string DaqRelay = "DAQ_Relay";

        public bool[] GetMultiDigIn(string dev, int port, int lineStart, int lineEnd)
        {
            var line = (lineEnd == lineStart) ? lineEnd.ToString() : string.Format("{0}:{1}", lineStart, lineEnd);
            
            string fullChannel = string.Format("{0}/port{1}/line{2}", dev, port, line);

            using (var task = new Task())
            {
                task.DIChannels.CreateChannel(fullChannel, "", ChannelLineGrouping.OneChannelForAllLines);

                var reader = new DigitalSingleChannelReader(task.Stream);

                lock (locker)
                {
                    var values = reader.ReadSingleSampleMultiLine();
                    return values;
                }
            }
        }

        public void SetSingleDigOut(string dev, int port, int line, bool value)
        {
            SetMultiDigOut(dev, port, line,line,new []{value});
        }

        public void SetMultiDigOut(string dev, int port, int lineStart, int lineEnd, bool[] values)
        {
            var line = (lineEnd == lineStart) ? lineEnd.ToString() : string.Format("{0}:{1}", lineStart, lineEnd);
            var count = lineEnd - lineStart + 1;

            if (values.Length != count)
            {
                throw new Exception("Incorrect number of binary values!");
            }

            string fullChannel = string.Format("{0}/port{1}/line{2}", dev, port, line);

            using (var task = new Task())
            {
                task.DOChannels.CreateChannel(fullChannel, "", ChannelLineGrouping.OneChannelForAllLines);

                var writer = new DigitalSingleChannelWriter(task.Stream);

                lock (locker)
                {
                    writer.WriteSingleSampleMultiLine(true, values);
                }
            }
        }
    }
}