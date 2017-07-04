﻿using System;

namespace TimTemp1.Abstractions
{
    public abstract class BaseCommand : ICommand
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; }

        protected BaseCommand()
        {
            Id = Guid.NewGuid();
            Timestamp = DateTime.Now;
        }
    }
}