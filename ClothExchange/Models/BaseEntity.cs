namespace ClothExchange.Models;

// Abstract base class: Clothing and ExchangeRequest inherit Id and CreatedAt from here.
// An abstract class cannot be created with "new" - it only exists to be inherited.
// It also implements the interface IDisplayable, but leaves GetSummary() abstract,
// so every child class must write its own version (polymorphism / method overriding).
public abstract class BaseEntity : IDisplayable
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }

    // Constructor: every new object gets the current time
    protected BaseEntity()
    {
        CreatedAt = DateTime.Now;
    }

    public abstract string GetSummary();
}
