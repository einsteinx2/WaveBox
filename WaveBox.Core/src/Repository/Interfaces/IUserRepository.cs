using System;
using System.Collections.Generic;

namespace WaveBox.Model.Repository
{
	public interface IUserRepository
	{
		string UserNameForSessionid(string sessionId);
		List<User> AllUsers();
		List<User> ExpiredUsers();
	}
}

