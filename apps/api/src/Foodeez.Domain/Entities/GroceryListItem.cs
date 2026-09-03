using Foodeez.Domain.Common;

namespace Foodeez.Domain.Entities;

public class GroceryListItem : BaseEntity
{
    public Guid GroceryListId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// How much to buy, as it would be written on a paper list - "500 g", "2 bunches",
    /// "1 loaf". Text rather than a number and a unit because consolidating six meals'
    /// worth of chicken produces an amount, not an arithmetic result, and because this is
    /// the field people edit by hand in the shop.
    /// </summary>
    public string Quantity { get; set; } = string.Empty;

    /// <summary>Aisle, so the list can be walked in the order a shop is laid out.</summary>
    public string Category { get; set; } = "Other";

    /// <summary>Which planned meals wanted it, so a reader can tell why it is on the list.</summary>
    public string? Source { get; set; }

    public bool IsChecked { get; set; }

    /// <summary>
    /// Added by hand rather than derived from the plan. Regenerating replaces what the plan
    /// produced and leaves these alone - otherwise a refresh would delete the things only
    /// the shopper knew they needed.
    /// </summary>
    public bool IsCustom { get; set; }

    public int SortOrder { get; set; }

    public GroceryList GroceryList { get; set; } = null!;
}
