using System;
using System.Collections.Generic;

namespace WaveBox.Model.Repository
{
	public interface ISessionRepository
	{
		Session SessionForRowId(int rowId);
		Session SessionForSessionId(string sessionId);
		Session CreateSession(int userId, string clientName);
		List<Session> AllSessions();
		int CountSessions();
		bool DeleteSessionsForUserId(int userId);
	}
}

