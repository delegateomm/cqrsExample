using TimTemp1.Abstractions;
using TimTemp1.Sagas;

namespace TimTemp1.Commands
{
    public class CreateContractCommand : BaseCommand
    {
        public double Amount { get; set; }

        public CreateContractCommand()
        {
            SagaName = typeof(ContractSaga).Name;
        }
    }
}