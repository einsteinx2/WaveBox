using System;

namespace WaveBox.Core.Model
{
	public enum Role
	{
		// Test - used for support, read-only access
		Test = 1,
		// Guest - unprivileged user, read-only access
		Guest = 2,
		// User - standard user, read/write access, basic settings
		User = 3,
		// Admin - privileged user, read/write access, full settings
		Admin = 4
	}
}
