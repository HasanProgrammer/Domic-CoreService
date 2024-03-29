﻿using Domic.Core.Domain.Contracts.Abstracts;

namespace Domic.Core.Domain.Entities;

public class Log : BaseEntity<string>
{
    public new string Id      { get; set; }
    public string UniqueKey   { get; set; }
    public string ServiceName { get; set; }
    public object Item        { get; set; }
}