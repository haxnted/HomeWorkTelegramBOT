using System.Text.Json;

namespace HomeWorkHub
{
    public class FileManagement
    {
        public FileManagement()
        {
            FileAvailabilityCheck();
            GetApi();
            GetHomeWorks();
            GetMainIdAdmin();
        }

        public string ApiKey { get; private set; } = "";
        public long ID_MAIN_ADMIN { get; private set; }
        public readonly string pathHomeWork = $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\HomeWork\Files";
        private readonly string _pathAdminList = $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\HomeWork\AdminList.json";
        private readonly string _pathBanList = $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\HomeWork\BanList.json";
        private readonly string _pathApiTelegram = $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\HomeWork\ApiTelegram.txt";
        private readonly string _pathMainAdmin = $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\HomeWork\IdMainAdmin.txt";
        private List<string> _listHomeWorks = new List<string>();
        public void RemoveFile(string path) =>
           _listHomeWorks.Remove(path);
        public bool DeleteFile(string name)
        {

            if (string.IsNullOrEmpty(name) || !File.Exists(Path.Combine(pathHomeWork, name)))
                return false;
            else
            {
                File.Delete(Path.Combine(pathHomeWork, name));
                return true;
            }
        }
        private void GetApi()
        {
            using StreamReader sr = new StreamReader(_pathApiTelegram);
            ApiKey = sr.ReadToEnd();
            sr.Close();
        }
        public List<string> GetFilesByMask(string currentName)
        {
            if (string.IsNullOrEmpty(currentName))
                return new List<string>();
            List<string> files = new List<string>();
            foreach (var item in _listHomeWorks)
            {
                if (item != null && item.ToLower().Contains(currentName.ToLower()))
                    files.Add(item);           
            }
            return files;
        }

        private void GetMainIdAdmin()
        {
            using StreamReader sr = new StreamReader(_pathMainAdmin);
            string text = sr.ReadToEnd();
            sr.Close();
            if (long.TryParse(text, out long id))
                ID_MAIN_ADMIN = id;
            else
                ID_MAIN_ADMIN = 0;
        }

        private void FileAvailabilityCheck()
        {
            List<long> list = new List<long>() { 0 };


            //Create main directory in Roaming
            if (!Directory.Exists($@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\HomeWork"))
                Directory.CreateDirectory($@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\HomeWork");
            
            //Create folder for files / homeworks
            if (!Directory.Exists(pathHomeWork))
                Directory.CreateDirectory(pathHomeWork);

            if (!File.Exists(_pathBanList))
                using (File.Create(_pathBanList)) { }    
            
            if (!File.Exists(_pathAdminList))
                using (File.Create(_pathAdminList)) { }
            

            if (!File.Exists(_pathMainAdmin))
                using (File.Create(_pathMainAdmin)) { }

            if (!File.Exists(_pathApiTelegram))
            {
                using (File.Create(_pathApiTelegram)) { }

                Console.Write("Введите апи бота: ");
                string text = Console.ReadLine() ?? throw new InvalidProgramException("Вы оставили пустое поле.");
                Console.WriteLine($"Если вы ввели верный токен - бот запущен. Если вы сделали ошибку в токене, то исправить можно по пути: {_pathMainAdmin}");
                using StreamWriter sw = new StreamWriter(_pathApiTelegram);
                sw.Write(text);
                sw.Close();
            }
        }

        public List<long> GetBanList()
        {
            string text;
            using (StreamReader sr = new StreamReader(_pathBanList))
            {
                text = sr.ReadToEnd();
                sr.Close();
            }
            if (string.IsNullOrEmpty(text))
                return new List<long>();
            else
                return JsonSerializer.Deserialize<List<long>>(text) ?? throw new NullReferenceException("banlist is null");
        }

        public List<long> GetAdminsList()
        {
            string text;
            using (StreamReader sr = new StreamReader(_pathAdminList))
            {
                text = sr.ReadToEnd();
                sr.Close();
            }
            List<long>? list;
            if (string.IsNullOrEmpty(text))
                list = new List<long>();
            else
                list = JsonSerializer.Deserialize<List<long>>(text);

            if (ID_MAIN_ADMIN != 0)
            {
                if (!list.Contains(ID_MAIN_ADMIN))
                    list.Add(ID_MAIN_ADMIN);
                
            }


            return list ?? throw new NullReferenceException("Adminlist is null");
        }

        public bool IsStorageEmpty()=>
            _listHomeWorks.Count == 0;

        private void GetHomeWorks()
        {
            _listHomeWorks.Clear();
            _listHomeWorks = Directory.GetFiles(pathHomeWork)
                .Select(Path.GetFileName)
                .ToList();
        }

        public List<string> GetListHomeWorks() =>
            _listHomeWorks;

        public void SaveBanList(List<long> banlist)
        {
            string text = JsonSerializer.Serialize(banlist, new JsonSerializerOptions() { WriteIndented = true });
            using StreamWriter sw = new StreamWriter(_pathBanList);
            sw.Write(text);
            sw.Close();
        }

        public void SaveAdminList(List<long> adminlist)
        {
            string text = JsonSerializer.Serialize(adminlist, new JsonSerializerOptions() { WriteIndented = true });
            using StreamWriter sw = new StreamWriter(_pathAdminList);
            sw.Write(text);
            sw.Close();
        }

        public string? GetNameFile(int index)
        {
            if (index < _listHomeWorks.Count && _listHomeWorks[index] != null)
                return _listHomeWorks[index];
            else
                return "Файл не найден";
        }

        public string ShowFiles() =>
            _listHomeWorks.Select((fileName) => $"{fileName}\n").Aggregate((current, next) => current + next);

        public void AddFile(string file) =>
            _listHomeWorks.Add(file);

        public bool IsHomeWorkContains(string text) =>
            _listHomeWorks.Contains(text);

        public string GetPathFile(int index)
        {
            if (index >= _listHomeWorks.Count)   
                return "";
            
            return $@"{pathHomeWork}\{_listHomeWorks[index]}";

        }
        public string GetPathFile(string path) =>
            $@"{pathHomeWork}\{path}";
    }
}
