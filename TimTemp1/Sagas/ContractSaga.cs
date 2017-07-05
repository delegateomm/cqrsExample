using System;
using System.Collections.Generic;
using TimTemp1.Abstractions;
using TimTemp1.Abstractions.Enums;
using TimTemp1.Commands;
using TimTemp1.DomainEvents;

namespace TimTemp1.Sagas
{
    /// <summary>
    /// All methods with bussiness logic for Contracts
    /// </summary>
    public class ContractSaga : BaseSaga
    {
        public ContractSaga(IBus bus) : base(bus)
        {
        }

        public void Handle(CreateContractCommand command)
        {
            Bus.RaiseEvent(new StartCreatingContractEvent());

            Console.WriteLine($"command Id{command.Id} - {command.GetType().Name} SUCCEED");
            var contractId = new Random(1000).Next(0, 9999);

            Bus.RaiseEvent(new FinishCreatingContractEvent{Model = contractId});
            Bus.RaiseCommandCompletionEventEvent(new CommandCompletionEvent(command.Id)
            {
                CompletionStatus = CommandCompletionStatus.Successed,
                Model = new {Id = contractId, Amount = command.Amount}
            });
        }
    }
}