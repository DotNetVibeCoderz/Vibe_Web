namespace SMSNet.Models;

public static class AppRoles
{
    public const string Admin = "admin";
    public const string Guru = "guru";
    public const string Siswa = "siswa";
    public const string OrangTua = "orangtua";

    public static readonly string[] All = { Admin, Guru, Siswa, OrangTua };
}
