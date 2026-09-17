namespace Invoice.DTOs;

public class PagedResultDto<T>
{
    public IEnumerable<T> Data { get; set; } = new List<T>();
    public int TotalRecords { get; set; }
}
