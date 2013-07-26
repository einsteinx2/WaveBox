using System;
using System.Collections.Generic;

namespace WaveBox.Core.Model.Repository
{
	public interface IUserRepository
	{
		User UserForId(int userId);
		User UserForName(string userName);
		User CreateUser(string userName, string password, long? deleteTime);
		User CreateTestUser(long? durationSeconds);
		string UserNameForSessionid(string sessionId);
		List<User> AllUsers();
		List<User> ExpiredUsers();
	}
}

