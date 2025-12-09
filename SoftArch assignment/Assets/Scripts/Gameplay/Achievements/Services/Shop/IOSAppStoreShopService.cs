using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IOSAppStoreShopService : ShopService
{
    public override void PurchaseProduct(string productName)
    {
        shopText.text = "You've purchased " + productName + " from the iOS App Store, you must be rich.";
    }
}
