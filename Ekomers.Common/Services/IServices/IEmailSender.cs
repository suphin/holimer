// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Ekomers.Common.Services;

public interface IEmailSenderService
{
	Task<bool> SendEmailAsync(string email, string subject, string message);
}
