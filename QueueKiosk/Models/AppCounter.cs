namespace QueueKiosk.Models;

public class AppCounter
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Menyimpan ID service yang didukung, dipisahkan koma (contoh: "1,2,3"). 
    // Jika kosong, berarti counter melayani SEMUA service.
    public string SupportedServiceIds { get; set; } = string.Empty;
}
