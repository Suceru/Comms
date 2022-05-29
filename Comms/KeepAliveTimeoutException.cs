using System;

namespace Comms;

public class KeepAliveTimeoutException : Exception
{
	public KeepAliveTimeoutException(string message)
		: base(message)
	{
	}
}
