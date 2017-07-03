﻿using System.Threading.Tasks;
using TimTemp1.Abstractions;
using TimTemp1.Commands;
using TimTemp1.DomainEvents;

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
        public async Task CreateConract()
        {
            Bus.RaiseEvent(new StartCreatingContractEvent());
            await Bus.SendCommand(new CreateContractCommand());
            Bus.RaiseEvent(new FinishCreatingContractEvent());
        }
    }
}