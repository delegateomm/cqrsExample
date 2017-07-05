using TimTemp1.Commands;
using TimTemp1.DtoModels.DomainServiceInputModels;

namespace TimTemp1.DtoModels
{
    public static class DtoModelsExtensions
    {
        public static CreateContractCommand ToCreateContractCommand(this ContractInputModel source)
        {
            return new CreateContractCommand{Amount = source.Amount};
        }
    }
}