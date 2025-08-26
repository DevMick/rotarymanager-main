namespace RotaryClubManager.Application.DTOs.Formation
{
    public class UploadFileDto
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Length { get; set; }
        public Stream FileStream { get; set; } = null!;
    }
}
