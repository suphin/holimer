// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Ekomers.Common.Services;

public interface ISmsSender
{
    Task<bool> SendSmsAsync(string number, string message);
}
