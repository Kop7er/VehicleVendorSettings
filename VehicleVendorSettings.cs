using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Vehicle Vendor Settings", "Kopter", "1.0.1")]
    [Description("Change the scrap price for each vehicle, and if needed with permissions.")]
    public class VehicleVendorSettings : RustPlugin
    {
        int ScrapID;

        #region Oxide Hooks

        void Init()
        {
            permission.RegisterPermission("vehiclevendorsettings.all", this);
            permission.RegisterPermission("vehiclevendorsettings.minicopter", this);
            permission.RegisterPermission("vehiclevendorsettings.transportscraphelicopter", this);
            permission.RegisterPermission("vehiclevendorsettings.rowboat", this);
            permission.RegisterPermission("vehiclevendorsettings.rhib", this);
        }

        void OnServerInitialized()
        {
            ScrapID = ItemManager.FindItemDefinition("scrap").itemid;
        }

        void OnNpcConversationResponded(NPCTalking npcTalking, BasePlayer player, ConversationData conversationData, ConversationData.ResponseNode responseNode)
        {
            int ScrapAmount = GetScrapAmount(player, responseNode);

            if (ScrapAmount <= 0) return;

            var ScrapDifference = ItemManager.CreateByName("scrap", ScrapAmount, 0);

            if (ScrapDifference == null) return;

            player.inventory.GiveItem(ScrapDifference, player.inventory.containerMain);

            timer.Once((float)0.5, () =>
            {
                player.inventory.Take(null, ScrapID, ScrapAmount);
            });

            return;
        }

        object OnNpcConversationRespond(NPCTalking npcTalking, BasePlayer player, ConversationData conversationData, ConversationData.ResponseNode responseNode)
        {
            switch (responseNode.responseTextLocalized.english)
            {
                case "[PAY 750]":

                    if (config.MinicopterPrice < 750)
                    {
                        var ScrapDifference = ItemManager.CreateByName("scrap", 750 - config.MinicopterPrice, 0);
                        if (ScrapDifference == null) return null;
                        player.inventory.GiveItem(ScrapDifference, player.inventory.containerMain);
                    }

                    if (config.MinicopterPrice > 750)
                    {
                        player.inventory.Take(null, ScrapID, config.MinicopterPrice - 750);
                    }

                    return null;

                case "[PAY 1250]":

                    if (config.ScrapHeliPrice < 1250)
                    {
                        var ScrapDifference = ItemManager.CreateByName("scrap", 1250 - config.ScrapHeliPrice, 0);
                        if (ScrapDifference == null) return null;
                        player.inventory.GiveItem(ScrapDifference, player.inventory.containerMain);
                    }

                    if (config.ScrapHeliPrice > 1250)
                    {
                        player.inventory.Take(null, ScrapID, config.ScrapHeliPrice - 1250);
                    }

                    return null;

                case "[PAY 125 SCRAP]":

                    if (config.RowBoatPrice < 125)
                    {
                        var ScrapDifference = ItemManager.CreateByName("scrap", 125 - config.RowBoatPrice, 0);
                        if (ScrapDifference == null) return null;
                        player.inventory.GiveItem(ScrapDifference, player.inventory.containerMain);
                    }

                    if (config.MinicopterPrice > 125)
                    {
                        player.inventory.Take(null, ScrapID, config.RowBoatPrice - 125);
                    }

                    return null;

                case "[PAY 300 SCRAP]":

                    if (config.RHIBPrice < 300)
                    {
                        var ScrapDifference = ItemManager.CreateByName("scrap", 300 - config.RHIBPrice, 0);
                        if (ScrapDifference == null) return null;
                        player.inventory.GiveItem(ScrapDifference, player.inventory.containerMain);
                    }

                    if (config.RHIBPrice > 300)
                    {
                        player.inventory.Take(null, ScrapID, config.RHIBPrice - 300);
                    }

                    return null;
            }

            return null;
        }

        #endregion

        #region Scrap Amount

        int GetScrapAmount(BasePlayer player, ConversationData.ResponseNode responseNode)
        {
            switch (responseNode.resultingSpeechNode)
            {
                case "minicopterbuy":

                    if (config.MinicopterPrice == 750) return 0;

                    if (config.PermissionNeeded && !(permission.UserHasPermission(player.UserIDString, "vehiclevendorsettings.all") || permission.UserHasPermission(player.UserIDString, "vehiclevendorsettings.minicopter"))) return 0;

                    if (player.inventory.GetAmount(ScrapID) < config.MinicopterPrice) return 0;

                    return 750 - config.MinicopterPrice;

                case "transportbuy":

                    if (config.ScrapHeliPrice == 1250) return 0;

                    if (config.PermissionNeeded && !(permission.UserHasPermission(player.UserIDString, "vehiclevendorsettings.all") || permission.UserHasPermission(player.UserIDString, "vehiclevendorsettings.transportscraphelicopter"))) return 0;

                    if (player.inventory.GetAmount(ScrapID) < config.ScrapHeliPrice) return 0;

                    return 1250 - config.ScrapHeliPrice;

                case "pay_rowboat":

                    if (config.RowBoatPrice == 125) return 0;

                    if (player.inventory.GetAmount(ScrapID) < config.RowBoatPrice) return 0;

                    if (config.PermissionNeeded && !(permission.UserHasPermission(player.UserIDString, "vehiclevendorsettings.all") || permission.UserHasPermission(player.UserIDString, "vehiclevendorsettings.rowboat"))) return 0;

                    return 125 - config.RowBoatPrice;

                case "pay_rhib":

                    if (config.RHIBPrice == 300) return 0;

                    if (player.inventory.GetAmount(ScrapID) < config.RHIBPrice) return 0;

                    if (config.PermissionNeeded && !(permission.UserHasPermission(player.UserIDString, "vehiclevendorsettings.all") || permission.UserHasPermission(player.UserIDString, "vehiclevendorsettings.rhib"))) return 0;

                    return 300 - config.RHIBPrice;
            }

            return 0;
        }

        #endregion

        #region Config

        private ConfigData config = new ConfigData();
        private class ConfigData
        {
            [JsonProperty(PropertyName = "Minicopter Price")]
            public int MinicopterPrice = 750;

            [JsonProperty(PropertyName = "Scrap Helicopter Price")]
            public int ScrapHeliPrice = 1250;

            [JsonProperty(PropertyName = "Row Boat Price")]
            public int RowBoatPrice = 125;

            [JsonProperty(PropertyName = "RHIB Price")]
            public int RHIBPrice = 300;

            [JsonProperty(PropertyName = "Requires Permissions")]
            public bool PermissionNeeded = false;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<ConfigData>();

                if (config == null)
                {
                    LoadDefaultConfig();
                }
            }

            catch
            {
                PrintError("Configuration file is corrupt, check your config file at https://jsonlint.com/!");
                LoadDefaultConfig();
                return;
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            config = new ConfigData();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion
    }
}