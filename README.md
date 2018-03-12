ElasticEpiserver
===
You are now looking at the documentation of ElasticEpiserver. 
ElasticEpiserver is an implementation of the popular Elasticsearch paradigm for typical Episerver CMS projects. 

To get started, the first thing you need to do is to install Elasticsearch on your development environment. Subsequently, you can proceed by installing our NuGet package through Visual Studio onto your Episerver project. 

Install Elasticsearch
---
Please refer to this page for the latest guide on how to install Elasticsearch on your machine:
https://www.elastic.co/downloads/elasticsearch

### Norwegian settings
ElasticEpiserver contains some special settings that only apply for norwegian indexes. To enable these, you need to place the two text files located in `ElasticSearch-config` in your Elasticsearch installation's `/config` directory. 

 - [nynorsk.txt](https://github.com/Altinn/elasticsearch-episerver/blob/master/ElasticSearch-config/nynorsk.txt)
 - [norwegian_stop.txt](https://github.com/Altinn/elasticsearch-episerver/blob/master/ElasticSearch-config/norwegian_stop.txt)

It is important that these files' names remain untouched.

Install our NuGet package
---
TODO

Integrate with your Episerver project
---
When you have setup Elasticsearch and installed our NuGet package, it is time to customize your project to start taking advantage of our implementation. 

### Build your index

**Disclaimer:**
The examples we provide throughout the following text is based on an Episerver Alloy site. To make this process as easy as possible, we suggest you make yourself familiar with Alloy's page types and that you get a general overview of how the project is set up. 

**1. Let Episerver know about your Elasticsearch server.** 

To do this, paste the following code into one of your project initializers. This code will specify where your Elasticsearch is running as well as giving names to the different indices that ElasticEpiserver will utilize for its search. 
```csharp
ElasticEpiClient.Current.ElasticWebUrl = "http://localhost:9200";
ElasticEpiClient.Current.BaseIndexName = "alloy_content";
ElasticEpiClient.Current.HitsLogIndexName = "alloy_hitslog";
ElasticEpiClient.Current.StatisticsIndexName = "alloy_statistics";
```

**2. Create your index document class.**

All documents in the ElasticEpiserver index is represented by an implementation of our base class named `ElasticEpiDocument`. This base class contains the bare minimum of an index document. That means properties for title, ingress, body, publish dates etc. See our source code for specifics. 
To include your own page types' properties in the index, you can inherit from this class as follows:
```csharp
public class AlloyIndexDocument : ElasticEpiDocument
{
    public string UniqueSellingPoints { get; set; }

    public override IList<string> GetLanguageAnalyzableProperties()
    {
        return base.GetLanguageAnalyzableProperties()
            .Concat(new[] { nameof(UniqueSellingPoints) })
            .ToList();
    }

    public override IList<string> GetMatchFilterProperties()
    {
        return base.GetMatchFilterProperties()
            .Concat(new[] { nameof(UniqueSellingPoints) })
            .ToList();
    }
}
```
Notice how this class contains its own separate property `UniqueSellingPoints`. The Elasticsearch index will now include this property as a separate field in addition to the properties of the `ElasticEpiDocument` base class. 

You will notice that the class overrides two functions, namely `GetLanguageAnalyzableProperties()` and `GetMatchFilterProperties()`. These functions will let the ElasticEpiserver know which properties should be treated as language and which properties that should be available for match filtering respectively. More on this later. However, it is important that you include your newly created properties in the list of analyzable properties if you want to perform full text searches on them. 

**3. Tell ElasticEpiserver how to handle your Episerver content.**

In this section you will let ElasticEpiserver know how to build its indices based on your Episerver content. To do this, you need to implement three different interfaces, namely `IEpiContentValidator`, `IEpiPageParser` and `IEpiBlockParser`. Your respective implementations of these interfaces must then be registered in the IoC container. The following shows simple examples of how to do this: 
```csharp
public class AlloyContentValidator : IEpiContentValidator
{
    public bool ShouldBeIndexed(IContent content)
    {
        var publishedFilter = new FilterPublished();

        return !publishedFilter.ShouldFilter(content) ||
               !(content is BlockData) ||
               !(content is MediaData) ||
               !(content is ContainerPage) ||
               !(content is ContentAssetFolder) ||
               !(content is ContentFolder);
    }
}
```
The above implementation lets ElasticEpiserver know if the given `IContent` should be included in the index. By default, you also need to implement `ISearchablePage` on all your page types that you wish to include in the index. The above function is simply a second layer of validation.
```csharp
public class AlloyPageParser : IEpiPageParser
{
   public ElasticEpiDocument Parse(PageData page)
   {
	   switch (page.GetOriginalType().Name)
	   {
	       case nameof(ProductPage):
	           return new ProductPageParser().Parse((ProductPage)page);
	       case nameof(StandardPage):
	           return new StandardPageParser<StandardPage>().Parse((StandardPage)page);
	       default:
	           return new DefaultPageParser<SitePageData>().Parse((SitePageData)page);
	   }
   }
}
```
This code is called when the indexation comes across an Episerver page that implements the `ISearchablePage` interface and that has been validated for indexation by the `IEpiContentValidator` implementation. The switch statement determines the specific type of the `PageData` object and reroutes to a specific implementation of parsing logic. The `ProductPageParser` and `DefaultPageParser` is available below. You need to edit this code to match your project's types. 
```csharp
public class DefaultPageParser<T> : PageParserBase<T, AlloyIndexDocument> where T : SitePageData
{
}
```
This class represents the bare minimum required by a page parser implementation. Notice how it inherits from the `PageParserBase<T, TK>`, which is a base class part of ElasticEpiserver. It is important that the `TK` type parameter represents your implementation of index document that you created earlier. 
```csharp
public class ProductPageParser : StandardPageParser<ProductPage>
{
    public override AlloyIndexDocument Parse(ProductPage page)
    {
        var parsed = base.Parse(page);

        parsed.UniqueSellingPoints = string.Join(",", page.UniqueSellingPoints);

        return parsed;
    }
}
```
Note that this class inherits from the `StandardPageParser<T>` as shown below. In addition to the standard implementation, it parses the `UniqueSellingPoints` property on the `ProductPage` page type.
```csharp
public class StandardPageParser<T> : DefaultPageParser<T> where T : StandardPage
{
    public override AlloyIndexDocument Parse(T page)
    {
        var parsed = base.Parse(page);

        parsed.Body = string.Empty;

        parsed.Body += ServiceLocator.Current.GetInstance<IEpiContentParser>()
            .ParseToString(page.Language, page.MainBody);
        parsed.Body += ServiceLocator.Current.GetInstance<IEpiContentParser>()
            .ParseContentArea(page.Language, page.MainContentArea);

        return parsed;
    }
}
```
The `StandardPageParser<T>` overrides the `DefaultPageParser<T>` and specifies how the `Body` field of the index document should be parsed. Notice the use of our `IEpiContentParser` implementation. The returned object is created by us for parsing `XhtmlString` and `ContentArea` properties. You can of course create your own implementations. See the source code for details on our implementation. 
```csharp
public class AlloyBlockParser : IEpiBlockParser
{
    public string Parse(CultureInfo language, BlockData block)
    {
        switch (block.GetOriginalType().Name)
        {
            case nameof(ContactBlock):
                return new ContactBlockParser().Parse(language, (ContactBlock)block);
            default:
                return string.Empty;
        }
    }
}
```
Finally, this is an example of how to parse an Episerver block. Similarly as in the `IEpiPageParser` implementation, the code reroutes to specific parser based on the actual type of the `BlockData` object. The `ContactBlockParser` is available blow.
```csharp
public class ContactBlockParser : BlockParserBase<ContactBlock>
{
    public override string Parse(CultureInfo language, ContactBlock block)
    {
        return block.Heading;
    }
}
```
This is a very simple implementation for illustration purposes. In your project, your block parsers will likely be more complex than this. The returned string is the data that will be stored in the index.

**4. Register your implementations in the IoC container.**

Now that you have created implementation of our three interfaces, it is time to register them in the IoC container. **You should also register your index document class here**. The following shows a basic example of how to do this in an Episerver Alloy project. Pay careful attention to the `ConfigureContainer(ConfigurationExpression container)` function.
```csharp
[InitializableModule]
public class DependencyResolverInitialization : IConfigurableModule
{
    public void ConfigureContainer(ServiceConfigurationContext context)
    {
        //Implementations for custom interfaces can be registered here.
        context.StructureMap().Configure(ConfigureContainer);

        context.ConfigurationComplete += (o, e) =>
        {
            //Register custom implementations that should be used in favour of the default implementations
            context.Services.AddTransient<IContentRenderer, ErrorHandlingContentRenderer>()
                .AddTransient<ContentAreaRenderer, AlloyContentAreaRenderer>();
        };
    }

    private void ConfigureContainer(ConfigurationExpression container)
    {
	    ElasticEpiClient.Current.BaseIndexName = "alloy_content";
	    ElasticEpiClient.Current.HitsLogIndexName = "alloy_hitslog";
	    ElasticEpiClient.Current.StatisticsIndexName = "alloy_statistics";

		container.For<ElasticEpiDocument>().Use<AlloyIndexDocument>();
		container.For<IEpiContentValidator>().Use<AlloyContentValidator>();
		container.For<IEpiPageParser>().Use<AlloyPageParser>();
		container.For<IEpiBlockParser>().Use<AlloyBlockParser>();
    }

    public void Initialize(InitializationEngine context)
    {
        DependencyResolver.SetResolver(new ServiceLocatorDependencyResolver(context.Locate.Advanced));
    }

    public void Uninitialize(InitializationEngine context)
    {
    }

    public void Preload(string[] parameters)
    {
    }
}
```

**5. Run the indexation scheduled job.** 

It is time for you to run the Episerver scheduled job to build your index. You will find this job in the admin section of your Episerver site by the name of `ElasticEpiserver - Reindexing`. This job will run through all your content and build the index from scratch. If your index already exists, it will be recreated. 

### Perform searches on your index
Now that your index has been built, it is time to start performing searches on it. In ElasticEpiserver, searches are executed by using the `ElasticEpiSearch<T>` class. The type parameter `T` must represent your index document class, i.e. `ElasticEpiSearch<AlloyIndexDocument>`. The class is a singleton and the instance is accessed by the `Current` property, as in `ElasticEpiSearch<AlloyIndexDocument>.Current`. 

The `ElasticEpiSearch<T>` class contains a `Search()` function that takes a `ElasticEpiSearchParams` object as its argument. The `ElasticEpiSearchParams` class looks like the following and simply contains information specifying how you want to perform your search. Most of these properties should be self-explanatory. 
```csharp
public class ElasticEpiSearchParams
{
	public string Query { get; set; }
	public string LanguageName { get; set; }
	public int Skip { get; set; }
	public int Take { get; set; }
	public List<string> IncludedTypes { get; set; }
	public List<string> ExcludedTypes { get; set; }
	public bool IsExcludedFromStatistics { get; set; }
	
	/// <summary>
	/// Specify pairs of property names and values on which to perform an exact match query. 
	/// These property names must be returned by your index document's override of 
	/// ElasticEpiDocument.GetMatchFilterProperties.
	/// </summary>
	public Dictionary<string, string> MatchFilters { get; set; }

	/// <summary>
	/// Specifies how to handle best bets in the search
	/// </summary>
	public BestBetOptions BestBetOptions { get; set; }
}
```
The two last properties require some explanation. The `MatchFilters` dictionary is nicely documented in the code, but lets take some time to examine the `BestBetOptions` property. 

In ElasticEpiserver, you have the ability to specify best bets for particular search queries. That means that you can override Elasticsearch's document score and guarantee that if the user searches for a specific query, a specific page should appear on the very top of the search result. 

The `BestBetOptions` property of your search query parameters, enables you to specify some further logic regards to how this should work. 

The `BestBetOptions` class is very simple:
```csharp
public class BestBetOptions
{
    public IBestBetValidator Validator { get; set; }
    public object Data { get; set; }
}
```
The `Validator` property represents an implementation of the `IBestBetValidator` interface while the `Data` property simply holds an object. 

The `IBestBetValidator` interface can be implemented to specify whether a `PageData` should be included as a best bet. The interface looks like this: 

```csharp
public interface IBestBetValidator
{
    bool ShouldIncludeAsBestBet(PageData page, BestBetOptions options);
}
```
To clarify, lets examine a scenario in which this may be helpful:

Lets say your Episerver editor has used the administration tool to specify that whenever the users search for "top sporting brands", the `ProductPage` instances "Adidas", "Nike" and "Reebok" should appear on top of the search result. This will work fine out of the box. 

Lets continue to imagine that your Episerver site's search page consists of several contexts or tabs, one of which should only contain American sporting brands. That means that it should not include the German brand "Adidas" as a best bet anymore, but "Nike" and "Reebok" is still acceptable. To prevent "Adidas" from showing up, we can use the `BestBetOptions` class when performing the search.

We build our parameters object like the following:

```csharp
var query = "top sporting brands";
var context = "American";

var parameters = new ElasticEpiSearchParams(query, language.Name)
{
	Skip = skip,
	Take = take,
	BestBetOptions = new BestBetOptions
	{
		Validator = this,
		Data = context
	}
};
```

When ElasticEpiserver performs this search, it will start to insert the best bets that your editor has specified for this search query at the very top of the search result. However, for every best bet it comes across, it will make a call to the `BestBetOptions.Validator.ShouldIncludeAsBestBet()` function. It will supply the `PageData` object and the `BestBetOptions` specified in the search parameters back to the caller. At this point, the caller can determine whether it wants to include a given best bet in the search result. 

```csharp
public bool ShouldIncludeAsBestBet(PageData page, BestBetOptions options)
{
	var context = (string) options.Data;

	if (page == null)
		return false;
	if (new FilterPublished().ShouldFilter(page))
		return false;
	if (context == "American" && currentContext != "American")
		return false;

	return true;
}
```
The `options.Data` object contains the context we supplied when we performed the search initially. In this case we cast it back to a `string`. If the context is "American" while the implementer of `IBestBetValidator` is in a different context, we return `false` to indicate that we do not wish to include this page as a best bet. 

#### Re-implementing your project's SearchPageController
The following shows a modified `SearchPageController` from a basic Episerver Alloy site where the Episerver Search has been replaced by ElasticEpiserver.
```csharp
public class SearchPageController : PageControllerBase<SearchPage>
{
    private readonly UrlResolver _urlResolver;

    public SearchPageController(UrlResolver urlResolver)
    {
        _urlResolver = urlResolver;
    }

    [ValidateInput(false)]
    public ViewResult Index(SearchPage currentPage, string q)
    {
        var model = new SearchContentModel(currentPage)
        {
            SearchServiceDisabled = false,
            SearchedQuery = q
        };

        if (!string.IsNullOrEmpty(q))
        {
            var parameters = new ElasticEpiSearchParams(q, currentPage.Language.Name)
            {
                MatchFilters = new Dictionary<string, string>
                {
                    { nameof(AlloyIndexDocument.UniqueSellingPoints), "workflows" }
                }
            };

            var results = ElasticEpiSearch<AlloyIndexDocument>.Current.Search(parameters);

            model.NumberOfHits = results.Count;

            foreach (var res in results.Items)
            {
                var document = res.Document;

                var content = ServiceLocator.Current.GetInstance<IContentLoader>()
                    .Get<SitePageData>(new Guid(document.ContentGuid), currentPage.Language);

                model.Hits.Add(CreatePageHit(content));
            }
        }

        return View(model);
    }

    private SearchContentModel.SearchHit CreatePageHit(IContent content)
    {
        return new SearchContentModel.SearchHit
        {
            Title = content.Name,
            Url = _urlResolver.GetUrl(content.ContentLink),
            Excerpt = content is SitePageData ? ((SitePageData)content).TeaserText : string.Empty
        };
    }
}
```
Notice how the `parameters` object's `MatchFilters` property is set to contain "workflows" as a match on the `UniqueSellingPoints` field. This will ensure that all hits have "workflows" as a "unique selling point". This functionality probably doesn't make sense in your domain and is only present here for explanatory purposes. 

### Administration Tool
When you install ElasticEpiserver to your Episerver project, its administration area will contain a new plugin tool. You will find it under the "Tools" menu item by the name "ElasticEpiserver - Administration Tool". 
The purpose of the administration tool is to enable your web editors to customize the search engine to fit their specific needs. 
The administration tool is able to configure content boosting, decay of old content, synonyms, best bets, index field weights and more. More details on this is available within the tool itself. 
