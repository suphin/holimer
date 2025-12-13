namespace Ekomers.Models.ViewModels.Admin
{
    public class UserListVM
    {
        public string? Id { get; set; }
        public string? AdSoyad { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? Telefon { get; set; }
        public string? Departman { get; set; }
        public string? Sirket { get; set; }
        public int? SirketID { get; set; }
        public string? Unvan { get; set; }
        public DateTime? SonGirisTarihi { get; set; }
        public string Roller { get; set; }
        public string Online { get; set; }
        public int  ImageID { get; set; }
        public bool IsCrmUser { get; set; } = false;
	}
}
