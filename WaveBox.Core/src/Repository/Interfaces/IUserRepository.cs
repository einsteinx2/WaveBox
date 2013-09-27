using System;
using System.Collections.Generic;
using WaveBox.Core.Model;

namespace WaveBox.Core.Model.Repository
{
	public interface IUserRepository
	{
		User UserForId(int userId);
		User UserForName(string userName);
		User CreateUser(string userName, string password, Role role, long? deleteTime);
		User CreateTestUser(long? durationSeconds);
		IList<User> AllUsers();
		bool DeleteFromUserCache(User user);
		bool UpdateUserCache(User user);
		IList<User> ExpiredUsers();
	}
}

