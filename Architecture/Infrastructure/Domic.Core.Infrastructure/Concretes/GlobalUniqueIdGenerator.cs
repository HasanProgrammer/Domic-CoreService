﻿using Domic.Core.Domain.Contracts.Interfaces;
using NanoidDotNet;

namespace Domic.Core.Infrastructure.Concretes;

public class GlobalUniqueIdGenerator : IGlobalUniqueIdGenerator
{
    public string GetRandom() => Nanoid.Generate();

    public string GetRandom(int count) => Nanoid.Generate(size: count);
}