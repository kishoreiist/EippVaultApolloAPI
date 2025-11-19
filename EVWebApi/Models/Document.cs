public class Document {
    public int DocumentId { get; set; }
    public int CabinetId { get; set; }
    public string FileName { get; set; }
    public string FilePath { get; set; }
    public int Version { get; set; }
    public int UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; }
    public string Status { get; set; }
}
