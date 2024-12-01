namespace MxofieldDeckListsScraper.scraping.Moxfield;

public class DeckInfo
{
	public Guid Id { get; set; }
	public string Title { get; set; }
	public string Url { get; set; }
	public float Price { get; set; }
	public string LastUpdated { get; set; }
	public int Likes { get; set; }
	public int Views { get; set; }
}