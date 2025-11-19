public class Workflow {
    public int WorkflowId { get; set; }
    public int DocumentId { get; set; }
    public string CurrentStage { get; set; }
    public int AssignedTo { get; set; }
    public DateTime ActionDate { get; set; }
}
