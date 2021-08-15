using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace jiratg_bot
{
    public class Handlers
    {
        private static readonly Dictionary<long, BotFsm> _fsms = new();
        public static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception,
            CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException =>
                    $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
            CancellationToken cancellationToken)
        {
            var handler = update.Type switch
            {
                // UpdateType.Unknown:
                // UpdateType.ChannelPost:
                // UpdateType.EditedChannelPost:
                // UpdateType.ShippingQuery:
                // UpdateType.PreCheckoutQuery:
                // UpdateType.Poll:
                // UpdateType.EditedMessage
                // UpdateType.InlineQuery
                // ChosenInlineResult
                UpdateType.Message => BotUpdate(botClient, update),
                UpdateType.CallbackQuery => BotUpdate(botClient, update),
                _ => UnknownUpdateHandlerAsync(botClient, update)
            };

            try
            {
                await handler;
            }
            catch (Exception exception)
            {
                await HandleErrorAsync(botClient, exception, cancellationToken);
            }
        }

        private static async Task BotUpdate(ITelegramBotClient botClient, Update update)
        {
            var userId = ExtractUserId(update);
            _fsms.TryGetValue(userId, out var fsm);
            if (fsm == null)
            {
                var chatId = ExtractChatId(update);
                fsm = new BotFsm(botClient, chatId);
                _fsms[userId] = fsm;
                await fsm.Transition(MyStates.Init);
            }
            await fsm.StateDo(update);
        }

        private static long ExtractUserId(Update update)
        {
            return update.Type switch
            {
                UpdateType.Message => update.Message.From.Id,
                UpdateType.CallbackQuery => update.CallbackQuery.From.Id,
                _ => -1
            };
        }

        private static long ExtractChatId(Update update)
        {
            return update.Type switch
            {
                UpdateType.Message => update.Message.Chat.Id,
                UpdateType.CallbackQuery => update.CallbackQuery.Message.From.Id,
                _ => -1
            };
        }

        private static Task UnknownUpdateHandlerAsync(ITelegramBotClient botClient, Update update)
        {
            Console.WriteLine($"Unknown update type: {update.Type}");
            return Task.CompletedTask;
        }
    }
}