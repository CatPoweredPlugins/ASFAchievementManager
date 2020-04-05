using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using ArchiSteamFarm;
using ArchiSteamFarm.Localization;
using SteamKit2;
using SteamKit2.Internal;
using System.Globalization;
using System.Collections.Concurrent;

namespace ASFAchievementManager {
	public sealed class AchievementHandler : ClientMsgHandler {
		ConcurrentDictionary<ulong, StoredResponse> Responses = new ConcurrentDictionary<ulong, StoredResponse>();
		public override void HandleMsg(IPacketMsg packetMsg) {
			if (packetMsg == null) {
				ASF.ArchiLogger.LogNullError(nameof(packetMsg));

				return;
			}

			switch (packetMsg.MsgType) {
				case EMsg.ClientGetUserStatsResponse:
					HandleGetUserStatsResponse(packetMsg);

					break;
				case EMsg.ClientStoreUserStatsResponse:
					HandleStoreUserStatsResponse(packetMsg);

					break;
			}

		}

		private void HandleGetUserStatsResponse(IPacketMsg packetMsg) {
			if (packetMsg == null) {
				ASF.ArchiLogger.LogNullError(nameof(packetMsg));
				return;
			}
				ClientMsgProtobuf<CMsgClientGetUserStatsResponse> response = new ClientMsgProtobuf<CMsgClientGetUserStatsResponse>(packetMsg);
				if (!Responses.TryAdd(response.Body.game_id,new StoredResponse{
					Success = response.Body.eresult == 1,
					Response = response.Body
				})){
					ASF.ArchiLogger.LogGenericError(string.Format(Strings.ErrorFailingRequest, "GetAchievements"));
				}

				if (response.Body.eresult != 1) {
					ASF.ArchiLogger.LogGenericError(string.Format(Strings.ErrorFailingRequest, "GetAchievements"));
				}

		}

		private void HandleStoreUserStatsResponse(IPacketMsg packetMsg) {
			if (packetMsg == null) {
				ASF.ArchiLogger.LogNullError(nameof(packetMsg));
				return;
			}
				ClientMsgProtobuf<CMsgClientStoreUserStatsResponse> response = new ClientMsgProtobuf<CMsgClientStoreUserStatsResponse>(packetMsg);
				if (!Responses.TryAdd(response.Body.game_id, new StoredResponse {
					Success = response.Body.eresult == 1,
					Response = null //we don't care about this, just need to know that request was successful
				})) {
					ASF.ArchiLogger.LogGenericError(string.Format(Strings.ErrorFailingRequest, "SetAchievements"));
				}

				if (response.Body.eresult != 1) {
					ASF.ArchiLogger.LogGenericError(string.Format(Strings.ErrorFailingRequest, "SetAchievements"));
				}

		}

		//Utilities

		private List<StatData> ParseResponse(CMsgClientGetUserStatsResponse Response) {
			List<StatData> result = new List<StatData>();
			KeyValue KeyValues = new KeyValue();
			if (Response.schema != null) {
				using (MemoryStream ms = new MemoryStream(Response.schema)) {
					if (!KeyValues.TryReadAsBinary(ms)) {
						ASF.ArchiLogger.LogGenericError(string.Format(Strings.ErrorIsInvalid, nameof(Response.schema)));
						return null;
					};
				}

				//first we enumerate all real achievements
				foreach (KeyValue stat in KeyValues.Children.Find(Child => Child.Name == "stats").Children) {
					if (stat.Children.Find(Child => Child.Name == "type").Value == "4") {
						foreach (KeyValue Achievement in stat.Children.Find(Child => Child.Name == "bits").Children) {
							int bitNum = int.Parse(Achievement.Name);
							uint statNum = uint.Parse(stat.Name);
							bool isSet = false;
							if ((Response.stats != null) && (Response.stats.Find(statElement => statElement.stat_id == int.Parse(stat.Name)) != null)) {
								isSet = (Response.stats.Find(statElement => statElement.stat_id == int.Parse(stat.Name)).stat_value & ((uint) 1 << int.Parse(Achievement.Name))) != 0;
							};

							bool restricted = Achievement.Children.Find(Child => Child.Name == "permission") != null;

							string dependancyName = (Achievement.Children.Find(Child => Child.Name == "progress") == null) ? "" : Achievement.Children.Find(Child => Child.Name == "progress").Children.Find(Child => Child.Name == "value").Children.Find(Child => Child.Name == "operand1").Value;

							uint dependancyValue = uint.Parse((Achievement.Children.Find(Child => Child.Name == "progress") == null) ? "0" : Achievement.Children.Find(Child => Child.Name == "progress").Children.Find(Child => Child.Name == "max_val").Value);
							string lang = CultureInfo.CurrentUICulture.EnglishName.ToLower();
							if (lang.IndexOf('(') > 0) {
								lang = lang.Substring(0, lang.IndexOf('(') - 1);
							}
							if (Achievement.Children.Find(Child => Child.Name == "display").Children.Find(Child => Child.Name == "name").Children.Find(Child => Child.Name == lang) == null) {
								lang = "english";//fallback to english
							}

							string name = Achievement.Children.Find(Child => Child.Name == "display").Children.Find(Child => Child.Name == "name").Children.Find(Child => Child.Name == lang).Value;

							result.Add(new StatData() {
								StatNum = statNum,
								BitNum = bitNum,
								IsSet = isSet,
								Restricted = restricted,
								DependancyValue = dependancyValue,
								DependancyName = dependancyName,
								Dependancy = 0,
								Name = name
							});
						}
					}
				}
				//Now we update all dependancies
				foreach (KeyValue stat in KeyValues.Children.Find(Child => Child.Name == "stats").Children) {
					if (stat.Children.Find(Child => Child.Name == "type").Value == "1") {
						uint statNum = uint.Parse(stat.Name);
						bool restricted = stat.Children.Find(Child => Child.Name == "permission") != null;
						string name = stat.Children.Find(Child => Child.Name == "name").Value;
						StatData ParentStat = result.Find(item => item.DependancyName == name);
						if (ParentStat != null) {
							ParentStat.Dependancy = statNum;
							if (restricted && !ParentStat.Restricted) {
								ParentStat.Restricted = true;
							}
						}
					}
				}
			}
			return result;
		}

		private void SetStat(List<CMsgClientStoreUserStats2.Stats> statsToSet, List<StatData> Stats, StoredResponse storedResponse, int achievementnum, bool set = true) {
			if (achievementnum < 0 || achievementnum > Stats.Count) {
				return; //it should never happen
			}
			CMsgClientStoreUserStats2.Stats currentstat = statsToSet.Find(stat => stat.stat_id == Stats[achievementnum].StatNum);
			if (currentstat == null) {
				currentstat = new CMsgClientStoreUserStats2.Stats() {
					stat_id = Stats[achievementnum].StatNum,
					stat_value = storedResponse.Response.stats.Find(stat => stat.stat_id == Stats[achievementnum].StatNum) != null ? storedResponse.Response.stats.Find(stat => stat.stat_id == Stats[achievementnum].StatNum).stat_value : 0
				};
				statsToSet.Add(currentstat);
			}
			if (set) {
				currentstat.stat_value = currentstat.stat_value | ((uint) 1 << Stats[achievementnum].BitNum);
			} else {
				currentstat.stat_value = currentstat.stat_value & ~((uint) 1 << Stats[achievementnum].BitNum);
			}
			if (Stats[achievementnum].DependancyName != "") {
				CMsgClientStoreUserStats2.Stats dependancystat = statsToSet.Find(stat => stat.stat_id == Stats[achievementnum].Dependancy);
				if (dependancystat == null) {
					dependancystat = new CMsgClientStoreUserStats2.Stats() {
						stat_id = Stats[achievementnum].Dependancy,
						stat_value = set ? Stats[achievementnum].DependancyValue : 0
					};
					statsToSet.Add(dependancystat);
				}
			}

		}

		//Endpoints

		internal string GetAchievements(Bot bot, ulong gameID) {

			if (!Client.IsConnected) {
				return Strings.BotNotConnected;
			}

			ClientMsgProtobuf<CMsgClientGetUserStats> request = new ClientMsgProtobuf<CMsgClientGetUserStats>(EMsg.ClientGetUserStats) {
				Body = {
					game_id =  gameID,
					steam_id_for_user = bot.SteamID
				}
			};

			Responses.TryRemove(gameID,out StoredResponse Dummy);

			Client.Send(request);
			SpinWait.SpinUntil(() => Responses.ContainsKey(gameID), TimeSpan.FromSeconds(ASF.GlobalConfig.ConnectionTimeout));
			//get stored data
			if (!Responses.TryGetValue(gameID, out StoredResponse response)) {
				return "Can't retrieve achievements for " + gameID.ToString();
			}
			if (!response.Success) {
				return "Can't retrieve achievements for " + gameID.ToString();
			}
			List<string> responses = new List<string>();
			List<StatData> Stats = ParseResponse(response.Response);
			if (Stats == null) {
				bot.ArchiLogger.LogNullError(nameof(Stats));
			} else if (Stats.Count == 0) {
				bot.ArchiLogger.LogNullError(nameof(Stats));
			} else {

				foreach (StatData stat in Stats) {
					responses.Add(string.Format("{0,-5}", Stats.IndexOf(stat) + 1) + (stat.IsSet ? "[\u2705] " : "[\u274C] ") + (stat.Restricted ? "\u26A0\uFE0F " : "") + stat.Name);
				}

			}
			return responses.Count > 0 ? "\u200B\nAchievements for " + gameID.ToString() + ":\n" + string.Join(Environment.NewLine, responses) : "Can't retrieve achievements for " + gameID.ToString();
		}

		internal string SetAchievements(Bot bot, uint appId, HashSet<uint> achievements, bool set = true) {
			if (!Client.IsConnected) {
				return Strings.BotNotConnected;
			}
			List<string> responses = new List<string>();
			string GetAchievementsResult = GetAchievements(bot, appId);
			if (!Responses.TryGetValue(appId, out StoredResponse response)) {
				return GetAchievementsResult;
			}
			if (!response.Success) {
				return GetAchievementsResult;
			}
			List<StatData> Stats = ParseResponse(response.Response);
			if (Stats == null){
				responses.Add(Strings.WarningFailed);
				return "\u200B\n" + string.Join(Environment.NewLine, responses);
			}

			List<CMsgClientStoreUserStats2.Stats> statsToSet = new List<CMsgClientStoreUserStats2.Stats>();

			if (achievements.Count == 0) { //if no parameters provided - set/reset all. Don't kill me Archi.
				for (int counter = 0; counter < Stats.Count; counter++) {
					if (!Stats[counter].Restricted) {
						SetStat(statsToSet, Stats, response, counter, set);
					}
				}
			} else {
				foreach (uint achievement in achievements) {
					if (Stats.Count < achievement) {
						responses.Add("Achievement #" + achievement.ToString() + " is out of range");
						continue;
					}
					if (Stats[(int) achievement - 1].IsSet == set) {
						responses.Add("Achievement #" + achievement.ToString() + " is already " + (set ? "unlocked" : "locked"));
						continue;
					}
					if (Stats[(int) achievement - 1].Restricted) {
						responses.Add("Achievement #" + achievement.ToString() + " is protected and can't be switched");
						continue;
					}
					SetStat(statsToSet, Stats, response, (int) achievement - 1, set);
				}
			}
			if (statsToSet.Count == 0) {
				responses.Add(Strings.WarningFailed);
				return "\u200B\n" + string.Join(Environment.NewLine, responses);
			};
			if (responses.Count > 0) {
				responses.Add("Trying to switch remaining achievements..."); //if some errors occured
			}
			ClientMsgProtobuf<CMsgClientStoreUserStats2> request = new ClientMsgProtobuf<CMsgClientStoreUserStats2>(EMsg.ClientStoreUserStats2) {
				Body = {
					game_id = (uint) appId,
					settor_steam_id = (ulong)bot.SteamID,
					settee_steam_id = (ulong)bot.SteamID,
					explicit_reset = false,
					crc_stats = response.Response.crc_stats
				}
			};
			request.Body.stats.AddRange(statsToSet);
			Responses.TryRemove(appId, out StoredResponse Dummy);
			Client.Send(request);
			SpinWait.SpinUntil(() => Responses.ContainsKey(appId), TimeSpan.FromSeconds(ASF.GlobalConfig.ConnectionTimeout));
			if (!Responses.TryGetValue(appId, out StoredResponse storeResponse)) {
				responses.Add(Strings.WarningFailed);
			}
			responses.Add(storeResponse.Success ? Strings.Success : Strings.WarningFailed);
			return "\u200B\n" + string.Join(Environment.NewLine, responses);
		}

	}

}
