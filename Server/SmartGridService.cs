using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Configuration;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace Service
{
    public class SmartGridService : ISmartGridService, IDisposable
    {

        private bool disposed = false;

        public delegate void SGEventHandler(string message);

        public event SGEventHandler OnTransferStarted;
        public event SGEventHandler OnSampleReceived;
        public event SGEventHandler OnTransferCompleted;
        public event SGEventHandler OnWarningRaised;
        public event SGEventHandler FrequencySpike;
        public event SGEventHandler OutOfBandWarning;
        public event SGEventHandler PowerSpike;

        private static double totalPower = 0;
        private static int sampleCount = 0;
        private static double? previousFrequency = null;
        private static double frequencySum = 0;
        private static int frequencyCount = 0;

        public SmartGridService()
        {
            OnTransferStarted += msg =>
                Console.WriteLine(msg);

            OnSampleReceived += msg =>
                Console.WriteLine(
                    $"Sample received {msg}");

            OnTransferCompleted += msg =>
                Console.WriteLine(msg);

            OnWarningRaised += warning =>
                Console.WriteLine(
                    $"WARNING: {warning}");

            FrequencySpike += msg =>
               Console.WriteLine($"[FREQUENCY SPIKE] {msg}");

            OutOfBandWarning += msg =>
                Console.WriteLine($"[WARNING] {msg}");

            PowerSpike += msg =>
               Console.WriteLine($"[POWERR SPIKE] {msg}");

        }

        public void StartSession(string meta)
        {
            OnTransferStarted?.Invoke("Transfer started");
            

            Console.WriteLine($"Session started: {meta}");

            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Measurements");

            Directory.CreateDirectory(folder);

            string measurementsPath = Path.Combine(folder, "measurements_session.csv");

          string   rejectsPath = Path.Combine(folder, "rejects.csv");

            if (!File.Exists(measurementsPath))
            {
                File.WriteAllText(measurementsPath, "Timestamp,Volatage,Current,PowerUsage,Frequency,FaultIndicator" + Environment.NewLine);
            }

            if (!File.Exists(rejectsPath))
            {
                File.WriteAllText(rejectsPath, "TimeStamp,Voltage,Current,PowerUsage,Frequency,FaultIndicator,Rason" + Environment.NewLine);
            }

        }

        public void PushSample(SmartGridSample sample)
        {
            OnSampleReceived?.Invoke(sampleCount.ToString());

            if (sample == null)
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault
                    {
                        Message = "Sample cannot be null."
                    });
            }

            double pThreshold = double.Parse(ConfigurationManager.AppSettings["P_max_threshold"]);
            double fThreshold = double.Parse(ConfigurationManager.AppSettings["F_threshold"]);
            double devTreshhold = double.Parse(ConfigurationManager.AppSettings["Devitation_threshhold"]);

            if (sample.Timestamp == default(DateTime))
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        Message = "Timestamp is required.",
                        Field = "Timestamp"
                    },
                new FaultReason("Validation error occured.")
                );
            }

            if (sample.Frequency <= 0)
            {
                SaveReject(sample, "Frequency must be greater than 0.");

                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        Message = "Frequency must be greater than 0.",
                        Field = "Frequency"
                    },
                new FaultReason("Validation error occured.")
                );
            }

            if (sample.Voltage < 0 || sample.Voltage > 300)
            {
                SaveReject(sample, "Voltage out of allowed range");

                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        Message = "Voltage out of allowed range.",
                        Field = "Voltage"
                    },
                new FaultReason("Validation error occured.")
                );
            }

            if (sample.Current < 0 || sample.Current > 500)
            {
                SaveReject(sample, "Current out of allowed range");

                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        Message = "Current out of allowed range.",
                        Field = "Current"
                    },
                new FaultReason("Validation error occured.")
                );
            }

            if (sample.PowerUsage < 0 || sample.PowerUsage > 10)
            {
                SaveReject(sample, "Power usage out of allowed range");

                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        Message = "Power usage out of allowed range.",
                        Field = "PowerUsage"
                    },
                new FaultReason("Validation error occured.")
                );
            }

            if (sample.PowerUsage > pThreshold)
            {
                OnWarningRaised?.Invoke($"Power threshold exceeded: {sample.PowerUsage}");
            }

            if (sample.Frequency > fThreshold)
            {
                OnWarningRaised?.Invoke($"Frequency threshold exceeded: {sample.Frequency}");
            }

            sampleCount++;
            totalPower += sample.PowerUsage;

            double average = totalPower / sampleCount;

            if (sampleCount > 1)
            {
                double lower = average * (1 - devTreshhold/100);
                double upper = average * (1 + devTreshhold/100);

                if (sample.PowerUsage < lower ||
                    sample.PowerUsage > upper)
                {
                    OnWarningRaised?.Invoke($"Power usage deviates more than {devTreshhold}% from average.");
                }
            }

            if (previousFrequency.HasValue)
            {
                double deltaF = sample.Frequency - previousFrequency.Value;

                if (Math.Abs(deltaF) > fThreshold)
                {
                    string direction =
                        deltaF > 0
                        ? "over expected"
                        : "under expected";

                    FrequencySpike?.Invoke($"delta F = {deltaF:F2}, direction: {direction}");
                }
            }

            frequencyCount++;
            frequencySum += sample.Frequency;

            double fMean = frequencySum / frequencyCount;
            previousFrequency = sample.Frequency;

            double lowerF =  fMean * (1 - devTreshhold / 100.0);
            double upperF=  fMean * (1 + devTreshhold / 100.0);

            if (sample.Frequency < lowerF)
            {
                OutOfBandWarning?.Invoke($"Frequency {sample.Frequency:F2} is lower than expected value.");
            }
            else if (sample.Frequency > upperF)
            {
                OutOfBandWarning?.Invoke($"Frequency {sample.Frequency:F2} is greater than expected value.");
            }

            double power = sample.Voltage * sample.Current;

            if(power>pThreshold)
            {
                PowerSpike?.Invoke($"Power ={power:F2}");
            }

            SaveMeasurement(sample);

            Console.WriteLine($"Sample:\nTime:{sample.Timestamp}\nVoltage={sample.Voltage}\nCurrent={sample.Current}\nPower={sample.PowerUsage}\nFrequeny={sample.Frequency}\nFault={sample.FaultIndicator}\n");
        }

        public void EndSession()
        {
            OnTransferCompleted?.Invoke("Transfer completed");

            Console.WriteLine("Session ended");
        }

        ~SmartGridService()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
             //   if (disposing)
               // {
                //    Console.WriteLine("Disposing.");
             //   }
                disposed = true;
            }
        }

        private void SaveMeasurement(SmartGridSample sample)
        {
            using (StreamWriter sw = new StreamWriter(MeasurementsPath, true))
            {
                sw.WriteLine($"{sample.Timestamp}," + $"{sample.Voltage}," +$"{sample.Current}," + $"{sample.PowerUsage}," + $"{sample.Frequency}," + $"{sample.FaultIndicator}");
            }
        }

        private void SaveReject(SmartGridSample sample, string reason)
        {
            using (StreamWriter sw = new StreamWriter(RejectsPath, true))
            {
                sw.WriteLine($"{sample.Timestamp}," + $"{sample.Voltage}," + $"{sample.Current}," + $"{sample.PowerUsage}," + $"{sample.Frequency}," + $"{sample.FaultIndicator}," + $"\"{reason}\"");
            }
        }

        private string MeasurementsPath
        {
            get
            {
                string folder = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Measurements");

                Directory.CreateDirectory(folder);

                return Path.Combine(folder, "measurements_session.csv");
            }
        }

        private string RejectsPath
        {
            get
            {
                string folder = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Measurements");

                Directory.CreateDirectory(folder);

                return Path.Combine(folder, "rejects.csv");
            }
        }
    }
}
