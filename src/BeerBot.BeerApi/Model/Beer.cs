namespace BeerBot.BeerApi.Model
{
    public class Beer
    {
        public int Id { get; set; }
        public int BreweryId { get; set; }
        public string Name { get; set; } = null!;
        public int CategoryId { get; set; }
        public int StyleId { get; set; }
        public float? Abv { get; set; }
        public float? Ibu { get; set; }
        public float? Srm { get; set; }
        public string? Description { get; set; }
    }
}