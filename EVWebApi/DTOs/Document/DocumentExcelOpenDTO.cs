namespace EVWebApi.DTOs.Document
{
    public class DocumentExcelOpenDTO
    {
        public int DocumentId { get; set; }
        public int SheetIndex { get; set; } 
        public int StartRow { get; set; } = 2;       
        public int RowCount { get; set; } = 1000;
    }
}
