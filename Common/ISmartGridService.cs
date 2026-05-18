using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [ServiceContract]
    public interface ISmartGridService
    {
        [OperationContract]
        void StartSession(string meta);

        [OperationContract]
        [FaultContract(typeof(ValidationFault))]
        [FaultContract(typeof(DataFormatFault))]
        void PushSample(SmartGridSample sample);

        [OperationContract]
        void EndSession();
    }
}
