using Common;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ChannelFactory<ISmartGridService> channelFactory = null;

            try
            {
                Console.WriteLine("Attempting to connect to SmartGrid service...");

                channelFactory = new ChannelFactory<ISmartGridService>("EndPoint");

                ISmartGridService proxy= channelFactory.CreateChannel();

                Console.WriteLine("Connection established.");

                proxy.StartSession("Session");

                Console.WriteLine("Session started successfully.");

                // test validation
                //proxy.PushSample(new SmartGridSample
                //{
                //    Voltage = -20,
                //    Current = 14,
                //    Frequency = 50
                //});
                //  proxy.PushSample(new SmartGridSample
                //  { Current = 14,
                //  FaultIndicator = true,
                //  Frequency = 50,
                //  PowerUsage = 2,
                //  Timestamp = Convert.ToDateTime("2024-01-01 00:00:00"),
                //  Voltage = 200 });


                string path = ConfigurationManager.AppSettings["DataSetPath"];
                string invalidCsvPath =ConfigurationManager.AppSettings["InvalidRowsCsv"];

                string fullDatasetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);

                string fullInvalidCsvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, invalidCsvPath);

                List<SmartGridSample> samples = new List<SmartGridSample>();
                try
                {
                    if (!File.Exists(fullDatasetPath))
                    {
                        Console.WriteLine("Dataset file not found.");
                        Console.ReadKey();
                        return;
                    }

                    Directory.CreateDirectory(
                        Path.GetDirectoryName(fullInvalidCsvPath));

                    File.WriteAllText(fullInvalidCsvPath, "RowNumber,RawData,ErrorMessage" + Environment.NewLine);

                    int rowNumber = 0;

                    foreach (string line in File.ReadLines(fullDatasetPath).Skip(1).Take(106))
                    {
                        rowNumber++;

                        try
                        {
                            string[] parts = line.Split(',');

                            if (parts.Length < 6)
                            {
                                throw new Exception("Invalid number of columns");
                            }

                            bool timestampParsed = DateTime.TryParse(parts[0], out DateTime timestamp);

                            bool voltageParsed = double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture,  out double voltage);

                            bool currentParsed = double.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out double current);

                            bool powerParsed = double.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out double powerUsage);

                            bool frequencyParsed =  double.TryParse(parts[4], NumberStyles.Any, CultureInfo.InvariantCulture, out double frequency);

                            bool faultParsed = int.TryParse(parts[5], out int faultIndicator);

                            if (!timestampParsed || !voltageParsed || !currentParsed || !powerParsed || !frequencyParsed   || !faultParsed)
                            {
                                throw new Exception("Parsing failed");
                            }

                            SmartGridSample sample = new SmartGridSample
                            {
                                Timestamp = timestamp,
                                Voltage = voltage,
                                Current = current,
                                Frequency = frequency,
                                PowerUsage = powerUsage,
                                FaultIndicator = faultIndicator ==1
                            };

                            samples.Add(sample);
                            proxy.PushSample(sample);


                        }
                        catch (Exception ex)
                        {
                            string escapedLine =
                                $"\"{line.Replace("\"", "\"\"")}\"";

                            File.AppendAllText(
                                fullInvalidCsvPath,
                                $"{rowNumber},{escapedLine},\"{ex.Message}\"{Environment.NewLine}");
                        }
                    }

                    Console.WriteLine(
                        $"Loaded {samples.Count} valid samples.");

                    Console.WriteLine(
                        $"Invalid rows saved to CSV: {fullInvalidCsvPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Unexpected error: {ex.Message}");
                }


               




                proxy.EndSession();

                Console.WriteLine("Session finished.");

                if (channelFactory.State == CommunicationState.Opened)
                {
                    channelFactory.Close();
                    Console.WriteLine("Factory closed gracefully.");
                }
            }
            catch (FaultException<ValidationFault> validationEx)
            {
                Console.WriteLine("VALIDATION ERROR");
                Console.WriteLine($"Reason: {validationEx.Detail.Message}");

                channelFactory?.Abort();
            }
            catch (FaultException<DataFormatFault> formatEx)
            {
                Console.WriteLine("DATA FORMAT ERROR");
                Console.WriteLine($"Description: {formatEx.Detail.Message}");

                channelFactory?.Abort();
            }
            catch (CommunicationException commEx)
            {
                Console.WriteLine($"Communication problem: {commEx.Message}");

                channelFactory?.Abort();
            }
            catch (TimeoutException timeoutEx)
            {
                Console.WriteLine($"Timeout occurred: {timeoutEx.Message}");

                channelFactory?.Abort();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");

                channelFactory?.Abort();
            }
            finally
            {
                if (channelFactory != null && channelFactory.State != CommunicationState.Closed)
                {
                    channelFactory.Abort();
                }

                Console.WriteLine("All client resources have been cleaned up.");
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}
