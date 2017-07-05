using System.Threading.Tasks;
using TimTemp1.Abstractions;
using TimTemp1.Commands;
using TimTemp1.DomainEvents;
using TimTemp1.DtoModels;
using TimTemp1.DtoModels.DomainServiceInputModels;
using TimTemp1.DtoModels.DomainServiceResultModels;

namespace TimTemp1.ApplicationServices
{
    /// <summary>
    /// Service for Contract aggregate
    /// </summary>
    public class ContractApplicationService : BaseApplicationService
    {
        public ContractApplicationService(IBus bus) : base(bus)
        {
        }

        //TODO example - need to finish
        public async Task<ConractCreateActionResult> CreateConract(ContractInputModel model)
        {
            var command = model.ToCreateContractCommand();
            Bus.SendCommand(command);
            var commandResult = await Bus.WaitCommandCompletion(command.Id, CommandCompletionTimeout);
            return new ConractCreateActionResult
            {
                ContractId = ((dynamic) commandResult.Model).Id,
                Amount = ((dynamic) commandResult.Model).Amount
            };
        }
    }
}