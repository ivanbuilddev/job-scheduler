public class FileHasherParams
{
    public string DirectoryPath {get; set;} = string.Empty;
    public string FileToSave {get; set;} = string.Empty;
    public string Algorithm { get; set; } = "SHA256";
}