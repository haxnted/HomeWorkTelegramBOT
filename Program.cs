using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace HomeWorkHub
{
    public sealed class Program
    {
        static private PdfConverter _pdfConverter = new PdfConverter();
        static private Stack<Person> _whiteListUsers = new Stack<Person>();
        static private FileManagement _fileManagement = new FileManagement();
        static private UserDataBase _userDataBase = new UserDataBase();
        static private UserInfo? _userInfo;
        static private  TelegramBotClient? _client;
        static private readonly DateTime _date = DateTime.Now;
        private static void Main(string[] args)
        {
            _userInfo = new UserInfo(_fileManagement, _userDataBase);

            _client = new TelegramBotClient(_fileManagement.ApiKey);

            _client.StartReceiving(UpdateAsync, ErrorAsync);

            ProcessUserValidate();
            Console.ReadLine();
        }

        private static void ProcessUserValidate()
        {
            while (true)
            {
                if (_whiteListUsers.Count > 0)
                {
                    Person person = _whiteListUsers.Peek();
                    Print($"Пользователь {person} Подал на регистрацию. Добавить? (y - да, n - нет)", ConsoleColor.Red);

                    char answer;
                    string text = "";

                    do
                    {
                        Console.Write("Введите y для одобрения или n для отклонения: ");
                        answer = char.ToLower(Console.ReadKey().KeyChar);

                        if (answer == 'y')
                        {
                            text = "__Вы были одобрены. Введите команду__ /start \U0001F917";
                            _userDataBase.AddUser(person);
                            _userInfo?.AddToUserList(person.Id);
                        }
                        else if (answer == 'n')
                        {
                            text = "__Ваша заявка была отклонена__ \U0001F636";
                            _userInfo?.AddToBanList(person.Id);
                        }
                        else
                        {
                            Console.WriteLine("\nПожалуйста, введите 'y' или 'n'.");
                        }
                    } while (answer != 'y' && answer != 'n');

                    _client?.SendTextMessageAsync(person.Id, text, parseMode: ParseMode.Markdown);
                    //delete from whitelist
                    _whiteListUsers.Pop();
                    Console.WriteLine();
                }
                Thread.Sleep(4000);
            }
        }
        private async static Task UpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken token)
        {
            if (update == null || _userInfo == null)
                return;

            Message message = update.Message;
            if (message != null)
            {
                if (_userInfo.IsUserBanned(message.Chat.Id))
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Вы добавлены в черный список.");
                    return;
                }
                //cheсk is user new or banned
                if (message.Text == "/register" && !_userInfo.IsUserBanned(message.Chat.Id))
                {
                    if (_userInfo.IsUserExist(message.Chat.Id))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Вы уже добавлены.");
                        return;
                    }
                    _whiteListUsers.Push(new Person(message.Chat.Id, message.Chat.FirstName ?? "", message.Chat.LastName ?? ""));
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Ожидайте! Мы сообщим, когда вашу заявку рассмотрят");
                    return;
                }
                if (!_userInfo.IsUserExist(message.Chat.Id))
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Похоже вы новый пользователь, введите команду /register \U0001F609");
                    return;
                }

                if (message.Type == MessageType.Photo)
                {
                    _pdfConverter.AddPhoto(message.Chat.Id, message);
                    // You can remove photo deletion (done in order not to clutter the correspondence with the bot) or sending message if photo is added
                    //await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Фотография добавлена.");
                }
                if (message.Type == MessageType.Document)
                {
                    Print($"Id: {message.Chat.Id} FCs: {message.Chat.FirstName} {message.Chat.LastName} Text: {message?.Document?.FileName} Time: {DateTime.Now}", ConsoleColor.White);
                    await ShooseDocumentCommandAsync(botClient, update, message ?? throw new NullReferenceException("class Message - null"));
                }
                if (message.Type == MessageType.Text)
                {
                    Print($"Id: {message.Chat.Id} FCs: {message.Chat.FirstName} {message.Chat.LastName} Text: {message?.Text} Time: {DateTime.Now}", ConsoleColor.White);
                    await ShooseTextCommandAsync(botClient, message ?? throw new NullReferenceException("class Message - null"), token);
                }
            }
            if (update != null)
            {
                //if user getting file from message bot
                if (update.CallbackQuery != null)
                {
                    Print($"Id: {update.Id} CallBack Text: {update?.CallbackQuery?.Message?.Text} Time: {DateTime.Now}", ConsoleColor.White);
                    await ShooseCallBackQuery(botClient, update, token);
                }
            }
        }
        async private static Task ErrorAsync(ITelegramBotClient arg1, Exception exp, CancellationToken arg3)
        {
            Print($"Exeption: {exp.Message}, StackTrace: {exp.StackTrace}", ConsoleColor.Red);
            await arg1.SendTextMessageAsync(_fileManagement.ID_MAIN_ADMIN, $"Произошла ошибка, детали в CMD");
        }
        async static private Task ShooseCallBackQuery(ITelegramBotClient botClient, Update? update, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return;
            
            if (update?.CallbackQuery != null && update != null)
            {

                if (update?.CallbackQuery?.Data?.ToLower() == "последние 3 файла" && update.CallbackQuery.Message != null)
                {
                    List<string> homeworks = _fileManagement.GetListHomeWorks();
                    int count = homeworks.Count;
                    if (homeworks.Count <= 2)
                    {
                        await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, $"В хранилище только {homeworks.Count} файлов \U0001F611");
                        
                        for (int i = count - 1; i >= 0; i--)
                            await SendDocumentAsync(update, botClient, homeworks[i]);
                    }
                    else
                    {
                        for (int i = count - 1; i >= count - 3 && i >= 0; i--)
                        {
                            await SendDocumentAsync(update, botClient, homeworks[i]);
                        }
                    }
                }
                else if (update != null && update.CallbackQuery.Data != null && _fileManagement.IsHomeWorkContains(update.CallbackQuery.Data))
                    await SendDocumentAsync(update, botClient, update.CallbackQuery.Data);
            }

            return;
        }
        async static private Task SendDocumentAsync(Update update, ITelegramBotClient botClient, string filename)
        {
            if (update?.CallbackQuery?.Message != null)
            {
                await using FileStream stream = System.IO.File.OpenRead(_fileManagement.GetPathFile(filename));
                await botClient.SendDocumentAsync(update.CallbackQuery.Message.Chat.Id, new InputFileStream(stream, filename));
            }
        }
        async static private Task ShooseDocumentCommandAsync(ITelegramBotClient botClient, Update update, Message message)
        {
            if (message.Document != null && message.Document.FileName != null && _userInfo != null && update?.Message?.Document?.FileId != null)
            {
                var fileInfo = await botClient.GetFileAsync(update.Message.Document.FileId);
                string filePath = fileInfo.FilePath ?? throw new Exception("file path null");
                string destinationFilePath = $@"{_fileManagement.pathHomeWork}\{message.Document.FileName}";
                
                if (System.IO.File.Exists(destinationFilePath) || Regex.IsMatch(message.Document.FileName, @".\.(mp3|wav|ogg|jpg|jpeg|png|gif|mp4|avi|mkv|exe|dll)"))
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Вы прислали запрещенный формат файла (аудио, фото, видео) или файл с таким именем уже есть.\U0001F624");
                    return;
                }
                await using FileStream fileStream = System.IO.File.OpenWrite(destinationFilePath);
                await botClient.DownloadFileAsync(filePath, fileStream);
                fileStream.Close();

                _fileManagement.AddFile(message.Document.FileName);

                List<long> users = _userInfo.GetUsers();
                foreach (long user in users)
                {
                    if (!_userInfo.IsUserBanned(user))
                        await botClient.SendTextMessageAsync(user, $"Файл {message.Document.FileName} добавлен\U0001F973");
                }
                return;
            }
        }
        async static private Task ShooseTextCommandAsync(ITelegramBotClient botClient, Message message, CancellationToken token)
        {
            string text = "";

            if (message.Text != null && _userInfo != null)
            {
                switch (message.Text)
                {
                    case "/start": await botClient.SendTextMessageAsync(message.Chat.Id, "Добро пожаловать! Cнизу появилась панель с кнопками!", replyMarkup: GenerateKeyboard(message.Chat.Id)); break;
                    case var textbox when _fileManagement.IsHomeWorkContains(textbox): await GetFileByText(message, botClient); break;
                    case var textBot when textBot.ToLower().Contains("забанить:"): await BanUser(message, botClient);break;
                    case var textBot when textBot.ToLower().Contains("разбанить:"): await UnBanUser(message, botClient); break;
                    case var textBot when textBot.ToLower().Contains("деладмин:"): await RemoveAdmin(message, botClient); break;
                    case var textBot when textBot.ToLower().Contains("админ:"): await AddAdmin(message, botClient); break;
                    case var textBot when textBot.ToLower().Contains("удалить:"): await DeleteFile(message, botClient); break;
                    case var textBot when textBot.ToLower().Contains("/f"): await GetFilesByFilter(message, botClient, token); break;
                    case var textBot when textBot.ToLower().Contains("/pdf"): await GetPdfFile(message, botClient); break;
                    case "Список пользователей": await ShowUserList(message, botClient); break;
                    case "Список забаненых": await ShowBanList(message, botClient); break;
                    case "Список админов": await ShowAdminList(message, botClient); break;
                    case "Взять файл": await GetFileByButton(message, botClient, token); break;
                    case "Список файлов": await botClient.SendTextMessageAsync(message.Chat.Id, _fileManagement.IsStorageEmpty() ? "Хранилище пустое\U0001F910" : _fileManagement.ShowFiles()); break;
                    case "Добавить файл": await botClient.SendTextMessageAsync(message.Chat.Id, "Пришлите пожалуйста файл.", parseMode: ParseMode.Markdown); break;
                    case "Найти дз по фильтру": await botClient.SendTextMessageAsync(message.Chat.Id, "Чтобы использовать фильтр, используйте следующую конструкцию: /f слово или /f дд.мм.гггг\n Например: \n/f русский \n/f 05.10"); break;
                    case "Удалить файл": await botClient.SendTextMessageAsync(message.Chat.Id, "Чтобы удалить файл, введите ' удалить: имя файла '\n Учтите, что в имени файла должно быть расширение"); break;

                    case var textBot when textBot.ToLower().Contains("разбанить/забанить"):
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Чтобы забанить, введите - ' Забанить: id-пользователя '\nРазбанить - ' Разбанить: id-пользователя '");
                        break;
                    case "Создать pdf":
                        text = _pdfConverter.GetCountPhotos(message.Chat.Id) == 0 ? "Пришлите фотографии чтобы сконвертировать в pdf. " : $"На pdf прислано {_pdfConverter.GetCountPhotos(message.Chat.Id)} фото. \nЧтобы сформировать файл, введите следующую команду:\n' /pdf __название файла__ ' \nили\n' /pdf ' (создаст рандомное название) \n/cancel чтобы отменить";
                       
                        await botClient.SendTextMessageAsync(message.Chat.Id, text, parseMode: ParseMode.Markdown);
                        break;
                    case "/cancel":
                        if (_pdfConverter.IsUserContains(message.Chat.Id))
                        {
                            text = "Список фотографий был очищен";
                            _pdfConverter.RemoveData(message.Chat.Id);
                        }
                        await botClient.SendTextMessageAsync(message.Chat.Id, string.IsNullOrEmpty(text) ? "Удалять нечего, вы не присылали фотографии" : text);
                        break;



                    case "Снять/Назначить админом":
                        if (!_userInfo.IsUserAdmin(message.Chat.Id))
                            break;
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Чтобы назначить админом, введите команду ' админ: id пользователя ', или 'деладмин: id пользователя' чтобы удалить ", parseMode: ParseMode.Markdown);
                        break;
                    
                    default: await GetFileByIndex(message, botClient); break;
                }
            }
        }
        async static private Task ShowUserList(Message message, ITelegramBotClient botClient)
        {
            string text;
            if (!_userInfo.IsUserAdmin(message.Chat.Id))
                return;

            StringBuilder users = new StringBuilder();
            List<Person> listperson = _userDataBase.GetAllUsers();
            foreach (var item in listperson)
            {
                if (!_userInfo.IsUserBanned(item.Id))
                    users.Append($"{item}\n");

            }
            text = _userInfo.IsUserAdmin(message.Chat.Id) == false ? "Вам не доступна эта опция" : users.ToString();
            await botClient.SendTextMessageAsync(message.Chat.Id, text, parseMode: ParseMode.Markdown);
            return;
        }
        async static private Task ShowAdminList(Message message, ITelegramBotClient botClient)
        {
            if (message.Chat.Id != _fileManagement.ID_MAIN_ADMIN)
                return;
            List<long> admins = _fileManagement.GetAdminsList();

            if (admins.Contains(_fileManagement.ID_MAIN_ADMIN))
            {
                admins.Remove(_fileManagement.ID_MAIN_ADMIN);
                admins.Remove(0);
            }

            if (admins.Count == 0)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Админов нет");
                return;
            }
            await botClient.SendTextMessageAsync(message.Chat.Id, $"Cписок админов:\n {string.Join('\n', admins)}");
        }
        async static private Task ShowBanList(Message message, ITelegramBotClient botClient)
        {
            if (!_userInfo.IsUserAdmin(message.Chat.Id))
                return;
            List<long> banlist = _userInfo.GetBanUsers();
            if (banlist.Count == 0)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Банлист пуст.\U0001F61F");
                return;
            }
            List<Person> persons = _userDataBase.GetAllUsers();
            StringBuilder resultbanlist = new StringBuilder();
            foreach (long item in banlist)
            {
                foreach (Person person in persons)
                {
                    if (_userInfo.IsUserBanned(item) && person.Id == item)
                        resultbanlist.Append($"{person}\n");
                    else
                        resultbanlist.Append($"{item} - имени и фамилии нет");
                }
            }

            await botClient.SendTextMessageAsync(message.Chat.Id, resultbanlist.ToString());
        }
        async static private Task GetFileByText(Message message, ITelegramBotClient botClient)
        {
            List<string>tempList = _fileManagement.GetFilesByMask(message.Text);
            if (tempList.Count == 0)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, $"Хранилище пустое.\U0001F614");
                return;
            }
            string path = _fileManagement.GetPathFile(tempList[0]);//Get first file
            await using (FileStream stream = System.IO.File.OpenRead(path))
            {
                await botClient.SendDocumentAsync(message.Chat.Id, new InputFileStream(stream, _fileManagement.GetNameFile(0)));
            };
        }
        async static private Task GetFileByButton(Message message, ITelegramBotClient botClient, CancellationToken token)
        {
            List<string> homeworks = _fileManagement.GetListHomeWorks();
            if (homeworks.Count == 0)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Хранилище пустое\U0001F910");
                return;
            }
            List<List<InlineKeyboardButton>> temp = GetKeyboardMarkup(homeworks);
            temp.Add(new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData("Последние 3 файла") });

            await botClient.SendTextMessageAsync(message.Chat.Id, "Выберите файл: ", parseMode: ParseMode.Markdown, replyMarkup: new InlineKeyboardMarkup(temp), cancellationToken: token);
        }
        async static private Task GetFileByIndex(Message message, ITelegramBotClient botClient)
        {
            int index;
            if (int.TryParse(message.Text, out index))
            {
                if (_fileManagement.IsStorageEmpty())
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Хранилище пустое\U0001F910");
                    return;
                }

                string path = _fileManagement.GetPathFile(index);
                if (string.IsNullOrEmpty(path))
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"Вы ввели неверный индекс");
                    return;
                }
                await using FileStream stream = System.IO.File.OpenRead(path);
                await botClient.SendDocumentAsync(message.Chat.Id, new InputFileStream(stream, _fileManagement.GetNameFile(index)));
            }
            else
                await botClient.SendTextMessageAsync(message.Chat.Id, $"Команда {message.Text} не распознана");
            return;
        }
        async static private Task GetFilesByFilter(Message message, ITelegramBotClient botClient, CancellationToken token)
        {
            string text = message.Text.Substring(message.Text.IndexOf('f') + 1).TrimStart();
            List<string> tempList = _fileManagement.GetFilesByMask(text);
            if (tempList.Count == 0)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, $"По поиску {text} ничего не найдено.");
                return;

            }
            List<List<InlineKeyboardButton>> buttonRows = GetKeyboardMarkup(tempList, true);
            await botClient.SendTextMessageAsync(message.Chat.Id, $"Список по поиску {text}:\n", replyMarkup: new InlineKeyboardMarkup(buttonRows), cancellationToken: token);
        }
        async static private Task DeleteFile(Message message, ITelegramBotClient botClient)
        {
            string text;
            if (message.Chat.Id != _fileManagement.ID_MAIN_ADMIN)
                return;
            text = message.Text.Substring(message.Text.IndexOf(':') + 1).TrimStart();
            if (!_fileManagement.DeleteFile(text))
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Файл не был найден, возможно вы забыли дописать расширение файла.");
                return;
            }
            _fileManagement.RemoveFile(text);
            await botClient.SendTextMessageAsync(message.Chat.Id, "Файл удален.");
        }
        async static private Task GetPdfFile(Message message, ITelegramBotClient botClient)
        {
            string text = "";
            string temptext = message.Text.Substring(message.Text.IndexOf('f') + 1).TrimStart();
            if (!_pdfConverter.IsUserContains(message.Chat.Id))
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, $"Нет фотографий для конвертации. Возможно фотографии обнулились при новом запуске бота({_date}).");
                return;
            }
            else if (message.Text.ToLower().TrimStart() == "/pdf" || string.IsNullOrWhiteSpace(temptext))
            {
                string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
                text = new string(Enumerable.Repeat(chars, 10).Select(s => s[new Random().Next(0, s.Length)]).ToArray());

                string pathpdf = _pdfConverter.GetDocument(message.Chat.Id, text, botClient);
                await using FileStream stream = System.IO.File.OpenRead(pathpdf);
                await botClient.SendDocumentAsync(message.Chat.Id, new InputFileStream(stream, text.Contains(".pdf") ? text : text + ".pdf"));
            }
            else
            {
                string pathpdf = _pdfConverter.GetDocument(message.Chat.Id, temptext, botClient);
                await using FileStream stream = System.IO.File.OpenRead(pathpdf);
                await botClient.SendDocumentAsync(message.Chat.Id, new InputFileStream(stream, temptext.Contains(".pdf") ? temptext : temptext + ".pdf"));
                stream.Close();
                System.IO.File.Delete(pathpdf);
            }
        }
        async static private Task UnBanUser(Message message, ITelegramBotClient botClient)
        {
            long id;
            if (!_userInfo.IsUserAdmin(message.Chat.Id))
                return;
            else if (!long.TryParse(message.Text.Substring(message.Text.IndexOf(':') + 1).TrimStart(), out id))
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Айди либо не введен, либо не найден");
                return;
            }
            else if(_userInfo.IsUserAdmin(id) && _fileManagement.ID_MAIN_ADMIN == id)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Вам недоступны операции бан и разбан к админам.");
                return;
            }
            else if (message.Chat.Id == id) //if a user has banned themselves
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Вы пытаетесь разбанить себя?\U0001F928");
                return;
            }
            else if (!_userInfo.IsUserBanned(id))
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Вы пытаетесь разбанить пользователя который и так уже разбанен.");
                return;
            }

            _userInfo.RemoveUserFromBanList(id);
            await botClient.SendTextMessageAsync(message.Chat.Id, $"{id} - разбанен");
            await botClient.SendTextMessageAsync(_fileManagement.ID_MAIN_ADMIN, $"Админ({message.Chat.Id}) разбанил пользователя {id}");
            await botClient.SendTextMessageAsync(id, "Вы разбанены\U0001F917");
        }
        async static private Task BanUser(Message message, ITelegramBotClient botClient)
        {
            long id;
            if (!_userInfo.IsUserAdmin(message.Chat.Id))
                return;
            if (!long.TryParse(message.Text.Substring(message.Text.IndexOf(':') + 1).TrimStart(), out id))
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Айди либо не введен, либо не найден");
                return;
            }
            if (_userInfo.IsUserAdmin(id) && id == _fileManagement.ID_MAIN_ADMIN)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Вам недоступны операции бан и разбан к админам.");
                return;
            }
            if (!_userInfo.IsUserExist(id))
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, $"Пользователь с id {id} не найден.");
                return;
            }
            if (message.Chat.Id == id) //if a user has banned themselves
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Вы пытаетесь забанить сами себя?\U0001F914");
                return;
            }
            if (_userInfo.IsUserBanned(id))
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Нет смысла банить этого пользователя дважды.");
                return;
            }
            _userInfo.AddToBanList(id);
            await botClient.SendTextMessageAsync(message.Chat.Id, $"{id} - забанен");
            await botClient.SendTextMessageAsync(_fileManagement.ID_MAIN_ADMIN, $"Админ({message.Chat.Id}) забанил пользователя {id}");
            await botClient.SendTextMessageAsync(id, "Вы были забанены\U0001F636");
        }
        async static private Task RemoveAdmin(Message message, ITelegramBotClient botClient)
        {
            long id;
            if (message.Chat.Id != _fileManagement.ID_MAIN_ADMIN)
                return;
            else if (!long.TryParse(message.Text.Substring(message.Text.IndexOf(':') + 1).TrimStart(), out id))
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Айди либо не введен, либо не найден.");
                return;
            }
            else if (id == _fileManagement.ID_MAIN_ADMIN)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Вы пытаетесь удалить себя?");
                return;
            }
            else if (_userInfo.IsUserAdmin(id))
            {
                _userInfo.RemoveAdmin(id);
                await botClient.SendTextMessageAsync(id, "Вы убраны с поста админа.");
                await botClient.SendTextMessageAsync(message.Chat.Id, $"Пользователь {id} убран с поста админа.");
            }
        }
        async static private Task AddAdmin(Message message, ITelegramBotClient botClient) {
            long id;
            if (message.Chat.Id != _fileManagement.ID_MAIN_ADMIN)
                return;
            else if (!long.TryParse(message.Text.Substring(message.Text.IndexOf(':') + 1).TrimStart(), out id))
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Айди либо не введен, либо не найден.");
                return;
            }
            else if (id == _fileManagement.ID_MAIN_ADMIN)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Вы пытаетесь назначить себя админом, хотя вы и так админ.");
                return;
            }
            else if (_userInfo.IsUserAdmin(id))
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Этот пользователь уже является админом.");
                return;
            }
            else
            {
                _userInfo.AddAdmin(id);
                await botClient.SendTextMessageAsync(id, "Вы добавлены на пост админа.");
                await botClient.SendTextMessageAsync(message.Chat.Id, $"Пользователь {id} добавлен в админ лист.");
                return;
            }
        }
        private static List<List<InlineKeyboardButton>> GetKeyboardMarkup(List<string> list, bool isRowOne = false)
        {
            List<List<InlineKeyboardButton>>? buttonRows = new List<List<InlineKeyboardButton>>();
            if (isRowOne)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    List<InlineKeyboardButton> row = new()
                    {
                        InlineKeyboardButton.WithCallbackData(list[i])
                    };
                    buttonRows.Add(row);
                }
            }
            else
            {
                for (int i = 0; i < list.Count; i += 2)
                {

                    List<InlineKeyboardButton> row = new();
                    if (i < list.Count)
                        row.Add(InlineKeyboardButton.WithCallbackData(list[i]));

                    if (i + 1 < list.Count)
                        row.Add(InlineKeyboardButton.WithCallbackData(list[i + 1]));

                    buttonRows.Add(row);
                }
            }
            return buttonRows;
        }
        private static void Print(string text, ConsoleColor afterColor)
        {
            Console.ForegroundColor = afterColor;
            Console.WriteLine(text);
            Console.ForegroundColor = ConsoleColor.Gray;
        }
        private static ReplyKeyboardMarkup GenerateKeyboard(long id)
        {
            if (_userInfo == null)
                throw new ArgumentNullException("Ошибка при создании клавиатуры для пользователя");

            KeyboardButton[][] buttons;
            if (_fileManagement.ID_MAIN_ADMIN == id)
            {
                buttons = new[]
                {
                    new KeyboardButton[] { new KeyboardButton("Список файлов"), new KeyboardButton("Найти дз по фильтру") },
                    new KeyboardButton[] { new KeyboardButton("Создать pdf") },
                    new KeyboardButton[] { new KeyboardButton("Взять файл"), new KeyboardButton("Добавить файл"), new KeyboardButton("Удалить файл")},
                    new KeyboardButton[] { new KeyboardButton("Список пользователей"), new KeyboardButton("Список забаненых"),new KeyboardButton("Список админов")},
                    new KeyboardButton[] { new KeyboardButton("Разбанить/Забанить"), new KeyboardButton("Снять/Назначить админом") }
                };
                return new ReplyKeyboardMarkup(buttons);
            }
            else if (_userInfo.IsUserAdmin(id))
            {
                buttons = new[]
                {
                    new KeyboardButton[] { new KeyboardButton("Список файлов"), new KeyboardButton("Найти дз по фильтру")},
                    new KeyboardButton[] { new KeyboardButton("Создать pdf") },
                    new KeyboardButton[] { new KeyboardButton("Взять файл"),  new KeyboardButton("Добавить файл"), new KeyboardButton("Удалить файл")},
                    new KeyboardButton[] { new KeyboardButton("Список пользователей"), new KeyboardButton("Список забаненых") },
                    new KeyboardButton[] { new KeyboardButton("Разбанить/Забанить") },
                };
            }
            else
            {
                buttons = new[]
                {
                    new KeyboardButton[] { new KeyboardButton("Список файлов"), new KeyboardButton("Добавить файл") },
                    new KeyboardButton[] { new KeyboardButton("Взять файл"), new KeyboardButton("Найти дз по фильтру") },
                    new KeyboardButton[] { new KeyboardButton("Создать pdf") },
                };
            }
            return new ReplyKeyboardMarkup(buttons);
        }
    }
}
