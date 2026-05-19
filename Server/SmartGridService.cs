using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    public class SmartGridService : ISmartGridService, IDisposable
    {
        private bool disposed = false;

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
                    },
                new FaultReason("Validation error occured.")
                );
            }

            if (sample.Frequency <= 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        Message = "Frequency must be greater than 0.",
                        Field = "Frequency"
                    },
                new FaultReason("Validation error occured.")
                );
            }

            if (sample.Voltage < 0 || sample.Voltage > 1000)
            {
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
                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        Message = "Current out of allowed range.",
                        Field = "Current"
                    },
                new FaultReason("Validation error occured.")
                );
            }

            if (sample.PowerUsage < 0 || sample.PowerUsage > 20)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        Message = "Power usage out of allowed range.",
                        Field = "PowerUsage"
                    },
                new FaultReason("Validation error occured.")
                );
            }

            Console.WriteLine($"Sample: {sample.Timestamp}\nVoltage={sample.Voltage}\nCurrent={sample.Current}\nPower={sample.PowerUsage}\nFrequeny={sample.Frequency}\nFault={sample.FaultIndicator}");
        }

        public void EndSession()
        {
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
                if (disposing)
                {
                    Console.WriteLine("Disposing.");
                }
                disposed = true;
            }
        }
    }
}
