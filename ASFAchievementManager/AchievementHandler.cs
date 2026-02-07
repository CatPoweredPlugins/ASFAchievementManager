using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Localization;
using SteamKit2;
using SteamKit2.Internal;



namespace ASFAchievementManager;
public sealed class AchievementHandler : ClientMsgHandler {
	public override void HandleMsg(IPacketMsg packetMsg) {
		if (packetMsg == null) {
			ASF.ArchiLogger.LogNullError(packetMsg);

			return;
		}

#pragma warning disable IDE0010 // VS, go home, you're drunk
		switch (packetMsg.MsgType) {
			case EMsg.ClientGetUserStatsResponse:
				ClientMsgProtobuf<CMsgClientGetUserStatsResponse> getAchievementsResponse = new(packetMsg);
				Client.PostCallback(new GetAchievementsCallback(packetMsg.TargetJobID, getAchievementsResponse.Body));
				break;
			case EMsg.ClientStoreUserStatsResponse:
				ClientMsgProtobuf<CMsgClientStoreUserStatsResponse> setAchievementsResponse = new(packetMsg);
				Client.PostCallback(new SetAchievementsCallback(packetMsg.TargetJobID, setAchievementsResponse.Body));
				break;
			default:
				break;
		}
#pragma warning restore IDE0010 // VS, go home, you're drunk

	}

	internal abstract class AchievementsCallBack<T> : CallbackMsg {
		internal readonly T Response;
		internal readonly bool Success;

		internal AchievementsCallBack(JobID jobID, T msg, Func<T, EResult> eresultGetter, string error) {
			ArgumentNullException.ThrowIfNull(jobID);

			if (msg == null) {
				throw new ArgumentNullException(nameof(msg));
			}

			JobID = jobID;
			Success = eresultGetter(msg) == EResult.OK;
			Response = msg;

			if (!Success) {
				ASF.ArchiLogger.LogGenericError(string.Format(CultureInfo.CurrentCulture, Strings.ErrorFailingRequest, error));
			}
		}

	}

	internal sealed class GetAchievementsCallback : AchievementsCallBack<CMsgClientGetUserStatsResponse> {
		internal GetAchievementsCallback(JobID jobID, CMsgClientGetUserStatsResponse msg)
			: base(jobID, msg, msg => (EResult) msg.eresult, "GetAchievements") { }
	}

	internal sealed class SetAchievementsCallback : AchievementsCallBack<CMsgClientStoreUserStatsResponse> {
		internal SetAchievementsCallback(JobID jobID, CMsgClientStoreUserStatsResponse msg)
			: base(jobID, msg, msg => (EResult) msg.eresult, "SetAchievements") { }
	}

	//Utilities

	private static List<StatData>? ParseResponse(CMsgClientGetUserStatsResponse response) {
		List<StatData> result = [];
		KeyValue keyValues = new();
		if (response.schema != null) {
			using (MemoryStream ms = new(response.schema)) {
				if (!keyValues.TryReadAsBinary(ms)) {
					ASF.ArchiLogger.LogGenericError(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsInvalid, nameof(response.schema)));
					return null;
				};
			}

			//first we enumerate all real achievements
			foreach (KeyValue stat in keyValues.Children.Find(child => child.Name == "stats")?.Children ?? []) {
				string? childTypeValue = stat.Children.Find(child => child.Name == "type")?.Value?.ToUpperInvariant();
				if (childTypeValue == "4" || childTypeValue == "ACHIEVEMENTS") {
					foreach (KeyValue achievement in stat.Children.Find(child => child.Name == "bits")?.Children ?? []) {
						if (int.TryParse(achievement.Name, out int bitNum)) {
							if (uint.TryParse(stat.Name, out uint statNum)) {
								uint? stat_value = response?.stats?.Find(statElement => statElement.stat_id == statNum)?.stat_value;
								bool isSet = stat_value != null && (stat_value & ((uint) 1 << bitNum)) != 0;

								bool restricted = achievement.Children.Find(child => child.Name == "permission" && child.Value != null) != null;

								string? dependancyName = (achievement.Children.Find(child => child.Name == "progress") == null) ? "" : achievement.Children.Find(child => child.Name == "progress")?.Children?.Find(child => child.Name == "value")?.Children?.Find(child => child.Name == "operand1")?.Value;

								if (!uint.TryParse((achievement.Children.Find(child => child.Name == "progress") == null) ? "0" : achievement.Children.Find(child => child.Name == "progress")!.Children.Find(child => child.Name == "max_val")?.Value, out uint dependancyValue)) {
									ASF.ArchiLogger.LogGenericError(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsInvalid, nameof(dependancyValue)));
									return null;
								}

								string lang = ASFAchievementManager.AchievementsCulture == null ?
													CultureInfo.CurrentUICulture.EnglishName.ToLower(CultureInfo.CurrentCulture) :
													ASFAchievementManager.AchievementsCulture.EnglishName.ToLower(CultureInfo.CurrentCulture);

								Dictionary<string, string> countryLanguageMap = new()
								{
										{ "portuguese (brazil)", "brazilian" },
										{ "korean", "koreana" },
										{ "chinese (traditional)", "tchinese" },
										{ "chinese (simplified)", "schinese" }
								};

								if (countryLanguageMap.TryGetValue(lang, out string? value)) {
									lang = value;
								} else {
									if (lang.IndexOf('(', StringComparison.Ordinal) > 0) {
										lang = lang[..(lang.IndexOf('(', StringComparison.Ordinal) - 1)];
									}
								}
								if (achievement.Children.Find(child => child.Name == "display")?.Children?.Find(child => child.Name == "name")?.Children?.Find(child => child.Name == lang) == null) {
									lang = "english"; // Fallback
								}

								string? name = achievement.Children.Find(child => child.Name == "display")?.Children?.Find(child => child.Name == "name")?.Children?.Find(child => child.Name == lang)?.Value;
								result.Add(new StatData() {
									StatNum = statNum,
									BitNum = bitNum,
									IsSet = isSet,
									Restricted = restricted,
									DependancyValue = dependancyValue,
									DependancyName = dependancyName,
									Dependancy = 0,
									Name = name,
									StatValue = stat_value ?? 0
								});

							}
						}
					}
				}
			}
			//Now we update all dependancies
			foreach (KeyValue stat in keyValues.Children.Find(child => child.Name == "stats")?.Children ?? []) {
				if (stat.Children.Find(child => child.Name == "type")?.Value == "1") {
					if (uint.TryParse(stat.Name, out uint statNum)) {
						bool restricted = int.TryParse(stat.Children.Find(child => child.Name == "permission")?.Value, out int value) && value > 1;
						string? name = stat.Children.Find(child => child.Name == "name")?.Value;
						if (name != null) {
							StatData? parentStat = result.Find(item => item.DependancyName == name);
							if (parentStat != null) {
								parentStat.Dependancy = statNum;
								if (restricted && !parentStat.Restricted) {
									parentStat.Restricted = true;
								}
							}
						}
					}
				}
			}
		}
		return result;
	}

	private static IEnumerable<CMsgClientStoreUserStats2.Stats> GetStatsToSet(List<CMsgClientStoreUserStats2.Stats> statsToSet, StatData statToSet, bool set = true) {
		if (statToSet == null) {
			yield break; //it should never happen
		}

		CMsgClientStoreUserStats2.Stats? currentstat = statsToSet.Find(stat => stat.stat_id == statToSet.StatNum);
		if (currentstat == null) {
			currentstat = new CMsgClientStoreUserStats2.Stats() {
				stat_id = statToSet.StatNum,
				stat_value = statToSet.StatValue
			};
			yield return currentstat;
		}

		uint statMask = (uint) 1 << statToSet.BitNum;
		if (set) {
			currentstat.stat_value |= statMask;
		} else {
			currentstat.stat_value &= ~statMask;
		}
		if (!string.IsNullOrEmpty(statToSet.DependancyName)) {
			CMsgClientStoreUserStats2.Stats? dependancystat = statsToSet.Find(stat => stat.stat_id == statToSet.Dependancy);
			if (dependancystat == null) {
				dependancystat = new CMsgClientStoreUserStats2.Stats() {
					stat_id = statToSet.Dependancy,
					stat_value = set ? statToSet.DependancyValue : 0
				};
				yield return dependancystat;
			}
		}

	}

	//Endpoints

	internal async Task<string> GetAchievements(Bot bot, ulong gameID) {
		if (!Client.IsConnected) {
			return Strings.BotNotConnected;
		}

		GetAchievementsCallback? response = await GetAchievementsResponse(bot, gameID).ConfigureAwait(false);

		if (response == null || response.Response == null || !response.Success) {
			return "Can't retrieve achievements for " + gameID.ToString(CultureInfo.CurrentCulture);
		}

		List<string> responses = [];
		List<StatData>? stats = ParseResponse(response.Response);
		if (stats == null) {
			bot.ArchiLogger.LogNullError(stats);
		} else if (stats.Count == 0) {
			bot.ArchiLogger.LogNullError(null, nameof(stats));
		} else {

			foreach (StatData stat in stats) {
				responses.Add(string.Format(CultureInfo.CurrentCulture, "{0,-5}", stats.IndexOf(stat) + 1) + (stat.IsSet ? "[\u2705] " : "[\u274C] ") + (stat.Restricted ? "\u26A0\uFE0F " : "") + stat.Name);
			}

		}
		return responses.Count > 0 ? "\u200B\nAchievements for " + gameID.ToString(CultureInfo.CurrentCulture) + ":\n" + string.Join(Environment.NewLine, responses) : "Can't retrieve achievements for " + gameID.ToString(CultureInfo.CurrentCulture);
	}

	internal async Task<string> SetAchievements(Bot bot, uint appId, HashSet<uint> achievements, bool set = true) {
		if (!Client.IsConnected) {
			return Strings.BotNotConnected;
		}

		List<string> responses = [];

		GetAchievementsCallback? response = await GetAchievementsResponse(bot, appId).ConfigureAwait(false);
		if (response == null) {
			bot.ArchiLogger.LogNullError(response);
			return "Can't retrieve achievements for " + appId.ToString(CultureInfo.CurrentCulture);
			;
		}

		if (!response.Success) {
			return "Can't retrieve achievements for " + appId.ToString(CultureInfo.CurrentCulture);
			;
		}

		if (response.Response == null) {
			bot.ArchiLogger.LogNullError(response.Response);
			responses.Add(Strings.WarningFailed);
			return "\u200B\n" + string.Join(Environment.NewLine, responses);
		}

		List<StatData>? stats = ParseResponse(response.Response);
		if (stats == null) {
			responses.Add(Strings.WarningFailed);
			return "\u200B\n" + string.Join(Environment.NewLine, responses);
		}

		List<CMsgClientStoreUserStats2.Stats> statsToSet = [];

		if (achievements.Count == 0) { //if no parameters provided - set/reset all. Don't kill me Archi.
			foreach (StatData stat in stats.Where(s => !s.Restricted)) {
				statsToSet.AddRange(GetStatsToSet(statsToSet, stat, set));
			}
		} else {
			foreach (uint achievement in achievements) {
				if (stats.Count < achievement) {
					responses.Add("Achievement #" + achievement.ToString(CultureInfo.CurrentCulture) + " is out of range");
					continue;
				}

				if (stats[(int) achievement - 1].IsSet == set) {
					responses.Add("Achievement #" + achievement.ToString(CultureInfo.CurrentCulture) + " is already " + (set ? "unlocked" : "locked"));
					continue;
				}
				if (stats[(int) achievement - 1].Restricted) {
					responses.Add("Achievement #" + achievement.ToString(CultureInfo.CurrentCulture) + " is protected and can't be switched");
					continue;
				}

				statsToSet.AddRange(GetStatsToSet(statsToSet, stats[(int) achievement - 1], set));
			}
		}
		if (statsToSet.Count == 0) {
			responses.Add(Strings.WarningFailed);
			return "\u200B\n" + string.Join(Environment.NewLine, responses);
		};
		if (responses.Count > 0) {
			responses.Add("Trying to switch remaining achievements..."); //if some errors occured
		}
		ClientMsgProtobuf<CMsgClientStoreUserStats2> request = new(EMsg.ClientStoreUserStats2) {
			SourceJobID = Client.GetNextJobID(),
			Body = {
				game_id =  appId,
				settor_steam_id = bot.SteamID,
				settee_steam_id = bot.SteamID,
				explicit_reset = false,
				crc_stats = response.Response.crc_stats
			}
		};
		request.Body.stats.AddRange(statsToSet);
		Client.Send(request);

		SetAchievementsCallback setResponse = await new AsyncJob<SetAchievementsCallback>(Client, request.SourceJobID).ToLongRunningTask().ConfigureAwait(false);

		responses.Add((setResponse?.Success ?? false) ? Strings.Success : Strings.WarningFailed);
		return "\u200B\n" + string.Join(Environment.NewLine, responses);
	}

	private async Task<GetAchievementsCallback?> GetAchievementsResponse(Bot bot, ulong gameID) {
		if (!Client.IsConnected) {
			return null;
		}

		ClientMsgProtobuf<CMsgClientGetUserStats> request = new(EMsg.ClientGetUserStats) {
			SourceJobID = Client.GetNextJobID(),
			Body = {
				game_id =  gameID,
				steam_id_for_user = bot.SteamID,
			}
		};

		Client.Send(request);

		return await new AsyncJob<GetAchievementsCallback>(Client, request.SourceJobID).ToLongRunningTask().ConfigureAwait(false);
	}
}
