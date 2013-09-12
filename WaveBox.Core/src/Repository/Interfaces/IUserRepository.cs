using System;
using System.Collections.Generic;

namespace WaveBox.Core.Model.Repository
{
	public interface IUserRepository
	{
		User UserForId(int userId);
		User UserForName(string userName);
		User CreateUser(string userName, string password, int role, long? deleteTime);
		User CreateTestUser(long? durationSeconds);
		string UserNameForSessionid(string sessionId);
		IList<User> AllUsers();
		IList<User> ExpiredUsers();
	}
}

