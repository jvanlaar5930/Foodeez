namespace Foodeez.Application.Common;

/// <summary>
/// The stand-in picture for a recipe that came out of the advice tab.
///
/// A recipe the assistant wrote has no photograph - nobody cooked it and nobody
/// photographed it - and inventing one would misrepresent the dish. What it does have is a
/// provenance worth showing, so the thumbnail is the advice tab's own icon and label.
///
/// It is a marker, not a fetchable URL. Each client draws the icon and text itself with the
/// same components the tab uses, which stays sharp at any size, needs no hosting, and costs
/// no request; a client that does not recognise it falls through to its ordinary
/// broken-image handling and shows the usual placeholder.
/// </summary>
public static class AiRecipeImage
{
    public const string Marker = "foodeez://ask-ai";

    public static bool IsMarker(string? imageUrl) =>
        string.Equals(imageUrl, Marker, StringComparison.OrdinalIgnoreCase);
}
