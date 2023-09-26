using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Text.Json;

namespace HomeWorkHub
{
    public class UserDataBase
    {
        private readonly string _pathData = $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\HomeWork\DataUsers.json";
        private List<Person> _personData;
        public UserDataBase()
        {
            if (!File.Exists(_pathData))
                using (File.Create(_pathData)) { };
            _personData = FillUserList();
        }
        private List<Person> FillUserList()
        {
            string text = "";
            using (StreamReader streamReader = new StreamReader(_pathData))
            {
                text = streamReader.ReadToEnd();
            }
            if (string.IsNullOrEmpty(text))
                return new List<Person>();

            return JsonSerializer.Deserialize<List<Person>>(text);
        }
        public List<long> GetUserInDataBase() =>
             _personData.Select(person => person.Id).ToList();

        public List<Person> GetAllUsers() =>
            _personData;

        public void AddUser(Person person)
        {
            _personData.Add(person);    
            
            using(StreamWriter streamWriter = new StreamWriter(_pathData))
            {
                streamWriter.Write(JsonSerializer.Serialize(_personData, new JsonSerializerOptions() { WriteIndented = true }));
            }
        }
    }
}
