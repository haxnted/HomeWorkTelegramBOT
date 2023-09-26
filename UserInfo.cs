namespace HomeWorkHub
{
    [Serializable]
    public class Person
    {
        public Person(long Id, string Name, string Surname)
        {
            this.Id = Id;
            this.Name = Name;
            this.Surname = Surname;
        }
        public long Id { get; set; } 
        public string Name { get; set; } = "";
        public string Surname { get; set; } = "";
        public override string ToString() =>
            $"{Id} - {Name} {Surname}";
    }


    public class UserInfo
    {
        private FileManagement _fileManagement;
        public UserInfo(FileManagement fileManagement, UserDataBase userDataBase)
        {
            _banlist = fileManagement.GetBanList();
            _ids = userDataBase.GetUserInDataBase();
            _admins = fileManagement.GetAdminsList();
            _fileManagement = fileManagement;
        }
        private List<long> _banlist = new List<long>();
        private List<long> _ids = new List<long>();
        private List<long> _admins = new List<long>();
        public List<long> GetUsers() =>
            _ids;
        public List<long> GetBanUsers()=>
            _banlist;
        
        public void AddAdmin(long id)
        {
            _admins.Add(id);
            _fileManagement.SaveAdminList(_admins);
        }
        public void RemoveAdmin(long id)
        {
            _admins.Remove(id);
            _fileManagement.SaveAdminList(_admins);
        }
        public bool IsUserBanned(long userId) =>
            _banlist.Contains(userId);    

        public void RemoveUserFromBanList(long id)
        {
            if (!_banlist.Contains(id))
                return;
            _banlist.Remove(id);
            _fileManagement.SaveBanList(_banlist);
        }

        public void AddToBanList(long userId)
        {
            if (_banlist.Contains(userId))
                return;
            _banlist.Add(userId);
            _fileManagement.SaveBanList(_banlist);
        }
        public void AddToUserList(long userId) =>
            _ids.Add(userId);
        public bool IsUserExist(long userId) =>
            _ids.Contains(userId);  
        public bool IsUserAdmin(long userId) =>
            _admins.Contains(userId);
    }
}
