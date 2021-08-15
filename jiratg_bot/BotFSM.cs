using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace jiratg_bot
{

    enum MyStates
    {
        Init,
        WaitForType,
        WaitForHeader,
        WaitForBoard,
        WaitForSeverity,
        WaitForAssignee,
    };
    
    
    /*
     * FSM для бота.
     * Init -> WaitForType -> WaitForBoard -> WaitForSeverity -> WaitForAssignee -> WaitForHeader -> Init 
     */
    class BotFsm : FSM<MyStates>
    {
        
        private readonly ITelegramBotClient _bot;
        private readonly long _chatId;
        private int? _menuMessageId;
        private string _body;
        private string _header;
        private string _board;
        private string _severity;
        private string _assignee;
        private string _type;

        public BotFsm(ITelegramBotClient bot, long chatId) : base(MyStates.Init)
        {
            _bot = bot;
            _chatId = chatId;
        }

       
        async Task InitState(Update update)
        {
            if (update.Type != UpdateType.Message)
            {
                return;
            }

            var message = update.Message;
            if (message.Text != "/todo")
                return;
            if (message.ReplyToMessage == null)
            {
                await _bot.SendTextMessageAsync(
                    message.Chat.Id, "Нужен реплай на сообщение, из которого делается таск",
                    replyToMessageId: message.MessageId
                );
                return;
            }
            _body = message.ReplyToMessage.Text;
            await _bot.SendTextMessageAsync(message.Chat.Id,
                "Я сейчас задам несколько вопросов и создам таск в жире.",
                replyToMessageId: message.MessageId);
            await Transition(MyStates.WaitForType);
        }

        async Task WaitForTypeState(Update update)
        {
            if (update.Type != UpdateType.CallbackQuery)
                return;
            var callbackQuery = update.CallbackQuery;
            _type = callbackQuery.Data;

            await Transition(MyStates.WaitForBoard);
        }

        async Task WaitForBoardState(Update update)
        {
            if (update.Type != UpdateType.CallbackQuery)
                return;
            var callbackQuery = update.CallbackQuery;
            _board = callbackQuery.Data;

            await Transition(MyStates.WaitForSeverity);
        }

        async Task WaitForSeverityState(Update update)
        {
            if (update.Type != UpdateType.CallbackQuery)
                return;
            var callbackQuery = update.CallbackQuery;

            _severity = callbackQuery.Data;
            
            await Transition(MyStates.WaitForAssignee);
        }

        async Task WaitForAssigneeState(Update update)
        {
            if (update.Type != UpdateType.CallbackQuery)
                return;
            var callbackQuery = update.CallbackQuery;
            _assignee = callbackQuery.Data;

            await Transition(MyStates.WaitForHeader);
        }

        async Task WaitForHeaderState(Update update)
        {
            var message = update.Message;
            _header = message.Text;

            await Transition(MyStates.Init);
        }
        

        // ################################################ 
        // transitions.
        // методы для перехода в какое-то состояние. По-большому счёту, просто отправляем какой-то вопрос
        // ################################################

        async Task WaitForTypeTransition(MyStates prev)
        {
            await SimplyMenuTransition("Баг или таска?", new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🐛 Bug", "Bug"),
                    InlineKeyboardButton.WithCallbackData("✔️ Task", "Task"),
                }
            }));
        }

        async Task WaitForBoardTransition(MyStates prev)
        {
            await SimplyMenuTransition("На какую борду кладём?", new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Backend", "BAC"),
                    InlineKeyboardButton.WithCallbackData("Frontend", "FRONT"),
                    InlineKeyboardButton.WithCallbackData("Devops", "DEVOPS"),
                }
            }));
        }

        async Task WaitForSeverityTransition(MyStates prev)
        {
            await SimplyMenuTransition("Какая срочность?", new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("😴 Lowest", "Lowest"),
                    InlineKeyboardButton.WithCallbackData("🥱 Low", "Low"),
                    InlineKeyboardButton.WithCallbackData("Medium", "Medium"),
                    InlineKeyboardButton.WithCallbackData("🏃‍♂️ High", "High"),
                    InlineKeyboardButton.WithCallbackData("🔥 Highest", "Highest"),
                }
            }));
        }

        async Task WaitForAssigneeTransition(MyStates prev)
        {
            await SimplyMenuTransition("На кого назначить?", new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Ни на кого", "None"),
                    InlineKeyboardButton.WithCallbackData("А", "A"),
                    InlineKeyboardButton.WithCallbackData("B", "B"),
                    InlineKeyboardButton.WithCallbackData("C", "C"),
                    InlineKeyboardButton.WithCallbackData("D", "D"),
                }
            }));
        }

        async Task WaitForHeaderTransition(MyStates prev)
        {
            await RemoveInlineKeyboard("Заголовок сообщения?");
        }

        async Task InitTransition(MyStates prev)
        {
            if (prev == MyStates.Init)
                return;

            await _bot.SendTextMessageAsync(_chatId,
                $"Создаём {_type}: {_body} {_header} {_assignee} {_board} {_severity}");
            _body = string.Empty;
            _header = string.Empty;
            _board = string.Empty;
            _severity = string.Empty;
            _assignee = string.Empty;
            _type = string.Empty;
        }

        // ################################################ 
        // всякое
        // ################################################
        private async Task SimplyMenuTransition(string text, InlineKeyboardMarkup inlineKeyboard)
        {
            if (_menuMessageId.HasValue)
            {
                await _bot.EditMessageTextAsync(_chatId, _menuMessageId.Value, text, replyMarkup: inlineKeyboard);
            }
            else
            {
                var msg = await _bot.SendTextMessageAsync(_chatId, text, replyMarkup: inlineKeyboard);
                _menuMessageId = msg.MessageId;
            }
        }

        private async Task RemoveInlineKeyboard(string text)
        {
            if (_menuMessageId.HasValue)
            {
                var msg = await _bot.EditMessageTextAsync(_chatId, _menuMessageId.Value, text, replyMarkup: null);
                _menuMessageId = null;
            }
            else
            {
                var msg = await _bot.SendTextMessageAsync(_chatId, text);

            }
        }
    }
}