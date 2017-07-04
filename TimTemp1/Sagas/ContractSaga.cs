﻿using TimTemp1.Abstractions;
using TimTemp1.Commands;

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
            //TODO finish
        }
    }
}