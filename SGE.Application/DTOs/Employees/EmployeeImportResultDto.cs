public class EmployeeImportResultDto { public int TotalRows { get; set; } public int SuccessCount { get; set; } public int ErrorCount { get; set; } public List<string> Errors { get; set; } = new(); }
