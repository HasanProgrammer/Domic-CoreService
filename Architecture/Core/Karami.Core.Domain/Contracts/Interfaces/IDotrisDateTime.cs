﻿namespace Karami.Core.Domain.Contracts.Interfaces;

public interface IDotrisDateTime
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public string ToPersianShortDate(DateTime dateTime) => throw new NotImplementedException();
}