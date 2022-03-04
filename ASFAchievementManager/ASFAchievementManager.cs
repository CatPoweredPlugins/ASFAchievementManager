using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Interaction;
using ArchiSteamFarm.Plugins.Interfaces;
using ArchiSteamFarm.Localization;
using SteamKit2;
using System.Linq;
using System.Collections.Concurrent;

namespace ASFAchievementManager {
	[Export(typeof(IPlugin))]
	public sealed class ASFAchievementManager : IBotSteamClient, IBotCommand2 {
		private static readonly ConcurrentDictionary<Bot, AchievementHandler> AchievementHandlers = new();
		public string Name => "ASF Achievement Manager";
		public Version Version => typeof(ASFAchievementManager).Assembly.GetName().Version ?? new Version("0");

		public Task OnLoaded() {
			ASF.ArchiLogger.LogGenericInfo("ASF Achievement Manager Plugin by Ryzhehvost, powered by ginger cats");
			return Task.CompletedTask;
		}

		public async Task<string?> OnBotCommand(Bot bot, EAccess _, string message, string[] args, ulong steamID = 0) {
			switch (args.Length) {
				case 0:
					bot.ArchiLogger.LogNullError(nameof(args));

					return null;
				case 1:
					return args[0].ToUpperInvariant() switch {
						_ => null,
					};
				default:
					return args[0].ToUpperInvariant() switch {
						"ALIST" when args.Length > 2 => await ResponseAchievementList(steamID, args[1], Utilities.GetArgsAsText(args, 2, ",")).ConfigureAwait(false),
						"ALIST" => await ResponseAchievementList(steamID, bot, args[1]).ConfigureAwait(false),
						"ASET" when args.Length > 3 => await ResponseAchievementSet(steamID, args[1], args[2], Utilities.GetArgsAsText(args, 3, ","), true).ConfigureAwait(false),
						"ASET" when args.Length > 2 => await ResponseAchievementSet(steamID, bot, args[1], Utilities.GetArgsAsText(args, 2, ","), true).ConfigureAwait(false),
						"ARESET" when args.Length > 3 => await ResponseAchievementSet(steamID, args[1], args[2], Utilities.GetArgsAsText(args, 3, ","), false).ConfigureAwait(false),
						"ARESET" when args.Length > 2 => await ResponseAchievementSet(steamID, bot, args[1], Utilities.GetArgsAsText(args, 2, ","), false).ConfigureAwait(false),
						_ => null,
					};
			}
		}

		public Task OnBotSteamCallbacksInit(Bot bot, CallbackManager callbackManager) => Task.CompletedTask;

		public Task<IReadOnlyCollection<ClientMsgHandler>?> OnBotSteamHandlersInit(Bot bot) {
			AchievementHandler CurrentBotAchievementHandler = new();
			AchievementHandlers.TryAdd(bot, CurrentBotAchievementHandler);
			return Task.FromResult<IReadOnlyCollection<ClientMsgHandler>?>(new HashSet<ClientMsgHandler> { CurrentBotAchievementHandler });
		}

		//Responses

		private static async Task<string?> ResponseAchievementList(ulong steamID, Bot bot, string appids) {
			if (bot.GetAccess(steamID) < EAccess.Master) {
				return null;
			}

			string[] gameIDs = appids.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

			if (gameIDs.Length == 0) {
				return bot.Commands.FormatBotResponse(string.Format(Strings.ErrorIsEmpty, nameof(gameIDs)));
			}
			if (AchievementHandlers.TryGetValue(bot, out AchievementHandler? AchievementHandler)) {
				if (AchievementHandler == null) {
					bot.ArchiLogger.LogNullError(nameof(AchievementHandler));
					return null;
				}

				HashSet<uint> gamesToGetAchievements = new();

				foreach (string game in gameIDs) {
					if (!uint.TryParse(game, out uint gameID) || (gameID == 0)) {
						return bot.Commands.FormatBotResponse(string.Format(Strings.ErrorParsingObject, nameof(gameID)));
					}

					gamesToGetAchievements.Add(gameID);
				}


				IList<string> results = await Utilities.InParallel(gamesToGetAchievements.Select(appID => Task.Run<string>(() => AchievementHandler.GetAchievements(bot, appID)))).ConfigureAwait(false);

				List<string> responses = new(results.Where(result => !string.IsNullOrEmpty(result)));

				return responses.Count > 0 ? bot.Commands.FormatBotResponse(string.Join(Environment.NewLine, responses)) : null;

			} else {

				return bot.Commands.FormatBotResponse(string.Format(Strings.ErrorIsEmpty, nameof(AchievementHandlers)));
			}

		}

		private static async Task<string?> ResponseAchievementList(ulong steamID, string botNames, string appids) {

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return Commands.FormatStaticResponse(string.Format(Strings.BotNotFound, botNames));
			}

			IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseAchievementList(steamID, bot, appids))).ConfigureAwait(false);

			List<string?> responses = new(results.Where(result => !string.IsNullOrEmpty(result)));

			return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
		}


		private static async Task<string?> ResponseAchievementSet(ulong steamID, Bot bot, string appid, string achievementNumbers, bool set = true) {
			if (bot.GetAccess(steamID) < EAccess.Master) {
				return null;
			}

			if (string.IsNullOrEmpty(achievementNumbers)) {
				return bot.Commands.FormatBotResponse(string.Format(Strings.ErrorObjectIsNull, nameof(achievementNumbers)));
			}
			if (!uint.TryParse(appid, out uint appId)) {
				return bot.Commands.FormatBotResponse(string.Format(Strings.ErrorIsInvalid, nameof(appId)));
			}

			if (!AchievementHandlers.TryGetValue(bot, out AchievementHandler? AchievementHandler)) {
				return bot.Commands.FormatBotResponse(string.Format(Strings.ErrorIsEmpty, nameof(AchievementHandlers)));
			}

			if (AchievementHandler == null) {
				bot.ArchiLogger.LogNullError(nameof(AchievementHandler));
				return null;
			}

			HashSet<uint> achievements = new();

			string[] achievementStrings = achievementNumbers.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

			if (!achievementNumbers.Equals("*")) {
				foreach (string achievement in achievementStrings) {
					if (!uint.TryParse(achievement, out uint achievementNumber) || (achievementNumber == 0)) {
						return bot.Commands.FormatBotResponse(string.Format(Strings.ErrorParsingObject, achievement));
					}

					achievements.Add(achievementNumber);
				}
				if (achievements.Count == 0) {
					return bot.Commands.FormatBotResponse(string.Format(Strings.ErrorIsEmpty, "Achievements list"));
				}
			}
			return bot.Commands.FormatBotResponse(await Task.Run<string>(() => AchievementHandler.SetAchievements(bot, appId, achievements, set)).ConfigureAwait(false));
		}

		private static async Task<string?> ResponseAchievementSet(ulong steamID, string botNames, string appid, string achievementNumbers, bool set = true) {

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return Commands.FormatStaticResponse(string.Format(Strings.BotNotFound, botNames));
			}

			IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseAchievementSet(steamID, bot, appid, achievementNumbers, set))).ConfigureAwait(false);

			List<string?> responses = new(results.Where(result => !string.IsNullOrEmpty(result)));

			return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
		}

	}

}
