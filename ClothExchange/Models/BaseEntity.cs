namespace ClothExchange.Models
{
    // Abstract base class: every table class inherits Id and CreatedAt.
    public abstract class BaseEntity
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }

        protected BaseEntity()
        {
            CreatedAt = DateTime.Now;
        }
    }
}
