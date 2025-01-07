using System.Text.Json.Serialization;

namespace MtgDeckInfoScraper.Scraping.Models;

public class DeckInfo
{
	public Guid Id { get; private set; }
	public string Title { get; private set; }
	public string Url { get; private set; }
	public double Price { get; private set; }
	public char PriceSymbol { get; private set; }
	public string LastUpdated { get; private set; }
	public int Likes { get; private set; }
	public int Views { get; private set; }

	public DeckInfo(string title,
		string url,
		char priceSymbol,
		double price,
		string lastUpdated,
		int likes,
		int views)
	{
		Id = Guid.NewGuid();
		Title = title;
		Url = url;
		PriceSymbol = priceSymbol;
		Price = price;
		LastUpdated = lastUpdated;
		Likes = likes;
		Views = views;
	}

	[JsonConstructor]
	private DeckInfo(Guid id,
		string title,
		string url,
		char priceSymbol,
		double price,
		string lastUpdated,
		int likes,
		int views)
	{
		Id = id;
		Title = title;
		Url = url;
		PriceSymbol = priceSymbol;
		Price = price;
		LastUpdated = lastUpdated;
		Likes = likes;
		Views = views;
	}

	public override string ToString()
	{
		return $"{Title} ({PriceSymbol}{Price}), {Likes} likes, {Views} views, last updated {LastUpdated}, url: {Url}";
	}
}