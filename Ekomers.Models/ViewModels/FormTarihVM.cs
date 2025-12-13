using System.ComponentModel.DataAnnotations;

namespace Ekomers.Models.ViewModels
{
    public class FormTarihVM
    {
        [DataType(DataType.Date)]
        [Required(ErrorMessage = "Tarih alanı zorunludur")]
        [Display(Name = "Tarih seçiniz")]
        public DateTime Tarih { get; set; } = DateTime.Now;
    }
}
