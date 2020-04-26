namespace BeerBot.BeerApi.Model
{
    public class Style
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string Name { get; set; } = null!;
    }
}