using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class SmartGridService : ISmartGridService
    {
        public void StartSession(string meta)
        {
            Console.WriteLine($"Session started: {meta}");            
        }

        public void PushSample(SmartGridSample sample)
        {
            if (sample == null)
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault
                    {
                        Message = "Sample cannot be null."
                    });
            }

            if (sample.Timestamp == default(DateTime))
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        Message = "Timestamp is required.",
                        Field = "Timestamp"
                    });
            }

            if (sample.Frequency <= 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        Message = "Frequency must be greater than 0.",
                        Field = "Frequency"
                    });
            }

            if (sample.Voltage < 0 || sample.Voltage > 1000)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        Message = "Voltage out of allowed range.",
                        Field = "Voltage"
                    });
            }

            if (sample.Current < 0 || sample.Current > 500)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        Message = "Current out of allowed range.",
                        Field = "Current"
                    });
            }

            if (sample.PowerUsage < 0 || sample.PowerUsage > 20)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        Message = "Power usage out of allowed range.",
                        Field = "PowerUsage"
                    });
            }

            Console.WriteLine($"Sample: {sample.Timestamp}\nVoltage={sample.Voltage}\nCurrent={sample.Current}\nPower={sample.PowerUsage}\nFrequeny={sample.Frequency}\nFault={sample.FaultIndicator}");
        }

        public void EndSession()
        {
            Console.WriteLine("Session ended");
        }
    }
}
