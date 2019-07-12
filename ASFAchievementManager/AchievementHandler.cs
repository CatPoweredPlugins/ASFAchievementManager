using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ArchiSteamFarm;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.NLog;
using SteamKit2;
using SteamKit2.Internal;
using Newtonsoft.Json;
using System.Globalization;

namespace ASFAchievementManager {
	public sealed class AchievementHandler : ClientMsgHandler {
		List<StatData> Stats = new List<StatData>();
		CMsgClientGetUserStatsResponse OldResponse = null;
		bool Success;
		private readonly SemaphoreSlim AchievementSemaphore = new SemaphoreSlim(1, 1);

		public override void HandleMsg(IPacketMsg packetMsg) {
			if (packetMsg == null) {
				ASF.ArchiLogger.LogNullError(nameof(packetMsg));

				return;
			}

			//LastPacketReceived = DateTime.UtcNow;

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
				AchievementSemaphore.Release();
				return;
			}
			if (AchievementSemaphore.CurrentCount == 0) {
				ClientMsgProtobuf<CMsgClientGetUserStatsResponse> response = new ClientMsgProtobuf<CMsgClientGetUserStatsResponse>(packetMsg);
				//store data
				OldResponse = response.Body;
				Success = response.Body.eresult == 1;
				if (response.Body.eresult != 1) {
					ASF.ArchiLogger.LogGenericError(string.Format(Strings.ErrorFailingRequest, "GetUserStatsResponse"));
					AchievementSemaphore.Release();
					return;
				}
				KeyValue KeyValues = new KeyValue();
				if (response.Body.schemaSpecified && response.Body.schema != null) {
					using (MemoryStream ms = new MemoryStream(response.Body.schema)) {
						if (!KeyValues.TryReadAsBinary(ms)) {
							ASF.ArchiLogger.LogGenericError(string.Format(Strings.ErrorIsInvalid, nameof(response.Body.schema)));
							Success = false;
							AchievementSemaphore.Release();
							return;
						};
					}

					//first we enumerate all real achievements
					foreach (var stat in KeyValues.Children.Find(Child => Child.Name == "stats").Children) {
						if (stat.Children.Find(Child => Child.Name == "type").Value == "4") {
							foreach (var Achievement in stat.Children.Find(Child => Child.Name == "bits").Children) {
								int bitNum = int.Parse(Achievement.Name);
								uint statNum = uint.Parse(stat.Name);
								bool isSet = false;
								if ((response.Body.stats != null) && (response.Body.stats.Find(statElement => statElement.stat_id == int.Parse(stat.Name)) != null)) {
									isSet = (response.Body.stats.Find(statElement => statElement.stat_id == int.Parse(stat.Name)).stat_value & ((uint) 1 << int.Parse(Achievement.Name))) != 0;
								};

								bool restricted = Achievement.Children.Find(Child => Child.Name == "permission") != null;

								string dependancyName = (Achievement.Children.Find(Child => Child.Name == "progress") == null) ? "" : Achievement.Children.Find(Child => Child.Name == "progress").Children.Find(Child => Child.Name == "value").Children.Find(Child => Child.Name == "operand1").Value;

								uint dependancyValue = uint.Parse((Achievement.Children.Find(Child => Child.Name == "progress") == null) ? "0" : Achievement.Children.Find(Child => Child.Name == "progress").Children.Find(Child => Child.Name == "max_val").Value);
								string lang = CultureInfo.CurrentUICulture.EnglishName.ToLower();
								if (lang.IndexOf('(') > 0) {
									lang = lang.Substring(0, lang.IndexOf('(')-1);
								}
								if (Achievement.Children.Find(Child => Child.Name == "display").Children.Find(Child => Child.Name == "name").Children.Find(Child => Child.Name == lang) == null) {
									lang = "english";//fallback to english
								}

								string name = Achievement.Children.Find(Child => Child.Name == "display").Children.Find(Child => Child.Name == "name").Children.Find(Child => Child.Name == lang).Value;

								Stats.Add(new StatData() {
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
					foreach (var stat in KeyValues.Children.Find(Child => Child.Name == "stats").Children) {
						if (stat.Children.Find(Child => Child.Name == "type").Value == "1") {
							uint statNum = uint.Parse(stat.Name);
							bool restricted = stat.Children.Find(Child => Child.Name == "permission") != null;
							string name = stat.Children.Find(Child => Child.Name == "name").Value;
							StatData ParentStat = Stats.Find(item => item.DependancyName == name);
							if (ParentStat != null) {
								ParentStat.Dependancy = statNum;
								if (restricted && !ParentStat.Restricted) {
									ParentStat.Restricted = true;
								}
							}
						}
					}
				}
				AchievementSemaphore.Release();
			}
		}

		private void HandleStoreUserStatsResponse(IPacketMsg packetMsg) {
			if (packetMsg == null) {
				ASF.ArchiLogger.LogNullError(nameof(packetMsg));
				AchievementSemaphore.Release();
				return;
			}
			if (AchievementSemaphore.CurrentCount == 0) {
				ClientMsgProtobuf<CMsgClientStoreUserStatsResponse> response = new ClientMsgProtobuf<CMsgClientStoreUserStatsResponse>(packetMsg);
				string json = JsonConvert.SerializeObject(response.Body);
				if (response.Body.eresult != 1) {
					ASF.ArchiLogger.LogGenericError(string.Format(Strings.ErrorFailingRequest, "StoreUserStatsResponse"));
					AchievementSemaphore.Release();
					return;
				}
				//if eresult is
				Success = true;
				AchievementSemaphore.Release();
			}

		}

		//Utilities

		private async void TimeoutRelease (TimeSpan timeout) {
			await Task.Delay(timeout).ConfigureAwait(false);
			if (AchievementSemaphore.CurrentCount == 0) {
				Success = false;
				AchievementSemaphore.Release();
			}
		}

		private void SetStat(List<CMsgClientStoreUserStats2.Stats> statsToSet, int achievementnum, bool set = true) {
			if (achievementnum < 0 || achievementnum > Stats.Count) {
				return; //it should never happen
			}
			CMsgClientStoreUserStats2.Stats currentstat = statsToSet.Find(stat => stat.stat_id == Stats[achievementnum].StatNum);
			if (currentstat == null) {
				currentstat = new CMsgClientStoreUserStats2.Stats() {
					stat_id = Stats[achievementnum].StatNum,
					stat_value = OldResponse.stats.Find(stat => stat.stat_id == Stats[achievementnum].StatNum) != null ? OldResponse.stats.Find(stat => stat.stat_id == Stats[achievementnum].StatNum).stat_value : 0
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
		internal async Task<string> GetAchievements(Bot bot, uint gameID) {

			if (!Client.IsConnected) {
				return Strings.BotNotConnected;
			}

			await AchievementSemaphore.WaitAsync().ConfigureAwait(false);
			Stats.Clear();
			ClientMsgProtobuf<CMsgClientGetUserStats> request = new ClientMsgProtobuf<CMsgClientGetUserStats>(EMsg.ClientGetUserStats) {
				Body = {
					game_id = (uint) gameID,
					steam_id_for_user = (ulong) bot.SteamID
				}
			};

			TimeoutRelease(new TimeSpan(0, 0, ASF.GlobalConfig.ConnectionTimeout));
			Client.Send(request);
			await AchievementSemaphore.WaitAsync().ConfigureAwait(false);
			//get stored data
			if (!Success) {
				return "Can not retrieve achievements for " + gameID.ToString();
			}
			List<string> responses = new List<string>();
			if (Stats == null) {
				bot.ArchiLogger.LogNullError(nameof(Stats));
			} else if (Stats.Count == 0) {
				bot.ArchiLogger.LogNullError(nameof(Stats));
			} else {

				foreach (var stat in Stats) {
					responses.Add(string.Format("{0,-5}", Stats.IndexOf(stat) + 1) + (stat.IsSet ? "[\u2705] " : "[\u274C] ") + (stat.Restricted ? "\u26A0\uFE0F " : "") + stat.Name);
				}

			}
			AchievementSemaphore.Release();
			return responses.Count > 0 ? "\u200B\nAchievemens for " + gameID.ToString() + ":\n" + string.Join(Environment.NewLine, responses) : "Can not retrieve achievements for " + gameID.ToString();
		}

		internal async Task<string> SetAchievements(Bot bot, uint appId, HashSet<uint> achievements, bool set = true) {
			if (!Client.IsConnected) {
				return Strings.BotNotConnected;
			}
			List<string> responses = new List<string>();
			string GetAchievementsResult = await GetAchievements(bot, appId).ConfigureAwait(false);
			if (OldResponse == null) {
				return GetAchievementsResult;
			}
			if (OldResponse.eresult != 1) {
				return GetAchievementsResult;
			}
			await AchievementSemaphore.WaitAsync().ConfigureAwait(false);
			List<CMsgClientStoreUserStats2.Stats> statsToSet = new List<CMsgClientStoreUserStats2.Stats>();
			if (achievements.Count == 0) { //if no parameters provided - set/reset all. Don't kill me Archi.
				for (int counter = 0; counter < Stats.Count; counter++) {
					SetStat(statsToSet, counter, set);
				}
			} else {
				foreach (var ahcievement in achievements) {
					if (Stats.Count < ahcievement) {
						responses.Add("Achievement #" + ahcievement.ToString() + " is out of range");
						continue;
					}
					if (Stats[(int) ahcievement - 1].IsSet == set) {
						responses.Add("Achievement #" + ahcievement.ToString() + " is already " + (set ? "unlocked" : "locked"));
						continue;
					}
					if (Stats[(int) ahcievement - 1].Restricted) {
						responses.Add("Achievement #" + ahcievement.ToString() + " is protected and can't be switched");
						continue;
					}
					SetStat(statsToSet, (int) ahcievement - 1, set);
				}
			}
			if (statsToSet == null) {
				responses.Add(Strings.WarningFailed);
				AchievementSemaphore.Release();
				return "\u200B\n" + string.Join(Environment.NewLine, responses);
			};
			if (statsToSet.Count == 0) {
				responses.Add(Strings.WarningFailed);
				AchievementSemaphore.Release();
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
					crc_stats = OldResponse.crc_stats
				}
			};
			request.Body.stats.AddRange(statsToSet);
			//string json = JsonConvert.SerializeObject(request.Body);
			Success = false;
			TimeoutRelease(new TimeSpan(0, 0, ASF.GlobalConfig.ConnectionTimeout));
			Client.Send(request);			
			await AchievementSemaphore.WaitAsync().ConfigureAwait(false);
			responses.Add(Success ? Strings.Success : Strings.WarningFailed);
			AchievementSemaphore.Release();
			return "\u200B\n" + string.Join(Environment.NewLine, responses);
		}

	}


}
