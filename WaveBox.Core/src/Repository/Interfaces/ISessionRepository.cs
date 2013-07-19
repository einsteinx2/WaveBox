using System;
using System.Collections.Generic;

namespace WaveBox.Model.Repository
{
	public interface ISessionRepository
	{
		Session CreateSession(int userId, string clientName);
		List<Session> AllSessions();
		int CountSessions();
		bool DeleteSessionsForUserId(int userId);
	}
}

