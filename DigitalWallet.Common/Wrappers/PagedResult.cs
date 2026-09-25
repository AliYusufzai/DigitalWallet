namespace DigitalWallet.Common.Wrappers;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }

    // calculated automatically from other properties
    public int TotalPages
    {
        get { return (int)Math.Ceiling(TotalCount / (double)PageSize); }
    }

    public bool HasNext
    {
        get { return Page < TotalPages; }
    }

    public bool HasPrevious
    {
        get { return Page > 1; }
    }
}
