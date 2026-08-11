#if UNITY_STANDALONE

using TitanCore.Iap;

public class LocalStoreManager : StoreManager
{
    private Dispatcher dispatcher = new Dispatcher();

    public LocalStoreManager(StoreMenu menu) : base(menu)
    {
    }

    public override void Initialize()
    {
        SetState(StoreManagerState.Ready);
    }

    public override void StartPurchase(IapProduct product)
    {
        menu.ShowLoading();
        WebClient.SendLocalFreePurchase(product.productId, response =>
        {
            dispatcher.Push(() =>
            {
                menu.HideLoading();

                if (response.exception != null)
                {
                    ApplicationAlert.Show("Uh oh.", "Unable to connect to the local server.", null, "Okay");
                    return;
                }

                if (response.item == null || !response.item.success)
                {
                    ApplicationAlert.Show("Uh oh.", "The local server failed to grant currency.", null, "Okay");
                    return;
                }

                if (Account.describe != null)
                    Account.describe.currency += product.currencyReward;

                ApplicationAlert.Show("Success", $"{Constants.Premium_Currency_Sprite}{product.currencyReward} added.", null, "Okay");
            });
        });
    }

    public override void GetPrice(IapProduct product, out string priceString)
    {
        priceString = "Free";
    }

    public override void LateUpdate()
    {
        dispatcher.RunActions();
    }

    public override void Enable()
    {
    }

    public override void Disable()
    {
    }
}

#endif
