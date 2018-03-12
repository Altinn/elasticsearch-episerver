using System.Web.Mvc;
using System.Web.Routing;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;

namespace ElasticEpiserver.Module.Business.Initialization
{
    [InitializableModule]
    public class RoutesInitialization : IInitializableModule
    {
        public const string ControllerName = "AdministrationTool";

        public void Initialize(InitializationEngine context)
        {
            RouteTable.Routes.MapRoute("AdministrationPluginHome", "elasticepi-admin/search-admin",
                new { controller = ControllerName, action = "Index" });

            // Search
            RouteTable.Routes.MapRoute("AdministrationPluginSearch", "elasticepi-admin/search-admin/search",
                new { controller = ControllerName, action = "Search" });

            RouteTable.Routes.MapRoute("AdministrationPluginUpdateEngine", "elasticepi-admin/search-admin/UpdateEngine",
                new { controller = ControllerName, action = "UpdateEngine" });

            RouteTable.Routes.MapRoute("AdministrationPluginLogHit", "elasticepi-admin/search-admin/hit",
                new { controller = ControllerName, action = "LogHit" });

            // Decay
            RouteTable.Routes.MapRoute("AdministrationPluginSaveDecay", "elasticepi-admin/search-admin/save-decay",
                new { controller = ControllerName, action = "SaveDecay" });

            // Boosts
            RouteTable.Routes.MapRoute("AdministrationPluginSaveBoost", "elasticepi-admin/search-admin/save-boost",
                new { controller = ControllerName, action = "SaveBoost" });

            // Synonyms
            RouteTable.Routes.MapRoute("AdministrationPluginCreateSynonymContainer", "elasticepi-admin/search-admin/create-synonym-container",
                new { controller = ControllerName, action = "CreateSynonymContainer" });

            RouteTable.Routes.MapRoute("AdministrationPluginDeleteSynonymContainer", "elasticepi-admin/search-admin/delete-synonym-container",
                new { controller = ControllerName, action = "DeleteSynonymContainer" });

            RouteTable.Routes.MapRoute("AdministrationPluginAddSynonym", "elasticepi-admin/search-admin/add-synonym",
                new { controller = ControllerName, action = "AddSynonym" });

            RouteTable.Routes.MapRoute("AdministrationPluginRemoveSynonym", "elasticepi-admin/search-admin/remove-synonym",
                new { controller = ControllerName, action = "RemoveSynonym" });

            // Best bets
            RouteTable.Routes.MapRoute("AdministrationPluginCreateBestBet", "elasticepi-admin/search-admin/create-bestbet",
                new { controller = ControllerName, action = "CreateBestBet" });

            RouteTable.Routes.MapRoute("AdministrationPluginDeleteBestBet", "elasticepi-admin/search-admin/delete-bestbet",
                new { controller = ControllerName, action = "DeleteBestBet" });

            RouteTable.Routes.MapRoute("AdministrationPluginAddBestBetContent", "elasticepi-admin/search-admin/add-bestbet-content",
                new { controller = ControllerName, action = "AddBestBetContent" });

            RouteTable.Routes.MapRoute("AdministrationPluginRemoveBestBetContent", "elasticepi-admin/search-admin/remove-bestbet-content",
                new { controller = ControllerName, action = "RemoveBestBetContent" });

            RouteTable.Routes.MapRoute("AdministrationPluginOrderBestBetContents", "elasticepi-admin/search-admin/order-bestbet-contents",
                new { controller = ControllerName, action = "OrderBestBetContents" });

            // Weight settings
            RouteTable.Routes.MapRoute("AdministrationPluginUpdateWeightSettings", "elasticepi-admin/search-admin/save-weight-settings",
                new { controller = ControllerName, action = "SavePropertyWeightItem" });

            RouteTable.Routes.MapRoute("AdministrationPluginDeleteWeightSettings", "elasticepi-admin/search-admin/delete-weight-settings",
                new { controller = ControllerName, action = "DeletePropertyWeightItem" });
        }

        public void Uninitialize(InitializationEngine context)
        {
            RouteTable.Routes.Remove(RouteTable.Routes["AdministrationPluginHome"]);

            // Search
            RouteTable.Routes.Remove(RouteTable.Routes["AdministrationPluginSearch"]);
            RouteTable.Routes.Remove(RouteTable.Routes["AdministrationPluginUpdateEngine"]);
            RouteTable.Routes.Remove(RouteTable.Routes["AdministrationPluginLogHit"]);

            // Boost
            RouteTable.Routes.Remove(RouteTable.Routes["AdministrationPluginSaveBoost"]);

            // Decay
            RouteTable.Routes.Remove(RouteTable.Routes["AdministrationPluginSaveDecay"]);

            // Synonyms
            RouteTable.Routes.Remove(RouteTable.Routes["AdministrationPluginCreateSynonymContainer"]);
            RouteTable.Routes.Remove(RouteTable.Routes["AdministrationPluginDeleteSynonymContainer"]);
            RouteTable.Routes.Remove(RouteTable.Routes["AdministrationPluginAddSynonym"]);
            RouteTable.Routes.Remove(RouteTable.Routes["AdministrationPluginRemoveSynonym"]);

            // Best bets
            RouteTable.Routes.Remove(RouteTable.Routes["AdministrationPluginCreateBestBet"]);
            RouteTable.Routes.Remove(RouteTable.Routes["AdministrationPluginDeleteBestBet"]);
            RouteTable.Routes.Remove(RouteTable.Routes["AdministrationPluginAddBestBetContent"]);
            RouteTable.Routes.Remove(RouteTable.Routes["AdministrationPluginRemoveBestBetContent"]);
            RouteTable.Routes.Remove(RouteTable.Routes["AdministrationPluginOrderBestBetContents"]);

            // Weight settings
            RouteTable.Routes.Remove(RouteTable.Routes["AdministrationPluginUpdateWeightSettings"]);
            RouteTable.Routes.Remove(RouteTable.Routes["AdministrationPluginDeleteWeightSettings"]);
        }
    }
}