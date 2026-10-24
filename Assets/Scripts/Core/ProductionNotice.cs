namespace ForestShop.Core
{
    public enum ProductionKind { Harvest, Craft, Sale, Forage, Nursery }

    // A completed settlement, never a prediction or an additional reward.
    // Presentation is optional; this transient value is not part of a save.
    public readonly struct ProductionNotice
    {
        public readonly ProductionKind Kind;
        public readonly int Facility, Item, Amount;
        public ProductionNotice(ProductionKind kind, int facility, int item, int amount)
        { Kind = kind; Facility = facility; Item = item; Amount = amount; }
    }
}
