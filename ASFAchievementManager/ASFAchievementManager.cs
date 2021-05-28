using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Interaction;
using ArchiSteamFarm.Steam.Storage;
using ArchiSteamFarm.Plugins.Interfaces;
using ArchiSteamFarm.Localization;
using JetBrains.Annotations;
using SteamKit2;
using System.Linq;
using System.Collections.Concurrent;

namespace ASFAchievementManager {
	[Export(typeof(IPlugin))]
	public sealed class ASFAchievementManager : IBotSteamClient, IBotCommand {
		private static ConcurrentDictionary<Bot, AchievementHandler> AchievementHandlers = new ConcurrentDictionary<Bot, AchievementHandler>();
		public string Name => "ASF Achievement Manager";
		public Version Version => typeof(ASFAchievementManager).Assembly.GetName().Version ?? new Version("0");

		public void OnLoaded() => ASF.ArchiLogger.LogGenericInfo("ASF Achievement Manager Plugin by Ryzhehvost, powered by ginger cats");

		public async Task<string?> OnBotCommand([NotNull] Bot bot, ulong steamID, [NotNull] string message, string[] args) {

			switch (args.Length) {
				case 0:
					bot.ArchiLogger.LogNullError(nameof(args));

					return null;
				case 1:
					switch (args[0].ToUpperInvariant()) {

						default:
							return null;
					}
				default:
					switch (args[0].ToUpperInvariant()) {
						case "ALIST" when args.Length > 2:
							return await ResponseAchievementList(steamID, args[1], Utilities.GetArgsAsText(args, 2, ",")).ConfigureAwait(false);
						case "ALIST":
							return await ResponseAchievementList(steamID, bot, args[1]).ConfigureAwait(false);
						case "ASET" when args.Length > 3:
							return await ResponseAchievementSet(steamID, args[1], args[2], Utilities.GetArgsAsText(args, 3, ","), true).ConfigureAwait(false);
						case "ASET" when args.Length > 2:
							return await ResponseAchievementSet(steamID, bot, args[1], Utilities.GetArgsAsText(args, 2, ","), true).ConfigureAwait(false);
						case "ARESET" when args.Length > 3:
							return await ResponseAchievementSet(steamID, args[1], args[2], Utilities.GetArgsAsText(args, 3, ","), false).ConfigureAwait(false);
						case "ARESET" when args.Length > 2:
							return await ResponseAchievementSet(steamID, bot, args[1], Utilities.GetArgsAsText(args, 2, ","), false).ConfigureAwait(false);
						default:
							return null;
					}
			}
		}

		public void OnBotSteamCallbacksInit([NotNull] Bot bot, [NotNull] CallbackManager callbackManager) { }

		public IReadOnlyCollection<ClientMsgHandler> OnBotSteamHandlersInit([NotNull] Bot bot) {
			AchievementHandler CurrentBotAchievementHandler = new AchievementHandler();
			AchievementHandlers.TryAdd(bot, CurrentBotAchievementHandler);
			return new HashSet<ClientMsgHandler> { CurrentBotAchievementHandler };
		}

		//Responses

		private static async Task<string?> ResponseAchievementList(ulong steamID, Bot bot, string appids) {

			if (!bot.HasAccess(steamID, BotConfig.EAccess.Master)) {
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

				HashSet<uint> gamesToGetAchievements = new HashSet<uint>();

				foreach (string game in gameIDs) {
					if (!uint.TryParse(game, out uint gameID) || (gameID == 0)) {
						return bot.Commands.FormatBotResponse(string.Format(Strings.ErrorParsingObject, nameof(gameID)));
					}

					gamesToGetAchievements.Add(gameID);
				}


				IList<string> results = await Utilities.InParallel(gamesToGetAchievements.Select(appID => Task.Run<string>(() => AchievementHandler.GetAchievements(bot, appID)))).ConfigureAwait(false);

				List<string> responses = new List<string>(results.Where(result => !string.IsNullOrEmpty(result)));

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

			List<string?> responses = new List<string?>(results.Where(result => !string.IsNullOrEmpty(result)));

			return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
		}


		private static async Task<string?> ResponseAchievementSet(ulong steamID, Bot bot, string appid, string AchievementNumbers, bool set = true) {
			if (!bot.HasAccess(steamID, BotConfig.EAccess.Master)) {
				return null;
			}

			if (string.IsNullOrEmpty(AchievementNumbers)) {
				return bot.Commands.FormatBotResponse(string.Format(Strings.ErrorObjectIsNull, nameof(AchievementNumbers)));
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

			HashSet<uint> achievements = new HashSet<uint>();

			string[] achievementStrings = AchievementNumbers.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

			if (!AchievementNumbers.Equals("*")) {
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

		private static async Task<string?> ResponseAchievementSet(ulong steamID, string botNames, string appid, string AchievementNumbers, bool set = true) {

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return Commands.FormatStaticResponse(string.Format(Strings.BotNotFound, botNames));
			}

			IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseAchievementSet(steamID, bot, appid, AchievementNumbers, set))).ConfigureAwait(false);

			List<string?> responses = new List<string?>(results.Where(result => !string.IsNullOrEmpty(result)));

			return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
		}

	}

}
