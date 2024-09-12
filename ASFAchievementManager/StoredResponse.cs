using SteamKit2.Internal;

namespace ASFAchievementManager;

public class StoredResponse {
	public bool Success { get; set; }
	public CMsgClientGetUserStatsResponse? Response { get; set; }
}
