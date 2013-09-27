using System;
using System.Collections.Generic;
using WaveBox.Core.Model;

namespace WaveBox.Core.Model.Repository
{
	public interface IUserRepository
	{
		bool ReloadUsers();
		User UserForId(int userId);
		User UserForName(string userName);
		User CreateUser(string userName, string password, Role role, long? deleteTime);
		User CreateTestUser(long? durationSeconds);
		IList<User> AllUsers();
		IList<User> ExpiredUsers();
	}
}

