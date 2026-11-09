namespace aluguel.Services;

/// <summary>Consistent server-side text limits for item creation, editing and publication.</summary>
public static class ListingContentValidation
{
    public const int MaxTitleLength = 140;
    public const int MaxDescriptionLength = 6000;

    public static bool ValidTitle(string? title)
        => !string.IsNullOrWhiteSpace(title) && title.Trim().Length <= MaxTitleLength;

    public static bool ValidDescription(string? description)
        => !string.IsNullOrWhiteSpace(description) && description.Trim().Length <= MaxDescriptionLength;
}
