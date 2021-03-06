﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RabbitMqBusImplementation;
using TimTemp1.Abstractions;
using TimTemp1.ApplicationServices;
using TimTemp1.DomainEventHandlers;
using TimTemp1.DtoModels.DomainServiceInputModels;
using TimTemp1.Sagas;

namespace TestConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Test();

            Console.ReadLine();
        }

        private class MyClass
        {
            public string Name { get; set; }

            public int Age { get; set; }

        }

        private static async void Test()
        {
            IBus bus = new RabbitMqBus("timnginxdev01.spb.local");
            var configurator = new BusConfigurator(bus);

            configurator
                .RegisterSaga<ContractSaga>()
                .RegisterHandler<TestEventHandler>();

            ContractApplicationService applicationService = new ContractApplicationService(bus);

            var result = await applicationService.CreateConract(new ContractInputModel{Amount = 100800.99});
        }
    }
}
