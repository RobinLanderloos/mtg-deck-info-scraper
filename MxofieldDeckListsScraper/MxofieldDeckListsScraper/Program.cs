using MxofieldDeckListsScraper.Components;
using MxofieldDeckListsScraper.scraping.Moxfield;
using OpenQA.Selenium.Chrome;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
	.AddInteractiveWebAssemblyComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseWebAssemblyDebugging();
}
else
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
	.AddInteractiveWebAssemblyRenderMode()
	.AddAdditionalAssemblies(typeof(MxofieldDeckListsScraper.Client._Imports).Assembly);

var moxfieldScraper = new MoxfieldDeckInfoScraper();
var deckList = await moxfieldScraper.ScrapeDeckList("https://moxfield.com/decks/public?q=eyJodWIiOiIiLCJmb3JtYXQiOiIiLCJkZWNrTmFtZSI6IiIsImNhcmRJZCI6IiIsImNhcmROYW1lIjoiIiwiYm9hcmQiOiIiLCJsYXN0U2VhcmNoIjoiIiwiZmlsdGVyIjoiIiwiY29tbWFuZGVyQ2FyZElkIjoicjg0YjEiLCJjb21tYW5kZXJDYXJkTmFtZSI6IkNhcHRhaW4gTidnaGF0aHJvZCIsInBhcnRuZXJDYXJkSWQiOiIiLCJwYXJ0bmVyQ2FyZE5hbWUiOiIiLCJjb21tYW5kZXJTaWduYXR1cmVTcGVsbENhcmRJZCI6IiIsImNvbW1hbmRlclNpZ25hdHVyZVNwZWxsQ2FyZE5hbWUiOiIiLCJwYXJ0bmVyU2lnbmF0dXJlU3BlbGxDYXJkSWQiOiIiLCJwYXJ0bmVyU2lnbmF0dXJlU3BlbGxDYXJkTmFtZSI6IiIsImNvbXBhbmlvbkNhcmRJZCI6IiIsImNvbXBhbmlvbkNhcmROYW1lIjoiIiwiY29tbWFuZXJUaWVyU2V0dGluZyI6ImVxdWFscyIsImNvbW1hbmVyVGllciI6IiIsImRlY2tUaWVyU2V0dGluZyI6ImVxdWFscyIsImRlY2tUaWVyIjoiIiwic29ydENvbHVtbiI6InZpZXdzIiwic29ydERpcmVjdGlvbiI6ImRlc2NlbmRpbmciLCJwYWdlTnVtYmVyIjoxLCJwYWdlU2l6ZSI6NjQsInZpZXciOiJwdWJsaWMiLCJzZWxlY3RlZENhcmRJZHMiOnsiY29tbWFuZGVyQ2FyZElkIjoicjg0YjEifSwic2VsZWN0ZWRDYXJkTmFtZXMiOnsiY29tbWFuZGVyQ2FyZElkIjoiQ2FwdGFpbiBOJ2doYXRocm9kIn0sImh1Yk5hbWUiOiIifQ%3D%3D", 5);

app.Run();