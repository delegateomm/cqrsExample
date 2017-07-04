using TimTemp1.Abstractions;
using TimTemp1.Sagas;

namespace TimTemp1.Commands
{
    public class CreateContractCommand : BaseCommand
    {
        public CreateContractCommand()
        {
            SagaName = typeof(ContractSaga).Name;
        }
    }
}